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

namespace FSV.ViewModel.Owner
{
    using System;
    using System.Collections.Generic;
    using System.Data;
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
    using Exporter;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Passables;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class OwnerReportViewModel : LimitDataViewModel, ISortable, IEquatable<OwnerReportViewModel>
    {
        private readonly IDialogService dialogService;

        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IFolderTask folderTask;
        private readonly ILogger<OwnerReportViewModel> logger;
        private ICommand cancelScanCommand;
        private bool disposed;
        private ICommand exportCommand;
        private ICommand sortCommand;

        public OwnerReportViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IFolderTask folderTask,
            IConfigurationManager configurationManager,
            ILogger<OwnerReportViewModel> logger,
            IExportService exportService,
            UserPath userPath)
        {
            if (userPath == null)
            {
                throw new ArgumentNullException(nameof(userPath));
            }

            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.folderTask = folderTask ?? throw new ArgumentNullException(nameof(folderTask));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            this.ReportType = ReportType.Owner;
            this.ReportTypeCaption = OwnerReport;
            this.Title = userPath.Path;
            this.FolderPath = userPath.Path;
            this.DisplayName = OwnerReport;
            this.UserName = userPath.User;

            this.Pagination = new PaginationViewModel(configurationManager, this.PageChangeAsync, FolderReportResource.PaginationFoldersCaption);

            this.Header = new HeaderViewModel(this.Pagination)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync),
                RefreshCommand = new AsyncRelayCommand(this.RestartScan)
            };

            this.FoldersTable = this.GetDataTable();

            this.SortColumn = this.FoldersTable.Columns[1].ColumnName;
            this.SortDirection = SortOrder.Ascending;

            this.InitScanAsync().FireAndForgetSafeAsync();
        }

        public IList<FolderItemViewModel> AllItems { get; private set; }

        public ICommand CancelScanCommand => this.cancelScanCommand ??= new RelayCommand(this.StopScan, p => !this.folderTask.CancelRequested);

        public ICommand ExportCommand => this.exportCommand ??= new RelayCommand(this.Export);

        public PaginationViewModel Pagination { get; }

        public DataTable FoldersTable { get; }

        internal string UserName { get; }

        private IList<FolderItemViewModel> FilteredItems { get; set; }

        public bool Equals(OwnerReportViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.FolderPath == other.FolderPath && this.UserName == other.UserName;
        }

        public ICommand SortCommand => this.sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public string GetExportSortColumn()
        {
            return this.GetActualSortColumn(this.SortColumn);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.FoldersTable?.Dispose();
                this.disposed = true;
            }
        }

        private async Task PageChangeAsync(PageChangeMode mode)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task SearchAsync(object p)
        {
            await this.ChainTasksAsync(true);
        }

        private void StopScan(object _)
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
                count == 1 ? OwnerReportResource.LoadingFolderText : OwnerReportResource.LoadingFoldersText);
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
            IEnumerable<IFolderReport> list = reports.Where(m => m.Exception == null).ToList();

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

            this.FilteredItems = this.AllItems.ToList();

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

        private void CreateFilter()
        {
            var text = string.Empty;
            var totalRows = 0;

            if (!string.IsNullOrEmpty(this.Header.SearchText))
            {
                this.FilteredItems.Clear();

                IEnumerable<FolderItemViewModel> rows = this.AllItems.Where(this.CreateSearchExpression).ToList();

                totalRows = rows.Count();
                if (totalRows > 0)
                {
                    this.FilteredItems = rows.ToList();
                }

                text = string.Format(totalRows == 1 ? OwnerReportResource.FolderSearchResultText : OwnerReportResource.FolderSearchResultsText, totalRows, this.UserName, this.Header.SearchText);
            }
            else
            {
                totalRows = this.AllItems?.Count ?? 0;
                text = string.Format(totalRows == 1 ? OwnerReportResource.FolderResultText : OwnerReportResource.FolderResultsText, totalRows, this.UserName);
            }

            this.Pagination.TotalRows = totalRows;
            this.Header.SetText(text, totalRows == 0);
        }

        private async Task MakeResultAsync()
        {
            if (this.AllItems == null || this.AllItems.Count == 0)
            {
                return;
            }

            await this.dispatcherService.InvokeAsync(() =>
            {
                IEnumerable<FolderItemViewModel> pagedItems = string.IsNullOrEmpty(this.Header.SearchText) ? this.AllItems.AsEnumerable() : this.FilteredItems.AsEnumerable();

                if (this.SortDirection.Equals(SortOrder.Ascending))
                {
                    pagedItems = pagedItems.OrderBy(this.GetActualSortColumn(this.SortColumn));
                }

                if (this.SortDirection.Equals(SortOrder.Descending))
                {
                    pagedItems = pagedItems.OrderByDescending(this.GetActualSortColumn(this.SortColumn));
                }

                if (this.Pagination.PageSize > 0)
                {
                    pagedItems = pagedItems.Skip((this.Pagination.CurrentPage - 1) * this.Pagination.PageSize).Take(this.Pagination.PageSize);
                }

                this.FoldersTable.Clear();

                foreach (FolderItemViewModel item in pagedItems)
                {
                    DataRow row = this.FoldersTable.NewRow();
                    row["FullName"] = item.FullName;
                    row["Name"] = item.Name;
                    row["Owner"] = item.Owner;

                    this.FoldersTable.Rows.Add(row);
                }

                this.RaisePropertyChanged(nameof(this.FoldersTable));
            });
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

        private bool CreateSearchExpression(FolderItemViewModel m)
        {
            string searchText = this.Header.SearchText.ToLower();
            return m.FullName.ToLower().Contains(searchText) ||
                   m.Owner.ToLower().Contains(searchText) ||
                   m.Size.ToString(CultureInfo.InvariantCulture).Contains(searchText);
        }

        private DataTable GetDataTable()
        {
            var table = new DataTable("FoldersTable");
            table.Columns.Add(new DataColumn("Name"));
            table.Columns.Add(new DataColumn("FullName"));
            table.Columns.Add(new DataColumn("Owner"));

            return table;
        }

        private string GetActualSortColumn(string sortColumn)
        {
            switch (sortColumn)
            {
                case "SizeText":
                    return "Size";
                case "SizeWithSubFoldersText":
                    return "SizeInclSub";
                case "FileCountWithSubFolders":
                    return "FileCountInclSub";
                default:
                    return sortColumn;
            }
        }

        private async Task InitScanAsync()
        {
            this.DoProgress();

            this.RowCount = 0;
            this.Header.SetText(FolderReportResource.LoadingText);

            try
            {
                IEnumerable<IFolderReport> result = await this.folderTask.RunAsync(this.FolderPath, this.UserName, this.OnProgress);
                await this.OnCompletedAsync(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to initialize scan for folder {Path} and user ({User}) due to an unhandled error.", this.FolderPath, this.UserName);
                this.Header.SetText(string.Empty, true);
                this.dialogService.ShowMessage($"Failed to initialize scan for folder {this.FolderPath} and user ({this.UserName}) due to an unhandled error.");
            }

            this.StopProgress();
        }

        private void Export(object _)
        {
            this.exportService.ExportOwnerReports(new[] { this });
        }

        private async Task ExportReportAsync(ExporterBase exporter)
        {
            await exporter.ExportAsync(new[] { this });
        }

        private async Task RestartScan(object p)
        {
            this.Header.SearchClearCommand.Execute(null);

            this.AllItems?.Clear();
            this.FilteredItems?.Clear();
            this.FoldersTable?.Rows.Clear();

            this.Pagination.TotalRows = 0;

            await this.InitScanAsync();
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

            return this.Equals((OwnerReportViewModel)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.FolderPath, this.UserName);
        }
    }
}