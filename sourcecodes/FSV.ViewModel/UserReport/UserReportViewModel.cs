// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.ViewModel.UserReport
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Business;
    using Business.Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Core;
    using Database.Models;
    using Events;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Passables;
    using Prism.Events;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class UserReportViewModel : UserReportBaseViewModel, ISortable, IEquatable<UserReportViewModel>
    {
        private const string ReportModelTypeName = nameof(UserReportViewModel); // Info: this constant field is used to distinguish from other report-models of a different type, but initialized with an equal UserPath object.

        private readonly IUserPermissionTask _userPermissionTask;
        private readonly IUserReportService _userReportService;
        private readonly IConfigurationManager configurationManager;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;

        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly IExportService exportService;
        private readonly ILogger<UserReportViewModel> logger;

        private readonly object syncObject = new();

        private readonly UserPath userPath;
        private ICommand _cancelScanCommand;
        private ICommand _exportCommand;
        private ICommand _saveReportCommand;
        private ICommand _sortCommand;
        private bool disposed;

        internal UserReportViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            IUserReportService userReportService,
            IUserPermissionTask userPermissionTask,
            IDatabaseConfigurationManager dbConfigurationManager,
            IConfigurationManager configurationManager,
            ILogger<UserReportViewModel> logger,
            IExportService exportService,
            ModelBuilder<SubspaceContainerViewModel> subspaceContainerViewModelBuilder,
            UserPath userPath)
            : base(userPath?.Path, userPath?.User)
        {
            this.userPath = userPath ?? throw new ArgumentNullException(nameof(userPath));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this._userReportService = userReportService ?? throw new ArgumentNullException(nameof(userReportService));
            this._userPermissionTask = userPermissionTask ?? throw new ArgumentNullException(nameof(userPermissionTask));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            this.SubspaceContainer = subspaceContainerViewModelBuilder?.Build() ?? throw new ArgumentNullException(nameof(subspaceContainerViewModelBuilder));

            this.ReportType = ReportType.User;
            this.ReportTypeCaption = UserReport;
            this.Title = string.Format(UserReportResource.UserReportHeaderCaption, userPath.User, userPath.Path);
            this.DisplayName = $"{UserReport} - {userPath.User}";

            this.Pagination = new PaginationViewModel(this.configurationManager, async m => await this.MakeResultAsync())
            {
                ShowText = FolderReportResource.PaginationFoldersCaption
            };

            this.Header = new HeaderViewModel(this.Pagination, this.CancelCommand)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync),
                RefreshCommand = new AsyncRelayCommand(this.RestartScanAsync),
                UseRefreshCache = true
            };

            this.SortColumn = UserReportResource.UserReportNameCaption;
            this.SortDirection = SortOrder.Ascending;

            this.InitializeScanAsync().FireAndForgetSafeAsync();
        }

        public DataTable Folders { get; private set; }

        public SubspaceContainerViewModel SubspaceContainer { get; }

        public ICommand CancelScanCommand => this._cancelScanCommand ??= new RelayCommand(this.StopScan, p => !this._userPermissionTask.CancelRequested);
        public ICommand SaveReportCommand => this._saveReportCommand ??= new AsyncRelayCommand(this.SaveReportAsync);

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export);

        public DataTable FilteredFolders { get; private set; }

        public bool Equals(UserReportViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(this.userPath, other.userPath);
        }

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(async x => await this.MakeResultAsync());

        public string GetExportSortColumn()
        {
            return this.SortColumn;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.Folders?.Dispose();
                this.FilteredFolders?.Dispose();
                this.disposed = true;
            }
        }

        private async Task InitializeScanAsync()
        {
            this.DoProgress();

            try
            {
                await this.OnProgressAsync(0);
                UserPermissionTaskResult result = await this._userPermissionTask.RunAsync(this.UserName, this.FolderPath, async count => { await this.OnProgressAsync(count); });
                this.OnScanComplete(result);
            }
            catch (Exception ex)
            {
                this.StopScan(true);

                this.logger.LogError(ex, "Failed to initialize scan for folder {Path}.", this.FolderPath);
                this.dialogService.ShowMessage($"Failed to initialize scan for folder {this.FolderPath}.");
            }
            finally
            {
                this.StopProgress();
            }
        }

        private async Task OnProgressAsync(int count)
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                this.RowCount = count;
                this.Header.SetText(count == 1 ? UserReportResource.LoadFolderCaption : UserReportResource.LoadFoldersCaption);
            });
        }

        private void OnScanComplete(UserPermissionTaskResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.ScanCancelled)
            {
                this.Header.ShowCancel = false;
                this.Header.SearchDisabled = true;
                this.Header.SetText(string.Format(PermissionResource.ScanCancelledText, string.Empty));

                return;
            }

            this.AllFolders = result.Result;
            this.Folders = this.AllFolders.Clone();
            this.FilteredFolders = this.AllFolders.Clone();

            this.Header.ShowCancel = false;
            this.Header.SearchDisabled = this.AllFolders.Rows.Count == 0;

            var skippedFolders = new UserReportSkippedFoldersViewModel(result.ExceptionFolders);
            this.SkippedFolders = skippedFolders.Folders;

            lock (this.syncObject)
            {
                this.SubspaceContainer.ClearItems();
                this.SubspaceContainer.AddItem(skippedFolders);
            }

            foreach (IFolderReport folder in result.ExceptionFolders)
            {
                this.logger.LogError(folder.Exception, "The operation completed with errors.");
            }

            this.ChainTasksAsync(true).FireAndForgetSafeAsync();
        }

        private void StopScan(object p)
        {
            if (this._userPermissionTask.IsBusy)
            {
                this._userPermissionTask.Cancel();
            }

            this.Header.SetText(string.Format(PermissionResource.ScanCancelledText, p != null ? PermissionResource.ScanCancelledReasonText : string.Empty), true);
            this.Header.ShowCancel = false;
            this.Header.SearchDisabled = true;
        }

        private async Task SearchAsync(object O_o)
        {
            await this.ChainTasksAsync(true);
        }

        private async Task ChainTasksAsync(bool createFilter = false)
        {
            this.Header.ShowProgress();

            if (createFilter)
            {
                await Task.Run(this.CreateFilter);
            }

            await Task.Run(this.MakeResultAsync);

            this.Header.EndProgress();
        }

        private void CreateFilter()
        {
            int totalRows;
            string text;

            if (!string.IsNullOrEmpty(this.Header.SearchText))
            {
                this.FilteredFolders.Clear();

                DataRow[] rows = this.AllFolders.Select(this.CreateSearchExpression(this.Header.SearchText));
                if (rows.Length > 0)
                {
                    this.FilteredFolders = rows.CopyToDataTable();
                }

                totalRows = rows.Length;
                text = string.Format(totalRows == 1 ? UserReportResource.FolderSearchResultText : UserReportResource.FolderSearchResultsText, totalRows, this.FolderPath, this.Header.SearchText);
            }
            else
            {
                totalRows = this.AllFolders?.Rows.Count ?? 0;
                text = string.Format(totalRows == 1 ? UserReportResource.FolderResultText : UserReportResource.FolderResultsText, totalRows, this.FolderPath);
            }

            this.Pagination.TotalRows = totalRows;
            this.Header.SetText(text, totalRows == 0);
        }

        private async Task MakeResultAsync()
        {
            if (this.AllFolders == null || this.AllFolders.Rows.Count == 0)
            {
                return;
            }

            int end, start;
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            if (this.Pagination.PageSize > 0)
            {
                end = this.Pagination.PageSize * pageNo;
                start = this.Pagination.PageSize * (pageNo - 1);
            }
            else
            {
                end = this.Pagination.TotalRows;
                start = 0;
            }

            string sortExpression = string.IsNullOrEmpty(this.SortColumn) ? string.Empty : this.SortColumn + " " + this.SortDirection.ToShortString();

            DataTable dataTable = string.IsNullOrEmpty(this.Header.SearchText) ? this.AllFolders : this.FilteredFolders;
            DataRow[] rows = dataTable.Select(string.Empty, sortExpression);

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Folders.Clear();

                for (int index = start; index < end; index++)
                {
                    if (index == dataTable.Rows.Count)
                    {
                        break;
                    }

                    DataRow newRow = this.Folders.NewRow();
                    newRow.ItemArray = rows[index].ItemArray.Clone() as object[];
                    this.Folders.Rows.Add(newRow);
                }

                this.RaisePropertyChanged(nameof(this.Folders));
            });
        }

        private string CreateSearchExpression(string searchText)
        {
            searchText = "*" + searchText + "*";
            string result = string.Join(" OR ", this.AllFolders.Columns.Cast<DataColumn>()
                .Where(m => m.DataType == typeof(string))
                .Select(m => $"[{m.ColumnName}] LIKE '{searchText}'"));
            return result;
        }

        private async Task SaveReportAsync(object p)
        {
            if (this.dbConfigurationManager.HasConfiguredDatabaseProvider() == false)
            {
                this.dialogService.ShowMessage(SettingDatabaseResource.NoDBSetupText);
                return;
            }

            this.Header.ShowCancel = false;
            this.Header.ShowProgress();

            try
            {
                UserPermissionReport result = await this._userReportService.Save(Environment.UserName, this);
                if (result != null)
                {
                    await this.dispatcherService.InvokeAsync(() =>
                    {
                        this.eventAggregator.GetEvent<UserReportSavedEvent>().Publish();
                        this.dialogService.ShowMessage(string.Format(UserReportResource.ReportSaved, this.FolderPath));
                    });
                }
            }
            catch (Exception ex)
            {
                this.dialogService.ShowMessage(ex.Message);
            }
            finally
            {
                await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress());
            }
        }

        private async Task RestartScanAsync(object p)
        {
            if (p != null &&
                p is bool useCache &&
                useCache)
            {
                this._userPermissionTask.ClearActiveDirectoryCache();
                this.SetTipMessageAsync(CommonResource.ADCacheClearedText);
            }

            this.Header.SearchClearCommand.Execute(null);

            this.AllFolders?.Clear();
            this.FilteredFolders?.Clear();
            this.Folders?.Clear();

            this.Pagination.TotalRows = 0;

            await this.InitializeScanAsync();
        }

        private void Export(object _)
        {
            this.exportService.ExportUserReports(new[] { this });
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is UserReportViewModel other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.userPath != null ? this.userPath.GetHashCode() : 0) * 397) ^ ReportModelTypeName.GetHashCode();
            }
        }
    }
}