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

namespace FSV.ViewModel.Folder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Business.Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Core;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class FolderViewModel : LimitDataViewModel, ISortable, IEquatable<FolderViewModel>
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;

        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IFolderTask folderTask;
        private readonly ILogger<FolderViewModel> logger;

        private ICommand cancelScanCommand;
        private bool disposed;
        private ICommand exportCommand;

        private IList<FolderItemViewModel> filteredItems;

        private ICommand sortCommand;

        public FolderViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IFolderTask folderTask,
            IConfigurationManager configurationManager,
            ILogger<FolderViewModel> logger,
            IExportService exportService,
            string path)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.folderTask = folderTask ?? throw new ArgumentNullException(nameof(folderTask));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            this.ReportType = ReportType.Folder;
            this.ReportTypeCaption = FolderReport;
            this.Title = path;
            this.DisplayName = FolderReport;
            this.Path = path;
            this.FolderPath = path;

            this.FoldersTable = new FoldersTableViewModel(configurationManager);

            async Task PageChangeAsyncCallback(PageChangeMode m)
            {
                await this.MakeResult();
            }

            this.Pagination = new PaginationViewModel(this.configurationManager, PageChangeAsyncCallback)
            {
                ShowText = FolderReportResource.PaginationFoldersCaption
            };

            this.Header = new HeaderViewModel(this.Pagination, this.CancelScanCommand)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync),
                RefreshCommand = new AsyncRelayCommand(this.RestartScanAsync)
            };

            this.SortDirection = SortOrder.Ascending;
            this.SortColumn = this.FoldersTable.GetColumnName(1);

            this.InitScanAsync().FireAndForgetSafeAsync();
        }

        // It contains all items fetched from worker.
        public IList<FolderItemViewModel> AllItems { get; private set; }

        public ICommand CancelScanCommand => this.cancelScanCommand ??= new RelayCommand(this.StopScan, prop => !this.folderTask.CancelRequested);

        public ICommand ExportCommand => this.exportCommand ??= new RelayCommand(this.Export);

        public string Path { get; }

        public PaginationViewModel Pagination { get; }

        public FoldersTableViewModel FoldersTable { get; private set; }

        public bool Equals(FolderViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Path == other.Path;
        }

        public ICommand SortCommand => this.sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public string GetExportSortColumn()
        {
            return this.GetActualSortColumn(this.SortColumn);
        }

        private void StopScan(object obj)
        {
            if (this.folderTask.IsBusy)
            {
                this.folderTask.Cancel();
            }
        }

        private void OnProgress(long count)
        {
            this.RowCount = count;
            this.Header.SetText(
                string.Format(
                    count == 1 ? FolderReportResource.FolderResultText : FolderReportResource.FolderResultsText,
                    string.Empty));
        }

        private async Task OnCompletedAsync(IEnumerable<IFolderReport> folderReports)
        {
            if (folderReports == null)
            {
                this.Header.ShowCancel = false;
                this.Header.SearchDisabled = true;

                this.Header.SetText(FolderReportResource.CancelledText, true);

                return;
            }

            IEnumerable<IFolderReport> reports = folderReports.ToList();
            IEnumerable<IFolderReport> list = reports.Where(m => m.Exception == null);

            this.AllItems = this.LimitData(list)
                .Select(m => new FolderItemViewModel
                {
                    FileCount = m.FileCount,
                    FileCountWithSubFolders = m.FileCountInclSub,
                    FullName = m.FullName,
                    Name = m.Name,
                    Owner = m.Owner,
                    Size = m.Size,
                    SizeWithSubFolders = m.SizeInclSub
                }).ToList();

            this.filteredItems = this.AllItems.ToList();

            this.Header.ShowCancel = false;
            this.Header.SearchDisabled = this.AllItems.Count == 0;

            await this.ChainTasksAsync(true);

            await Task.Run(() =>
            {
                IEnumerable<Exception> exceptions = reports.Where(m => m.Exception != null).Select(m => m.Exception);
                foreach (Exception ex in exceptions)
                {
                    this.logger.LogError(ex, "The operation completed with errors.");
                }
            });
        }

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task SearchAsync(object p)
        {
            await this.ChainTasksAsync(true);
        }

        private async Task RestartScanAsync(object p)
        {
            this.Header.SearchClearCommand.Execute(null);

            this.AllItems?.Clear();
            this.filteredItems?.Clear();
            this.FoldersTable?.Clear();
            this.Pagination.TotalRows = 0;

            await this.InitScanAsync();
        }

        private void Export(object _)
        {
            this.exportService.ExportFolderReports(new[] { this });
        }

        private async Task InitScanAsync()
        {
            this.DoProgress();

            this.RowCount = 0;
            this.Header.SetText(FolderReportResource.LoadingText);

            try
            {
                IEnumerable<IFolderReport> result = await this.folderTask.RunAsync(this.Path, this.OnProgress);
                await this.OnCompletedAsync(result);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to initialize scan for path ({Path}).", this.Path);
                this.Header.SetText(string.Empty, true);
                this.dialogService.ShowMessage($"Failed to initialize scan for path ({this.Path}).");
            }

            this.StopProgress();
        }

        private bool CreateFilter()
        {
            try
            {
                var text = string.Empty;
                var totalRows = 0;

                if (!string.IsNullOrEmpty(this.Header.SearchText))
                {
                    this.filteredItems.Clear();

                    IEnumerable<FolderItemViewModel> rows = this.AllItems.Where(this.CreateSearchExpression).ToList();

                    totalRows = rows.Count();

                    if (totalRows > 0)
                    {
                        this.filteredItems = rows.ToList();
                    }

                    text = string.Format(totalRows == 1 ? FolderReportResource.FolderSearchResultText : FolderReportResource.FolderSearchResultsText, totalRows, this.Header.SearchText);
                }
                else
                {
                    totalRows = this.AllItems?.Count ?? 0;
                    text = totalRows == 0 ? FolderReportResource.FolderEmptyText : string.Format(totalRows == 1 ? FolderReportResource.FolderResultText : FolderReportResource.FolderResultsText, totalRows);
                }

                this.Pagination.TotalRows = totalRows;
                this.Header.SetText(text, totalRows == 0);
                return true;
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to create text-filter due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                this.dialogService.ShowMessage($"{errorMessage}: {ex.Message}");
                return false;
            }
        }

        private async Task MakeResult()
        {
            if (this.AllItems == null || this.AllItems.Count == 0)
            {
                return;
            }

            await this.dispatcherService.InvokeAsync(() =>
            {
                try
                {
                    IEnumerable<FolderItemViewModel> pagedItems = string.IsNullOrEmpty(this.Header.SearchText) ? this.AllItems.AsEnumerable() : this.filteredItems.AsEnumerable();

                    if (this.SortDirection.Equals(SortOrder.Ascending))
                    {
                        pagedItems = pagedItems.OrderBy(this.GetActualSortColumn(this.SortColumn));
                    }
                    else if (this.SortDirection.Equals(SortOrder.Descending))
                    {
                        pagedItems = pagedItems.OrderByDescending(this.GetActualSortColumn(this.SortColumn));
                    }

                    pagedItems = (pagedItems ?? Array.Empty<FolderItemViewModel>()).ToList();
                    if (this.Pagination.PageSize > 1)
                    {
                        pagedItems = pagedItems.Skip((this.Pagination.CurrentPage - 1) * this.Pagination.PageSize).Take(this.Pagination.PageSize);
                    }

                    this.FoldersTable.Clear();
                    foreach (FolderItemViewModel item in pagedItems)
                    {
                        this.FoldersTable.AddRowFrom(item);
                    }

                    this.RaisePropertyChanged(nameof(this.FoldersTable));
                }
                catch (Exception e)
                {
                    const string errorMessage = "Failed to build result-set due to an unhandled error.";
                    this.logger.LogError(e, errorMessage);
                    this.dialogService.ShowMessage(errorMessage);
                }
            });
        }

        private async Task ChainTasksAsync(bool createFilter = false)
        {
            this.Header.ShowProgress();

            if (createFilter)
            {
                bool result = await Task.Run(this.CreateFilter);
                if (result)
                {
                    await Task.Run(this.MakeResult);
                }
            }
            else
            {
                await Task.Run(this.MakeResult);
            }

            this.Header.EndProgress();
        }

        private bool CreateSearchExpression(FolderItemViewModel m)
        {
            string searchText = this.Header.SearchText.ToLowerInvariant();
            return m.FullName.ToLowerInvariant().Contains(searchText) ||
                   (m.Owner?.ToLowerInvariant().Contains(searchText) ?? false) ||
                   m.Size.ToString(CultureInfo.InvariantCulture).Contains(searchText);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !this.disposed)
            {
                this.FoldersTable?.Dispose();
                this.disposed = true;
            }
        }

        private string GetActualSortColumn(string sortColumn)
        {
            return sortColumn switch
            {
                "SizeText" => "Size",
                "SizeWithSubFoldersText" => "SizeWithSubFolders",
                _ => sortColumn
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((FolderViewModel)obj);
        }

        public override int GetHashCode()
        {
            return this.Path != null ? this.Path.GetHashCode() : 0;
        }
    }
}