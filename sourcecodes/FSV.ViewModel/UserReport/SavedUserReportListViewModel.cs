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
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Core;
    using Events;
    using Microsoft.Extensions.Logging;
    using Passables;
    using Prism.Events;
    using Resources;

    /// <summary>
    ///     Manages a list of all saved reports fetched from database.
    /// </summary>
    public class SavedUserReportListViewModel : UserReportBaseViewModel, ISortable
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<SavedUserReportViewModel> logger;
        private readonly INavigationService navigationService;

        private readonly IUserReportService userReportService;
        private ICommand _compareReportsCommand;
        private ICommand _sortCommand;

        public SavedUserReportListViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IUserReportService userReportService,
            IConfigurationManager configurationManager,
            ILogger<SavedUserReportViewModel> logger,
            UserPath userPath = null)
            : base(userPath?.Path ?? string.Empty, userPath?.User ?? string.Empty)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.userReportService = userReportService ?? throw new ArgumentNullException(nameof(userReportService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.Closable = false;
            this.Exportable = false;

            // ReportTypeCaption = UserReport;
            this.ReportType = ReportType.AllSavedUser;

            this.DisplayName = UserReportResource.AllSavedReportsCaption;
            this.Reports = new ObservableCollection<SavedUserReportListItemViewModel>();
            this.SortDirection = SortOrder.Indeterminate;


            async Task<bool> CanExecuteOpenOrDeleteReport(object p)
            {
                bool result = this.Reports.Any(m => m.IsSelected);
                return await Task.FromResult(result);
            }

            this.OpenReportCommand = new RelayCommand(this.OpenReport, p => this.Reports.Any(m => m.IsSelected));
            this.DeleteReportCommand = new AsyncRelayCommand(this.DeleteReportAsync, CanExecuteOpenOrDeleteReport);

            this.SetHeader();

            this.eventAggregator.GetEvent<UserReportSavedEvent>().Subscribe(() => this.ChainTasksAsync().FireAndForgetSafeAsync());
            this.eventAggregator.GetEvent<SavedUserReportsDeletedEvent>().Subscribe(x => this.OnReportsRemovedAsync(x).FireAndForgetSafeAsync());
        }

        public ObservableCollection<SavedUserReportListItemViewModel> Reports { get; }

        public ICommand OpenReportCommand { get; }

        public ICommand DeleteReportCommand { get; }

        public ICommand CompareReportsCommand => this._compareReportsCommand ??= new RelayCommand(this.CompareReports, this.CanCompare);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string GetExportSortColumn()
        {
            return this.SortColumn;
        }

        public override async Task RefreshContentAsync()
        {
            await this.ChainTasksAsync();
        }

        public void Compare(SavedUserReportListItemViewModel item1, SavedUserReportListItemViewModel item2)
        {
            item1.IsSelected = true;
            item2.IsSelected = true;
            this.CompareReports(null);
            item1.IsSelected = false;
            item2.IsSelected = false;
        }

        private void SetHeader()
        {
            this.Pagination = new PaginationViewModel(this.configurationManager, this.PageChangeAsync, CommonResource.ReportsTitle);

            this.Header = new HeaderViewModel(this.Pagination)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync)
            };

            this.Header.SetText(UserReportResource.AllSavedReportsCaption);
        }

        private async Task SearchAsync(object O_0)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task PageChangeAsync(PageChangeMode mode)
        {
            await this.ChainTasksAsync();
        }

        private bool CanCompare(object obj)
        {
            return (this.Reports?.Where(m => m.IsSelected).Count() ?? 0) == 2;
        }

        private void CompareReports(object obj)
        {
            SavedUserReportListItemViewModel[] selectedItems = this.Reports.Where(m => m.IsSelected).OrderBy(m => m.Report.Date).ToArray();
            if (!selectedItems[0].Equals(selectedItems[1]))
            {
                this.dialogService.ShowMessage(UserReportResource.CompareDifferentReportsText);
                return;
            }

            try
            {
                this.navigationService.NavigateWithAsync<CompareUserReportViewModel>(new CompareObjects<SavedUserReportListItemViewModel>(selectedItems[0], selectedItems[1]));
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to compare selected reports due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                this.dialogService.ShowMessage(errorMessage);
            }
        }

        private void OpenReport(object o_0)
        {
            foreach (SavedUserReportListItemViewModel selectedItem in this.Reports.Where(m => m.IsSelected))
            {
                this.navigationService.NavigateWithAsync<SavedUserReportViewModel>(selectedItem.Report);
            }
        }

        private async Task DeleteReportAsync(object O__p)
        {
            if (!this.dialogService.AskRemove())
            {
                return;
            }

            List<SavedUserReportListItemViewModel> selectedItems = this.Reports.Where(m => m.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                return;
            }

            this.Header.ShowProgress();

            try
            {
                List<SavedUserReportListItemViewModel> removedItems = await this.userReportService.DeleteAsync(selectedItems);

                if (removedItems.Count != selectedItems.Count)
                {
                    this.dialogService.ShowMessage(UserReportResource.NotAllDeletedMessage);
                }

                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.eventAggregator.GetEvent<SavedUserReportsDeletedEvent>().Publish(removedItems);
                    this.Header.EndProgress();
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to delete the selected report due to an unhandled error.");
            }
        }

        private async Task ChainTasksAsync()
        {
            Task taskProgress = Task.Run(async () => await this.dispatcherService.InvokeAsync(() => this.Header.ShowProgress()));
            Task taskNext = taskProgress.ContinueWith(async t => await this.DoChangePageAsync());

            await taskNext.ContinueWith(async t => await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress()));
        }

        private async Task DoChangePageAsync()
        {
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            int start = this.Pagination.PageSize * (pageNo - 1);

            try
            {
                this.Header.SetText(CommonResource.LoadingText);
                ResultEnumerableViewModel<SavedUserReportListItemViewModel> savedReports = await this.GetListItemsAsync(this.Header.SearchText, this.SortColumn, this.SortDirection == SortOrder.Ascending, start, this.Pagination.PageSize);

                if (pageNo == 1)
                {
                    this.Pagination.TotalRows = this.LimitTotalRows(savedReports.Total);
                }

                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.Reports.Clear();

                    foreach (SavedUserReportListItemViewModel item in this.LimitData(savedReports.Result, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, savedReports.Total))
                    {
                        this.Reports.Add(item);
                    }

                    bool searched = !string.IsNullOrEmpty(this.Header.SearchText);

                    if (this.Reports.Count == 0)
                    {
                        this.Pagination.TotalRows = 0;
                        this.Header.SearchDisabled = !searched;

                        this.Header.SetText(!searched ? UserReportResource.ReportEmptyCaption : string.Format(UserReportResource.AllSavedReportSearchResultsCaption, savedReports.Total, this.Header.SearchText));
                    }
                    else
                    {
                        this.Header.SearchDisabled = false;
                        this.Header.SetText(
                            searched
                                ? string.Format(savedReports.Total == 1 ? UserReportResource.AllSavedReportSearchResultCaption : UserReportResource.AllSavedReportSearchResultsCaption, savedReports.Total, this.Header.SearchText)
                                : string.Format(savedReports.Total == 1 ? UserReportResource.AllSavedReportResultCaption : UserReportResource.AllSavedReportResultsCaption, savedReports.Total)
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to navigate to the requested report-page due to an unhandled error.");
                this.Header.SetText(ErrorResource.DatabaseUnavailable, true);
                this.Header.SearchDisabled = true;
            }
        }

        private async Task OnReportsRemovedAsync(IEnumerable<SavedUserReportListItemViewModel> obj)
        {
            await this.ChainTasksAsync();
        }

        /// <summary>
        ///     Override this to return application search result items from database.
        /// </summary>
        /// <param name="searchKey"></param>
        /// <param name="sortKey"></param>
        /// <param name="ascending"></param>
        /// <param name="skip"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        protected virtual async Task<ResultEnumerableViewModel<SavedUserReportListItemViewModel>> GetListItemsAsync(string searchKey, string sortKey, bool ascending, int skip, int pageSize)
        {
            return await this.userReportService.GetAll(this.Header.SearchText, this.SortColumn, ascending, skip, this.Pagination.PageSize);
        }

        private async Task FillReportsAsync(ResultEnumerableViewModel<SavedUserReportListItemViewModel> savedReports)
        {
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            int start = this.Pagination.PageSize * (pageNo - 1);

            if (pageNo == 1)
            {
                this.Pagination.TotalRows = this.LimitTotalRows(savedReports.Total);
            }

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Reports.Clear();

                foreach (SavedUserReportListItemViewModel item in this.LimitData(savedReports.Result, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, savedReports.Total))
                {
                    this.Reports.Add(item);
                }

                bool searched = !string.IsNullOrEmpty(this.Header.SearchText);

                if (this.Reports.Count == 0)
                {
                    this.Pagination.TotalRows = 0;
                    this.Header.SearchDisabled = !searched;

                    this.Header.SetText(!searched ? UserReportResource.ReportEmptyCaption : string.Format(UserReportResource.AllSavedReportSearchResultsCaption, savedReports.Total, this.Header.SearchText), true);
                }
                else
                {
                    this.Header.SearchDisabled = false;
                    this.Header.SetText(
                        searched
                            ? string.Format(savedReports.Total == 1 ? UserReportResource.AllSavedReportSearchResultCaption : UserReportResource.AllSavedReportSearchResultsCaption, savedReports.Total, this.Header.SearchText)
                            : string.Format(savedReports.Total == 1 ? UserReportResource.AllSavedReportResultCaption : UserReportResource.AllSavedReportResultsCaption, savedReports.Total)
                    );
                }
            });
        }

        public async Task<SavedUserReportListViewModel> InitializeAsync(ResultEnumerableViewModel<SavedUserReportListItemViewModel> resultViewModel = null)
        {
            if (resultViewModel == null)
            {
                await this.ChainTasksAsync();
            }
            else
            {
                await this.FillReportsAsync(resultViewModel);
            }

            return this;
        }
    }
}