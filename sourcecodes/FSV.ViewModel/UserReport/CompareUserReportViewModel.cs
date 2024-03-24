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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Compare;
    using Configuration.Abstractions;
    using Core;
    using Passables;
    using Resources;

    public class CompareUserReportViewModel : UserReportBaseViewModel, ISortable
    {
        private readonly ICompareService compareService;
        private readonly IConfigurationManager configurationManager;
        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly SavedUserReportListItemViewModel newReport;

        private readonly SavedUserReportListItemViewModel oldReport;
        private ICommand _exportCommand;
        private int _selectedFilter = -1;
        private ICommand _sortCommand;

        public CompareUserReportViewModel(
            ICompareService compareService,
            IDispatcherService dispatcherService,
            IConfigurationManager configurationManager,
            IExportService exportService,
            CompareObjects<SavedUserReportListItemViewModel> compareObjects) :
            base(compareObjects?.Instance?.FolderPath, compareObjects?.Instance?.ReportUser)
        {
            this.compareService = compareService ?? throw new ArgumentNullException(nameof(compareService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            if (compareObjects == null)
            {
                throw new ArgumentNullException(nameof(compareObjects));
            }

            if (!compareObjects.Instance.Report.Folder.Equals(compareObjects.CompareWith.Report.Folder, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException(ErrorResource.CompareFailedDifferentFolder);
            }

            this.oldReport = compareObjects.Instance;
            this.newReport = compareObjects.CompareWith;

            this.SortColumn = "CompleteName";
            this.SortDirection = SortOrder.Indeterminate;

            this.ReportType = ReportType.CompareUser;
            this.ReportTypeCaption = UserReport;
            this.Title = string.Format(UserReportResource.CompareReportTitle, this.UserName, this.FolderPath);
            this.DisplayName = UserReportResource.CompareReportDisplayName;

            this.Filters = new CompareOptions();
            this.ComparedList = new ObservableCollection<CompareUserReportItemViewModel>();

            this.Pagination = new PaginationViewModel(this.configurationManager, async p => await this.ChainTasksAsync());
            this.Header = new HeaderViewModel(this.Pagination);

            this.Header.SetText(
                string.Format(
                    UserReportResource.CompareReportCaption,
                    this.oldReport.Date,
                    this.newReport.Date,
                    this.UserName));

            this.ChainTasksAsync(true).FireAndForgetSafeAsync();
        }

        private IList<CompareUserReportItemViewModel> InternalComparedList { get; set; }

        public int SelectedFilter
        {
            get => this._selectedFilter;
            set
            {
                this.Set(ref this._selectedFilter, value, nameof(this.SelectedFilter));
                this.ChainTasksAsync().FireAndForgetSafeAsync();
            }
        }

        public IList<CompareUserReportItemViewModel> ComparedList { get; }
        public IDictionary<int, string> Filters { get; }

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export);

        public override DataTable AllFolders
        {
            get => this.GetDataTableForExport();
            protected set => base.AllFolders = value;
        }

        public string SortColumn { get; set; }
        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(p => this.ChainTasksAsync());

        public string GetExportSortColumn()
        {
            switch (this.SortColumn)
            {
                case "CompleteName":
                    return ExportResource.UserReportCompareColumnNameCaption;
                case "OldPermission":
                    return ExportResource.UserReportCompareColumnOldReportCaption;
                case "NewPermission":
                    return ExportResource.UserReportCompareColumnNewReportCaption;
                default:
                    return this.SortColumn;
            }
        }

        private async Task Compare()
        {
            this.InternalComparedList = await this.compareService.Compare(this.oldReport.Id, this.newReport.Id);
        }

        private async Task ChainTasksAsync(bool compare = false)
        {
            await Task.Run(async () => await this.dispatcherService.InvokeAsync(() => this.Header.ShowProgress()));

            Task taskTwo;
            if (compare)
            {
                taskTwo = this.Compare().ContinueWith(t => this.MakeResultAsync(this.GetList()));
            }
            else
            {
                taskTwo = Task.Run(async () => await this.MakeResultAsync(this.GetList()));
            }

            await taskTwo.ContinueWith(async t => await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress()));
        }

        private async Task MakeResultAsync(IEnumerable<CompareUserReportItemViewModel> theList)
        {
            bool isAscending = this.SortDirection == SortOrder.Ascending;

            int skip, end;

            if (this.Pagination.PageSize > 0)
            {
                end = this.Pagination.PageSize;
                skip = this.Pagination.PageSize * (this.Pagination.CurrentPage - 1);
            }
            else
            {
                end = this.Pagination.TotalRows;
                skip = 0;
            }

            if (this.SortColumn == "State")
            {
                theList = isAscending ? theList.OrderBy(m => m.State.ToString()) : theList.OrderByDescending(m => m.State.ToString());
            }
            else if (this.SortColumn == "CompleteName")
            {
                theList = isAscending ? theList.OrderBy(m => m.CompleteName) : theList.OrderByDescending(m => m.CompleteName);
            }
            else if (this.SortColumn == "OldPermission")
            {
                theList = isAscending ? theList.OrderBy(m => m.OldPermission) : theList.OrderByDescending(m => m.OldPermission);
            }
            else if (this.SortColumn == "NewPermission")
            {
                theList = isAscending ? theList.OrderBy(m => m.NewPermission) : theList.OrderByDescending(m => m.NewPermission);
            }

            theList = theList.Skip(skip).Take(end);

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.ComparedList.Clear();
                foreach (CompareUserReportItemViewModel item in theList)
                {
                    this.ComparedList.Add(item);
                }
            });
        }

        private IEnumerable<CompareUserReportItemViewModel> GetList()
        {
            if (this.InternalComparedList != null)
            {
                IEnumerable<CompareUserReportItemViewModel> theList = null;
                if (this.SelectedFilter == CompareOption.All)
                {
                    theList = this.InternalComparedList;
                }
                else if (this.SelectedFilter == CompareOption.NotSimilar)
                {
                    theList = this.InternalComparedList.Where(m => m.State != CompareState.Similar);
                }
                else
                {
                    theList = this.InternalComparedList.Where(m => m.State == (CompareState)this.SelectedFilter);
                }

                int count = theList.Count();
                if (this.Pagination.TotalRows != count)
                {
                    this.Pagination.TotalRows = count;
                }

                return theList;
            }

            return new List<CompareUserReportItemViewModel>(0);
        }

        private DataTable GetDataTableForExport()
        {
            var table = new DataTable();
            table.Columns.Add(ExportResource.UserReportCompareColumnNameCaption);
            table.Columns.Add(ExportResource.UserReportCompareColumnOldReportCaption);
            table.Columns.Add(ExportResource.UserReportCompareColumnNewReportCaption);
            table.Columns.Add(ExportResource.UserReportCompareColumnTextCaption);
            table.Columns.Add(ExportResource.UserReportCompareColumnStateCaption);

            // Not using Parallel.ForEach because it is causing concurrency error (DataTable internal index is corrupt).
            foreach (CompareUserReportItemViewModel report in this.InternalComparedList)
            {
                DataRow dataRow = table.NewRow();
                dataRow[ExportResource.UserReportCompareColumnNameCaption] = report.CompleteName;
                dataRow[ExportResource.UserReportCompareColumnOldReportCaption] = report.OldPermission;
                dataRow[ExportResource.UserReportCompareColumnNewReportCaption] = report.NewPermission;
                dataRow[ExportResource.UserReportCompareColumnTextCaption] = report.Text;
                dataRow[ExportResource.UserReportCompareColumnStateCaption] = report.State.ToString();

                table.Rows.Add(dataRow);
            }

            return table;
        }

        private void Export(object _)
        {
            this.exportService.ExportUserReports(new[] { this });
        }
    }
}