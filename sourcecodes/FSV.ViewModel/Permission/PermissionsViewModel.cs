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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Business.Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Events;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;
    using Prism.Events;
    using Resources;

    public class PermissionsViewModel : PermissionReportBaseViewModel, ISortable
    {
        public const string GiListColumnName = FsvColumnConstants.PermissionGiListColumnName;
        private readonly Func<ReportTrustee> configReportTrustee;
        private readonly IConfigurationManager configurationManager;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly IExportService exportService;
        private readonly IFlyOutService flyOutService;
        private readonly ModelBuilder<DataTable, string, GroupPermissionsViewModel> groupPermissionsViewModelBuilder;
        private readonly ILogger<PermissionsViewModel> logger;
        private readonly INavigationService navigationService;
        private readonly PaginationViewModel Pagination;
        private readonly ModelBuilder<string, PermissionItemACLDifferenceViewModel> permissionItemAclDifferenceViewModelBuilder;
        private readonly ModelBuilder<string, PermissionItemAclViewModel> permissionItemAclViewModelBuilder;
        private readonly ModelBuilder<string, PermissionItemOwnerViewModel> permissionItemOwnerViewModelBuilder;
        private readonly ModelBuilder<string, PermissionItemSavedReportsViewModel> permissionItemSavedReportsViewModelBuilder;
        private readonly IPermissionTask permissionTask;
        private readonly IPermissionReportManager reportManager;

        private ICommand _cancelScanCommand;
        private ICommand _closeSubspaceCommand;
        private string _columnDomainName;

        private string _columnPermissionsName;

        private PermissionItemBase _currentSubSpace;
        private ICommand _exportCommand;
        private ICommand _groupInheritanceCommand;
        private ICommand _groupPermissionsCommand;

        private bool _maximizeSubspace;
        private ICommand _resizeSubspaceCommand;
        private ICommand _saveReportCommand;
        private ICommand _sortCommand;

        private bool disposed;

        public PermissionsViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            IFlyOutService flyOutService,
            IPermissionReportManager reportManager,
            IPermissionTask permissionTask,
            IDatabaseConfigurationManager dbConfigurationManager,
            IConfigurationManager configurationManager,
            Func<ReportTrustee> configReportTrustee,
            ILogger<PermissionsViewModel> logger,
            IExportService exportService,
            INavigationService navigationService,
            ModelBuilder<string, PermissionItemAclViewModel> permissionItemAclViewModelBuilder,
            ModelBuilder<string, PermissionItemACLDifferenceViewModel> permissionItemAclDifferenceViewModelBuilder,
            ModelBuilder<string, PermissionItemSavedReportsViewModel> permissionItemSavedReportsViewModelBuilder,
            ModelBuilder<string, PermissionItemOwnerViewModel> permissionItemOwnerViewModelBuilder,
            ModelBuilder<DataTable, string, GroupPermissionsViewModel> groupPermissionsViewModelBuilder,
            string path)
            : base(path)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));

            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.permissionTask = permissionTask ?? throw new ArgumentNullException(nameof(permissionTask));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.configReportTrustee = configReportTrustee ?? throw new ArgumentNullException(nameof(configReportTrustee));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            this.permissionItemAclViewModelBuilder = permissionItemAclViewModelBuilder ?? throw new ArgumentNullException(nameof(permissionItemAclViewModelBuilder));
            this.permissionItemAclDifferenceViewModelBuilder = permissionItemAclDifferenceViewModelBuilder ?? throw new ArgumentNullException(nameof(permissionItemAclDifferenceViewModelBuilder));
            this.permissionItemSavedReportsViewModelBuilder = permissionItemSavedReportsViewModelBuilder ?? throw new ArgumentNullException(nameof(permissionItemSavedReportsViewModelBuilder));
            this.permissionItemOwnerViewModelBuilder = permissionItemOwnerViewModelBuilder ?? throw new ArgumentNullException(nameof(permissionItemOwnerViewModelBuilder));
            this.groupPermissionsViewModelBuilder = groupPermissionsViewModelBuilder ?? throw new ArgumentNullException(nameof(groupPermissionsViewModelBuilder));

            this.ReportTypeCaption = PermissionReport;
            this.DisplayName = PermissionReport;
            this.ReportType = ReportType.Permission;

            this.SortDirection = SortOrder.Indeterminate;
            this.Items = new ObservableCollection<PermissionItemBase>();
            this.Pagination = new PaginationViewModel(this.configurationManager, this.ChangePageAsync, PermissionResource.UsersCaption);
            this.Header = new HeaderViewModel(this.Pagination, this.CancelScanCommand)
            {
                SearchCommand = new AsyncRelayCommand(this.Search),
                RefreshCommand = new AsyncRelayCommand(this.RestartScan),
                UseRefreshCache = true
            };

            this.SetDataTableColumns(configReportTrustee());

            this.flyOutService.ContentAdded += this.HandleFlyOutServiceContentAdded;

            this.InitializeScan().FireAndForgetSafeAsync();
        }

        public IEnumerable<IAclModel> AccessControlList
        {
            get
            {
                PermissionItemAclViewModel permissionItemAclViewModel = this.Items.OfType<PermissionItemAclViewModel>().FirstOrDefault();
                IEnumerable<IAclModel> accessControlList = permissionItemAclViewModel?.AccessControlList;
                return accessControlList?.ToList() ?? Enumerable.Empty<IAclModel>();
            }
        }

        public PermissionItemBase CurrentSubspace
        {
            get => this._currentSubSpace;
            set => this.DoSet(ref this._currentSubSpace, value, nameof(this.CurrentSubspace));
        }

        public bool MaximizeSubspace
        {
            get => this._maximizeSubspace;
            private set => this.Set(ref this._maximizeSubspace, value, nameof(this.MaximizeSubspace));
        }

        public IList<PermissionItemBase> Items { get; }

        public DataTable Permissions { get; private set; }

        public string OriginatingGroupColumnName { get; private set; }

        public ICommand CancelScanCommand => this._cancelScanCommand ??= new RelayCommand(this.StopScan, p => !this.permissionTask.CancelRequested);

        public ICommand SaveReportCommand => this._saveReportCommand ??= new AsyncRelayCommand(this.SaveReportAsync, this.CanSaveReportAsync);

        public ICommand CloseSubspaceCommand => this._closeSubspaceCommand ??= new RelayCommand(this.CloseSubspace);

        public ICommand ResizeSubspaceCommand => this._resizeSubspaceCommand ??= new RelayCommand(this.ResizeSubspace);

        public ICommand GroupInheritanceCommand => this._groupInheritanceCommand ??= new RelayCommand(this.ShowGroupInheritance);

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export, this.CanExport);

        public ICommand GroupPermissionsCommand => this._groupPermissionsCommand ??= new AsyncRelayCommand(this.InitiateGroupPermissionsAsync, this.CanInitiateGroupPermissionsAsync);

        private DataTable FilteredPermissions { get; set; }

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string GetExportSortColumn()
        {
            return this.SortColumn;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !this.disposed)
            {
                this.flyOutService.ContentAdded -= this.HandleFlyOutServiceContentAdded;
                this.disposed = true;
            }
        }

        private void HandleFlyOutServiceContentAdded(object s, FlyoutViewModel e)
        {
            this.CurrentSubspace = null;
        }

        protected override void OnClosing(CloseCommandEventArgs e)
        {
            base.OnClosing(e);

            this.StopScan(null);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(nameof(PermissionsViewModel), this.SelectedFolderPath).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is PermissionsViewModel vm && vm.GetHashCode() == this.GetHashCode();
        }

        private async Task ChangePageAsync(PageChangeMode o_0)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object o_O)
        {
            await this.ChainTasksAsync();
        }

        private async Task Search(object O_o)
        {
            await this.ChainTasksAsync(true);
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

            SavedReportItemViewModel report = await Task.Run(async () =>
            {
                try
                {
                    SavedReportItemViewModel report = this.reportManager.Add(Environment.UserName, null, this.SelectedFolderPath, this.AllPermissions, this.dbConfigurationManager.Config.Encrypted);
                    this.logger.LogInformation("The {Path} permission report has been saved in database.", this.SelectedFolderPath);
                    return report;
                }
                catch (Exception ex)
                {
                    const string errorMessage = "Failed to save the selected permission report to the database due to an unhandled error.";
                    this.logger.LogError(ex, errorMessage);
                    await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(errorMessage));
                    return null;
                }
            });

            if (report != null)
            {
                this.eventAggregator.GetEvent<PermissionSavedEvent>().Publish(report);
                this.dialogService.ShowMessage(string.Format(PermissionResource.PermissionSavedText, this.SelectedFolderPath));
            }

            this.Header.EndProgress();
        }

        private async Task<bool> CanSaveReportAsync(object _)
        {
            bool canSaveReport = !this.IsWorking && !this.Header.IsWorking && !this.Header.SearchDisabled && this.AllPermissions?.Rows.Count > 0;
            return await Task.FromResult(canSaveReport);
        }

        private async Task InitializeScan()
        {
            this.DoProgress();

            string selectedFolderPath = this.SelectedFolderPath;

            try
            {
                await this.AddItemsAsync();

                this.logger.LogDebug("Start Scan for {Path}.", selectedFolderPath);

                this.OnProgress(0);
                DataTable result = await this.permissionTask.RunAsync(selectedFolderPath, this.OnProgress);
                this.OnScanComplete(result);
            }
            catch (Exception e)
            {
                this.StopScan(true);

                this.logger.LogError(e, "Failed to initialize folder scan for path {Path}.", selectedFolderPath);
                this.dialogService.ShowMessage("Failed to initialize folder scan.");
            }
            finally
            {
                this.StopProgress();
                this.logger.LogDebug("Scan for {Path} completed.", selectedFolderPath);
            }
        }

        private void OnScanComplete(DataTable result)
        {
            if (result == null)
            {
                this.Header.ShowCancel = false;
                this.Header.SearchDisabled = true;
                this.Header.SetText(string.Format(PermissionResource.ScanCancelledText, string.Empty));

                return;
            }

            this.AllPermissions = this.LimitData(result);
            this.Permissions = this.AllPermissions.Clone();
            this.FilteredPermissions = this.AllPermissions.Clone();

            this.Header.ShowCancel = false;
            this.Header.SearchDisabled = this.AllPermissions.Rows.Count == 0;

            this.ChainTasksAsync(true).FireAndForgetSafeAsync();
        }

        private void OnProgress(int count)
        {
            this.RowCount = count;
            this.Header.SetText(count == 1 ? PermissionResource.UserFoundCaption : PermissionResource.UsersFoundCaption);
        }

        private async Task AddItemsAsync()
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                ReportTrustee reportTrustee = this.configReportTrustee();
                if (reportTrustee.Settings.ShowAcl)
                {
                    this.Items.Add(this.permissionItemAclViewModelBuilder.Build(this.SelectedFolderPath));
                }

                this.Items.Add(this.permissionItemOwnerViewModelBuilder.Build(this.SelectedFolderPath));
                this.Items.Add(this.permissionItemAclDifferenceViewModelBuilder.Build(this.SelectedFolderPath));
                this.Items.Add(this.permissionItemSavedReportsViewModelBuilder.Build(this.SelectedFolderPath));
            });
        }

        private async Task RestartScan(object p)
        {
            if (p != null &&
                p is bool useCache &&
                useCache)
            {
                this.permissionTask.ClearADCache();
                this.SetTipMessageAsync(CommonResource.ADCacheClearedText);
            }

            this.Header.SearchClearCommand.Execute(null);

            this.Pagination.TotalRows = 0;

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.AllPermissions?.Clear();
                this.FilteredPermissions?.Clear();
                this.Permissions?.Clear();
                this.Items.Clear();
            });

            await this.InitializeScan();
        }

        private void StopScan(object p)
        {
            this.Items
                .OfType<PermissionItemACLDifferenceViewModel>()
                .FirstOrDefault()?
                .StopScan();

            if (this.permissionTask.IsBusy)
            {
                this.permissionTask.Cancel();
            }

            this.Header.SetText(string.Format(PermissionResource.ScanCancelledText, p != null ? PermissionResource.ScanCancelledReasonText : string.Empty), true);
            this.Header.ShowCancel = false;
            this.Header.SearchDisabled = true;
        }

        private async Task MakeResultAsync()
        {
            if (this.AllPermissions == null || this.AllPermissions.Rows.Count == 0)
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

            DataTable dataTable = string.IsNullOrEmpty(this.Header.SearchText) ? this.AllPermissions : this.FilteredPermissions;

            DataRow[] rows = dataTable.Select(string.Empty, sortExpression);

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Permissions.Clear();

                for (int index = start; index < end; index++)
                {
                    if (index == dataTable.Rows.Count)
                    {
                        break;
                    }

                    DataRow newRow = this.Permissions.NewRow();
                    newRow.ItemArray = rows[index].ItemArray.Clone() as object[];
                    this.Permissions.Rows.Add(newRow);
                }

                this.RaisePropertyChanged(nameof(this.Permissions));
            });
        }

        private void CreateFilter()
        {
            int totalRows;
            string text;

            if (!string.IsNullOrEmpty(this.Header.SearchText))
            {
                this.FilteredPermissions.Clear();

                DataRow[] rows = this.AllPermissions.Select(this.CreateSearchExpression(this.Header.SearchText));
                if (rows.Length > 0)
                {
                    this.FilteredPermissions = rows.CopyToDataTable();
                }

                totalRows = rows.Length;
                text = string.Format(totalRows == 1 ? PermissionResource.PermissionSearchResultText : PermissionResource.PermissionSearchResultsText, totalRows, this.SelectedFolderPath, this.Header.SearchText);
            }
            else
            {
                totalRows = this.AllPermissions?.Rows.Count ?? 0;
                text = string.Format(totalRows == 1 ? PermissionResource.PermissionResultText : PermissionResource.PermissionResultsText, totalRows, this.SelectedFolderPath);
            }

            this.Pagination.TotalRows = totalRows;

            this.Header.SetText(text, totalRows == 0);
        }

        private async Task ChainTasksAsync(bool createFilter = false)
        {
            this.Header.ShowProgress();

            if (createFilter)
            {
                await Task.Run(this.CreateFilter);
            }

            await this.MakeResultAsync();

            this.Header.EndProgress();
        }

        private string CreateSearchExpression(string searchText)
        {
            searchText = "*" + searchText + "*";
            string result = string.Join(" OR ", this.AllPermissions.Columns.Cast<DataColumn>()
                .Where(m => m.DataType == typeof(string))
                .Select(m => $"[{m.ColumnName}] LIKE '{searchText}'"));
            return result;
        }

        private string GetParentPath(string path)
        {
            return path.Substring(0, path.LastIndexOf(@"\"));
        }

        private void ShowGroupInheritance(object param)
        {
            DataRow row = (param as DataRowView)?.Row;
            List<string> list = row[FsvColumnConstants.PermissionGiListColumnName] as List<string> ?? new List<string>(0);

            this.dialogService.ShowDialog(new GroupInheritanceViewModel(list));
        }

        private void CloseSubspace(object p)
        {
            this.CurrentSubspace = null;
            this.MaximizeSubspace = false;
        }

        private void ResizeSubspace(object p)
        {
            this.MaximizeSubspace = !this.MaximizeSubspace;
        }

        private void Export(object p)
        {
            this.exportService.ExportPermissionReports(new[] { this });
        }

        private bool CanExport(object O_0)
        {
            return !this.IsWorking && !this.Header.IsWorking && !this.Header.SearchDisabled;
        }

        private Task<bool> CanInitiateGroupPermissionsAsync(object _)
        {
            return Task.FromResult(this.AllPermissions is not null && this.AllPermissions.Rows.Count > 0);
        }

        private async Task InitiateGroupPermissionsAsync(object _)
        {
            IEnumerable<DataRow> rows = this.AllPermissions
                .AsEnumerable()
                .Where(m => m[FsvColumnConstants.PermissionGiListColumnName] is IEnumerable<string> list && list.Any())
                .GroupBy(m => new
                {
                    Group = m.Field<string>(this.OriginatingGroupColumnName),
                    Domain = m.Field<string>(this._columnDomainName),
                    Permissions = m.Field<string>(this._columnPermissionsName)
                })
                .Select(m => m.First());

            try
            {
                DataTable groupsTable = this.GetGroupPermissionsDataTable(rows);

                GroupPermissionsViewModel groupViewModel = this.groupPermissionsViewModelBuilder.Build(groupsTable, this.FolderPath);
                await this.navigationService.NavigateWithAsync(groupViewModel);
                await groupViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while generating group permissions for {folderPath}", this.FolderPath);
                this.dialogService.ShowMessage($"Error while generating group permissions for {this.FolderPath}");
            }
        }

        private DataTable GetGroupPermissionsDataTable(IEnumerable<DataRow> dataRows)
        {
            DataTable groupsTable = new("PermissionGroups");

            groupsTable.Columns.AddRange(new[]
            {
                new DataColumn(this.OriginatingGroupColumnName, typeof(string)),
                new DataColumn(this._columnDomainName, typeof(string)),
                new DataColumn(this._columnPermissionsName, typeof(string))
            });

            foreach (DataRow row in dataRows)
            {
                groupsTable.Rows.Add(
                    row[this.OriginatingGroupColumnName],
                    row[this._columnDomainName],
                    row[this._columnPermissionsName]);
            }

            return groupsTable;
        }

        private void SetDataTableColumns(ReportTrustee reportTrustee)
        {
            if (reportTrustee?.TrusteeGridColumns is null)
            {
                this.OriginatingGroupColumnName = FsvColumnConstants.OriginatingGroup;
                this._columnPermissionsName = FsvColumnConstants.Rigths;
                this._columnDomainName = FsvColumnConstants.Domain;
                return;
            }

            Dictionary<string, string> columns = reportTrustee
                .TrusteeGridColumns
                .Where(m => m.Name is FsvColumnConstants.OriginatingGroup or FsvColumnConstants.Domain or FsvColumnConstants.Rigths)
                .ToDictionary(m => m.Name, n => n.DisplayName ?? string.Empty);

            this.OriginatingGroupColumnName = columns.TryGetValue(FsvColumnConstants.OriginatingGroup, out string originatingGroup) && string.IsNullOrEmpty(originatingGroup) == false
                ? originatingGroup
                : FsvColumnConstants.OriginatingGroup;

            this._columnDomainName = columns.TryGetValue(FsvColumnConstants.Domain, out string domain) && string.IsNullOrEmpty(domain) == false
                ? domain
                : FsvColumnConstants.Domain;

            this._columnPermissionsName = columns.TryGetValue(FsvColumnConstants.Rigths, out string rights) && string.IsNullOrEmpty(rights) == false
                ? rights
                : FsvColumnConstants.Rigths;
        }
    }
}