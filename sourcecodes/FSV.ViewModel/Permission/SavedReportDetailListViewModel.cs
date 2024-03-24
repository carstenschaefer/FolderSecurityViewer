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
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Database.Models;
    using Resources;

    /// <summary>
    ///     Represents a saved permission and its detail list in separated report tab.
    /// </summary>
    public class SavedReportDetailListViewModel : PermissionReportBaseViewModel, ISortable, IEquatable<SavedReportDetailListViewModel>
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly ModelBuilder<GridMetadataModel> gridMetadataModelBuilder;

        private readonly PermissionReport report;
        private readonly string reportFolder;
        private readonly int reportId;

        private readonly IPermissionReportManager reportManager;
        private DataTable _allPermissions;
        private ICommand _exportCommand;
        private ICommand _sortCommand;
        private bool disposed;
        private GridMetadataModel view;

        public SavedReportDetailListViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IPermissionReportManager reportManager,
            IConfigurationManager configurationManager,
            IExportService exportService,
            ModelBuilder<GridMetadataModel> gridMetadataModelBuilder,
            PermissionReport report) :
            base(report?.Folder)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.gridMetadataModelBuilder = gridMetadataModelBuilder ?? throw new ArgumentNullException(nameof(gridMetadataModelBuilder));
            this.report = report ?? throw new ArgumentNullException(nameof(report));

            this.ReportType = ReportType.SavedPermission;
            this.ReportTypeCaption = PermissionReport;

            this.reportId = report.Id;
            this.reportFolder = report.Folder;
            this.Date = report.Date.ToString("g");
            this.ExportDate = report.Date;
            this.SortDirection = SortOrder.Indeterminate;

            this.Pagination = new PaginationViewModel(this.configurationManager, this.ChangePageAsync, PermissionResource.UsersCaption);

            this.Header = new HeaderViewModel(this.Pagination)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync)
            };

            this.Title = string.Format(PermissionResource.SavedReportTitle, this.report.Folder, this.report.Date.ToString("g"));
            this.DisplayName = string.Format(PermissionResource.SavedReportDisplayName, this.report.Date.ToString("g"));

            this.ChainTasksAsync().FireAndForgetSafeAsync();
        }

        public GridMetadataModel GridMetadata => this.view ??= this.gridMetadataModelBuilder.Build();

        public string User => this.report.User;

        public string Date { get; }

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export);

        public override DataTable AllPermissions => this._allPermissions ??= this.reportManager.GetAll(this.report.Id);

        public ObservableCollection<SavedReportDetailItemViewModel> DetailList { get; } = new();

        public PaginationViewModel Pagination { get; }

        public bool Equals(SavedReportDetailListViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.reportFolder == other.reportFolder && this.reportId == other.reportId && this.Date == other.Date;
        }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public string GetExportSortColumn()
        {
            // The list bound to grid is type of SavedReportDetailItemViewModel.
            // The list exporting is DataTable. Check overridden AllPermissions property. It fetches all records from database for export.
            // The columns in AllPermissions DataTable are filled based on the names set in configuration.
            // Properties in SavedReportDetailItemViewModel are mapped with original column name. For example, AccountName is mapped with sAMAccountName.
            // So figure out the sort name from original column name mapped with property.

            PropertyInfo propertyInfo = typeof(SavedReportDetailItemViewModel).GetProperty(this.SortColumn);
            object mapAttribute = propertyInfo?.GetCustomAttributes(typeof(MapAtttribute), false).FirstOrDefault();
            if (mapAttribute is MapAtttribute mappedAttribute)
            {
                ConfigRoot configRoot = this.configurationManager.ConfigRoot;
                Report configRootReport = configRoot.Report;
                ReportTrustee reportTrustee = configRootReport.Trustee;
                return reportTrustee.TrusteeGridColumns.FirstOrDefault(m => m.Name.Equals(mappedAttribute.Name))?.DisplayName ?? this.SortColumn;
            }

            return this.SortColumn;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !this.disposed)
            {
                this._allPermissions?.Dispose();
                this.disposed = true;
            }
        }

        private async Task MakeResultAsync()
        {
            int end, start;
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            if (this.Pagination.PageSize > 0)
            {
                end = this.Pagination.PageSize;
                start = this.Pagination.PageSize * (pageNo - 1);
            }
            else
            {
                end = this.Pagination.TotalRows;
                start = 0;
            }

            IEnumerable<SavedReportDetailItemViewModel> details = this.reportManager.GetAll(this.report.Id, this.Header.SearchText, this.SortColumn, this.SortDirection.Equals(SortOrder.Ascending), start, end, out int total);

            if (pageNo == 1)
            {
                this.Pagination.TotalRows = this.LimitTotalRows(total);
            }

            this.Pagination.TotalRows = this.LimitTotalRows(total);

            details = this.LimitData(details, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, total);

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.DetailList.Clear();

                foreach (SavedReportDetailItemViewModel item in details)
                {
                    this.DetailList.Add(item);
                }

                if (this.DetailList.Count == 0)
                {
                    this.Header.SearchDisabled = string.IsNullOrEmpty(this.Header.SearchText);
                    this.Pagination.TotalRows = 0;
                }

                this.Header.SetText(
                    (string.IsNullOrEmpty(this.Header.SearchText) ? string.Empty : string.Format(PermissionResource.AllSavedReportSearchedText, this.Header.SearchText)) +
                    string.Format(total == 1 ? PermissionResource.SavedRowCountCaption : PermissionResource.SavedRowsCountCaption, total));

                this.RaisePropertyChanged(() => this.DetailList);
            });
        }

        private async Task ChainTasksAsync()
        {
            await Task.Run(async () => await this.dispatcherService.InvokeAsync(() => this.Header.ShowProgress()))
                .ContinueWith(async t => { await this.MakeResultAsync(); })
                .ContinueWith(async t => { await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress()); });
        }

        private async Task ChangePageAsync(PageChangeMode mode)
        {
            await this.ChainTasksAsync();
        }

        private async Task SearchAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private void Export(object p)
        {
            this.exportService.ExportPermissionReports(new[] { this });
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

            return this.Equals((SavedReportDetailListViewModel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.reportFolder != null ? this.reportFolder.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ this.reportId;
                hashCode = (hashCode * 397) ^ (this.Date != null ? this.Date.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}