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
    ///     Represents all saved permissions fetched from database.
    /// </summary>
    public class AllSavedReportListViewModel : PermissionReportBaseViewModel, ISortable
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<AllSavedReportListViewModel> logger;
        private readonly INavigationService navigationService;

        private readonly IPermissionReportManager reportManager;
        private ICommand _sortCommand;

        public AllSavedReportListViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IPermissionReportManager reportManager,
            IConfigurationManager configurationManager,
            ILogger<AllSavedReportListViewModel> logger)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.ReportType = ReportType.AllSavedPermission;

            this.DisplayName = PermissionResource.AllSavedReportsCaption;
            this.Reports = new ObservableCollection<SavedReportItemViewModel>();
            this.Closable = false;
            this.Exportable = false;

            this.OpenCommand = new RelayCommand(this.Open, p => this.Reports.Any(m => m.IsSelected));

            async Task<bool> CanExecuteDeleteAsync(object p)
            {
                bool result = this.Reports.Any(m => m.IsSelected);
                return await Task.FromResult(result);
            }

            this.DeleteCommand = new AsyncRelayCommand(this.DeleteAsync, CanExecuteDeleteAsync);
            this.CompareCommand = new AsyncRelayCommand(this.CompareAsync, this.CanCompareAsync);

            this.SortDirection = SortOrder.Indeterminate;

            this.SetHeader();
            this.SetEvents();
        }

        public bool Encrypted { get; set; }

        public ObservableCollection<SavedReportItemViewModel> Reports { get; }

        public ICommand OpenCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand ChangePageCommand { get; }

        public ICommand CompareCommand { get; }

        public PaginationViewModel Pagination { get; private set; }

        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public string GetExportSortColumn()
        {
            return null;
        }

        public async Task CompareAsync(SavedReportItemViewModel item1, SavedReportItemViewModel item2)
        {
            if (item1 == null)
            {
                throw new ArgumentNullException(nameof(item1));
            }

            if (item2 == null)
            {
                throw new ArgumentNullException(nameof(item2));
            }

            item1.IsSelected = true;
            item2.IsSelected = true;
            await this.CompareAsync(null);
            item1.IsSelected = false;
            item2.IsSelected = false;
        }

        internal async Task<AllSavedReportListViewModel> InitializeAsync(ResultEnumerableViewModel<SavedReportItemViewModel> resultViewModel = null)
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

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task ChangePageAsync(PageChangeMode mode)
        {
            await this.ChainTasksAsync();
        }

        private async Task SearchAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task DoChangePageAsync()
        {
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            int start = this.Pagination.PageSize * (pageNo - 1);

            try
            {
                this.Header.SetText(CommonResource.LoadingText);
                IList<SavedReportItemViewModel> savedReports = this.reportManager.GetAll(this.Header.SearchText, this.SortColumn, this.SortDirection == SortOrder.Ascending, start, this.Pagination.PageSize, out int total);

                if (pageNo == 1)
                {
                    this.Pagination.TotalRows = this.LimitTotalRows(total);
                }

                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.Reports.Clear();

                    foreach (SavedReportItemViewModel item in this.LimitData(savedReports, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, total))
                    {
                        this.Reports.Add(item);
                    }

                    if (this.Reports.Count == 0)
                    {
                        this.Header.SearchDisabled = string.IsNullOrEmpty(this.Header.SearchText);

                        this.Header.SetText((this.Header.SearchDisabled ? string.Empty : string.Format(PermissionResource.AllSavedReportSearchedText, this.Header.SearchText)) +
                                            PermissionResource.ReportEmptyCaption, true);

                        this.Pagination.TotalRows = 0;
                    }
                    else
                    {
                        this.Header.SetText(
                            (string.IsNullOrEmpty(this.Header.SearchText) ? string.Empty : string.Format(PermissionResource.AllSavedReportSearchedText, this.Header.SearchText)) +
                            string.Format(total == 1 ? PermissionResource.AllSavedReportRowCountCaption : PermissionResource.AllSavedReportRowsCountCaption, total));
                    }
                });
            }
            catch (Exception)
            {
                this.Header.SetText(ErrorResource.DatabaseUnavailable, true);
                this.Header.SearchDisabled = true;
            }
        }

        private async Task ChainTasksAsync()
        {
            Task taskProgress = Task.Run(async () => await this.dispatcherService.InvokeAsync(() => this.Header.ShowProgress()));
            Task taskNext = taskProgress.ContinueWith(t => this.DoChangePageAsync());

            await taskNext.ContinueWith(async t => { await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress()); });
        }

        private void Open(object obj)
        {
            SavedReportItemViewModel selectedItem = this.Reports.FirstOrDefault(m => m.IsSelected);
            if (selectedItem == null)
            {
                return;
            }

            this.navigationService.NavigateWithAsync<SavedReportDetailListViewModel>(selectedItem.Report);
        }

        private async Task DeleteAsync(object obj)
        {
            if (!this.dialogService.AskRemove())
            {
                return;
            }

            List<SavedReportItemViewModel> selectedItems = this.Reports.Where(m => m.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            this.Header.ShowProgress();

            await Task.Run(async () =>
            {
                try
                {
                    for (int i = selectedItems.Count - 1; i >= 0; i--)
                    {
                        this.reportManager.Delete(selectedItems[i].Report.Id);
                        this.logger.LogInformation("{Folder} dated {Date} has been removed from saved report.", selectedItems[i].Report.Folder, selectedItems[i].Report.Date);
                        await this.dispatcherService.InvokeAsync(() => this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Publish(selectedItems[i]));
                    }

                    await this.dispatcherService.InvokeAsync(() => this.Header.EndProgress());
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to delete selected reports due to an unhandled error.");
                }
            });
        }

        private async Task CompareAsync(object obj)
        {
            SavedReportItemViewModel[] selectedItems = this.Reports.Where(m => m.IsSelected).OrderBy(m => m.Report.Date).ToArray();

            if (selectedItems.Length != 2)
            {
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(PermissionCompareResource.CompareItemsError));
                return;
            }

            SavedReportItemViewModel firstSelectedItem = selectedItems[0];
            SavedReportItemViewModel secondSelectedItem = selectedItems[1];
            if (!firstSelectedItem.Report.Folder.Equals(secondSelectedItem.Report.Folder, StringComparison.InvariantCultureIgnoreCase))
            {
                this.dialogService.ShowMessage(PermissionCompareResource.CompareDifferentReportsText);
                return;
            }

            try
            {
                var compareObjects = new CompareObjects<SavedReportItemViewModel>(firstSelectedItem, secondSelectedItem);
                await this.navigationService.NavigateWithAsync<ComparePermissionViewModel>(compareObjects);
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to compare the selected reports due to an unhandled error.";
                this.dialogService.ShowMessage(errorMessage);
                this.logger.LogError(e, errorMessage);
            }
        }

        private async Task OnSave(SavedReportItemViewModel permission)
        {
            await this.DoChangePageAsync();
        }

        private async Task OnRemove(SavedReportItemViewModel permission)
        {
            await this.DoChangePageAsync();
        }

        private async Task OnDescriptionUpdated(SavedReportItemViewModel permission)
        {
            await this.DoChangePageAsync();
        }

        private void SetEvents()
        {
            this.eventAggregator.GetEvent<PermissionSavedEvent>().Subscribe(x => this.OnSave(x).FireAndForgetSafeAsync());

            this.eventAggregator.GetEvent<SavedPermissionUpdatedEvent>().Subscribe(x => this.OnDescriptionUpdated(x).FireAndForgetSafeAsync());

            this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Subscribe(x => this.OnRemove(x).FireAndForgetSafeAsync());
        }

        private void SetHeader()
        {
            this.Pagination = new PaginationViewModel(this.configurationManager, this.ChangePageAsync, CommonResource.ReportsTitle);

            this.Header = new HeaderViewModel(this.Pagination)
            {
                SearchCommand = new AsyncRelayCommand(this.SearchAsync)
            };

            this.Header.SetText(PermissionResource.AllSavedReportsCaption);
        }

        private async Task<bool> CanCompareAsync(object p)
        {
            bool result = (this.Reports?.Where(m => m.IsSelected).Count() ?? 0) == 2;
            return await Task.FromResult(result);
        }

        private async Task FillReportsAsync(ResultEnumerableViewModel<SavedReportItemViewModel> resultViewModel)
        {
            int total = resultViewModel.Total;

            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            if (pageNo == 1)
            {
                this.Pagination.TotalRows = this.LimitTotalRows(total);
            }

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Reports.Clear();

                foreach (SavedReportItemViewModel item in this.LimitData(resultViewModel.Result, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, total))
                {
                    this.Reports.Add(item);
                }

                if (this.Reports.Count == 0)
                {
                    this.Header.SearchDisabled = true;

                    this.Header.SetText(PermissionResource.ReportEmptyCaption, true);

                    this.Pagination.TotalRows = 0;
                }
                else
                {
                    this.Header.SetText(string.Format(total == 1 ? PermissionResource.AllSavedReportRowCountCaption : PermissionResource.AllSavedReportRowsCountCaption, total));
                }
            });
        }
    }
}