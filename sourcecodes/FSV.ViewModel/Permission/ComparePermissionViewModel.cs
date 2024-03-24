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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Compare;
    using Configuration.Abstractions;
    using Core;
    using Passables;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ComparePermissionViewModel : PermissionReportBaseViewModel, ISortable
    {
        private readonly SavedReportItemViewModel _newerPermission;
        private readonly SavedReportItemViewModel _olderPermission;
        private readonly IConfigurationManager configurationManager;
        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IPermissionReportManager reportManager;
        private ICommand _exportCommand;
        private int _selectedFilter = -1;

        private ICommand _sortCommand;

        public ComparePermissionViewModel(
            IDispatcherService dispatcherService,
            IPermissionReportManager reportManager,
            IConfigurationManager configurationManager,
            IExportService exportService,
            CompareObjects<SavedReportItemViewModel> permissions) : base(permissions?.Instance?.SelectedFolderPath)
        {
            if (permissions is null)
            {
                throw new ArgumentNullException(nameof(permissions));
            }

            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            if (!permissions.Instance.Report.Folder.Equals(permissions.CompareWith.Report.Folder, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException(ErrorResource.CompareFailedDifferentFolder);
            }

            this._olderPermission = permissions.Instance;
            this._newerPermission = permissions.CompareWith;

            this.ReportType = ReportType.ComparePermission;
            this.ReportTypeCaption = PermissionReport;
            this.DisplayName = PermissionCompareResource.CompareReportTitle;
            this.Title = $"{PermissionCompareResource.CompareReportTitle} - {this._olderPermission.SelectedFolderPath}";

            this.InternalComparedList = new List<ComparePermissionItemViewModel>();
            this.ComparedList = new ObservableCollection<ComparePermissionItemViewModel>();

            async Task PageChangeAsyncCallback(PageChangeMode m)
            {
                await this.ChainTasksAsync();
            }

            this.Pagination = new PaginationViewModel(this.configurationManager, PageChangeAsyncCallback)
            {
                ShowText = PermissionResource.UsersCaption
            };

            this.Header = new HeaderViewModel(this.Pagination);
            this.Header.SetText(
                string.Format(
                    PermissionCompareResource.PermissionCompareCaption,
                    this._olderPermission.Date,
                    this._newerPermission.Date));

            this.SortDirection = SortOrder.Indeterminate;
            this.Filters = new CompareOptions();

            this.ChainTasksAsync(true).FireAndForgetSafeAsync();
        }

        private IList<ComparePermissionItemViewModel> InternalComparedList { get; }

        public ObservableCollection<ComparePermissionItemViewModel> ComparedList { get; }

        public IDictionary<int, string> Filters { get; }

        public int SelectedFilter
        {
            get => this._selectedFilter;
            set
            {
                this.Set(ref this._selectedFilter, value, nameof(this.SelectedFilter));
                this.ChainTasksAsync().FireAndForgetSafeAsync();
            }
        }

        public override DataTable AllPermissions
        {
            get => this.GetDataTableForExport();

            protected set => base.AllPermissions = value;
        }

        public PaginationViewModel Pagination { get; }

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(async x => await this.ChainTasksAsync());

        public string GetExportSortColumn()
        {
            return this.SortColumn switch
            {
                "OldPermission" => "OldReport",
                "NewPermission" => "NewReport",
                _ => this.SortColumn
            };
        }

        private async Task ChainTasksAsync(bool compare = false)
        {
            Task taskProgress = Task.Run(async () => await this.dispatcherService.InvokeAsync(() => this.Header.ShowProgress()));

            Task taskNext;
            if (compare)
            {
                taskNext = taskProgress.ContinueWith(t => this.Compare())
                    .ContinueWith(async t =>
                    {
                        IEnumerable<ComparePermissionItemViewModel> list = this.GetPermissionItemsList();
                        await this.MakeResultAsync(list);
                    });
            }
            else
            {
                taskNext = taskProgress.ContinueWith(async t =>
                {
                    IEnumerable<ComparePermissionItemViewModel> list = this.GetPermissionItemsList();
                    await this.MakeResultAsync(list);
                });
            }

            await taskNext.ContinueWith(async t => await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress()));
        }

        private void Compare()
        {
            var total = 0;

            var oldReportItems = this.reportManager.GetAll(this._olderPermission.Report.Id, -1, 0, out total)
                .OrderBy(m => m.Detail.Id)
                .GroupBy(m => m.AccountName)
                .Select(group => new { Group = group, Count = group.Count() })
                .SelectMany(groupWithCount =>
                    groupWithCount.Group.Zip(
                        Enumerable.Range(1, groupWithCount.Count),
                        (oldReport, rowNumber) => new { Report = oldReport, RowNumber = rowNumber }
                    )
                );

            var newReportItems = this.reportManager.GetAll(this._newerPermission.Report.Id, -1, 0, out total)
                .OrderBy(m => m.Detail.Id)
                .GroupBy(m => m.AccountName)
                .Select(group => new { Group = group, Count = group.Count() })
                .SelectMany(groupWithCount =>
                    groupWithCount.Group.Zip(
                        Enumerable.Range(1, groupWithCount.Count),
                        (newReport, rowNumber) => new { Report = newReport, RowNumber = rowNumber }
                    )
                );

            IEnumerable<JoinReport> olderJoin = from oldReport in oldReportItems
                join newReport in newReportItems on
                    new { oldReport.Report.AccountName, oldReport.RowNumber } equals new { AccountName = newReport?.Report.AccountName ?? null, RowNumber = newReport?.RowNumber ?? 0 } into nrp
                from newReport in nrp.DefaultIfEmpty()
                select new JoinReport(oldReport.Report.AccountName, oldReport?.Report, newReport?.Report, oldReport.RowNumber);

            IEnumerable<JoinReport> newerJoin = from newReport in newReportItems
                join oldReport in oldReportItems on
                    new { newReport.Report.AccountName, newReport.RowNumber } equals new { AccountName = oldReport?.Report.AccountName ?? null, RowNumber = oldReport?.RowNumber ?? 0 } into orp
                from oldReport in orp.DefaultIfEmpty()
                select new JoinReport(newReport.Report.AccountName, oldReport?.Report, newReport?.Report, newReport.RowNumber);

            Parallel.ForEach(olderJoin.Union(newerJoin, new JoinReportEqualityComparer()), async report => await this.FillReportAsync(report));
        }

        private async Task MakeResultAsync(IEnumerable<ComparePermissionItemViewModel> theList)
        {
            // if (InternalComparedList?.Count == 0) return;

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
            else if (this.SortColumn == "AccountName")
            {
                theList = isAscending ? theList.OrderBy(m => m.AccountName) : theList.OrderByDescending(m => m.AccountName);
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
                foreach (ComparePermissionItemViewModel item in theList)
                {
                    this.ComparedList.Add(item);
                }
            });
        }

        private async Task FillReportAsync(JoinReport report)
        {
            var comparedItem = new ComparePermissionItemViewModel
            {
                AccountName = report.AccountName,
                OldPermission = report.OldReport?.Permissions ?? string.Empty,
                NewPermission = report.NewReport?.Permissions ?? string.Empty
            };

            if (report.NewReport == null)
            {
                comparedItem.State = CompareState.Removed;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionRemovedText, report.AccountName);
            }
            else if (report.OldReport == null)
            {
                comparedItem.State = CompareState.Added;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionAddedText, report.AccountName, report.NewReport.Permissions);
            }
            else if (report.OldReport.Permissions.Equals(report.NewReport.Permissions))
            {
                comparedItem.State = CompareState.Similar;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionUnchangedText, report.AccountName, report.NewReport.Permissions);
            }
            else
            {
                comparedItem.State = CompareState.Changed;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionChangedText, report.AccountName, report.NewReport.Permissions);
            }

            await this.dispatcherService.InvokeAsync(() => this.InternalComparedList.Add(comparedItem));
        }

        private DataTable GetDataTableForExport()
        {
            var table = new DataTable();
            table.Columns.Add("AccountName");
            table.Columns.Add("OldReport");
            table.Columns.Add("NewReport");
            table.Columns.Add("Text");
            table.Columns.Add("State");

            // Not using Parallel.ForEach because it is causing concurrency error (DataTable internal index is corrupt).
            foreach (ComparePermissionItemViewModel report in this.InternalComparedList)
            {
                lock (table)
                {
                    DataRow dataRow = table.NewRow();
                    dataRow["AccountName"] = report.AccountName;
                    dataRow["OldReport"] = report.OldPermission;
                    dataRow["NewReport"] = report.NewPermission;
                    dataRow["Text"] = report.Text;
                    dataRow["State"] = report.State.ToString();

                    table.Rows.Add(dataRow);
                }
            }

            return table;
        }

        private IEnumerable<ComparePermissionItemViewModel> GetPermissionItemsList()
        {
            if (this.InternalComparedList != null)
            {
                IEnumerable<ComparePermissionItemViewModel> theList = null;
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

            return new List<ComparePermissionItemViewModel>(0);
        }

        private void Export(object p)
        {
            this.exportService.ExportPermissionReports(new[] { this });
        }

        private class JoinReport
        {
            public JoinReport(string accountName, SavedReportDetailItemViewModel oldReport, SavedReportDetailItemViewModel newReport, int rowNumber)
            {
                this.AccountName = accountName;
                this.OldReport = oldReport;
                this.NewReport = newReport;
                this.RowNumber = rowNumber;
            }

            public string AccountName { get; }
            public SavedReportDetailItemViewModel OldReport { get; }
            public SavedReportDetailItemViewModel NewReport { get; }
            public int RowNumber { get; }
        }

        private class JoinReportEqualityComparer : IEqualityComparer<JoinReport>
        {
            public bool Equals(JoinReport x, JoinReport y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return x.AccountName == y.AccountName && x.RowNumber == y.RowNumber;
            }

            public int GetHashCode(JoinReport obj)
            {
                return HashCode.Combine(obj.AccountName, obj.RowNumber);
            }
        }
    }
}