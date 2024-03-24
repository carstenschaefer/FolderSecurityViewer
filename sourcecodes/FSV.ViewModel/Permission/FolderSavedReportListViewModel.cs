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
    using Core;
    using Events;
    using Microsoft.Extensions.Logging;
    using Passables;
    using Prism.Events;
    using Resources;

    /// <summary>
    ///     Represents all saved permissions of specific folder, and it is displayed in a separate tab.
    /// </summary>
    public class FolderSavedReportListViewModel : PermissionReportBaseViewModel
    {
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<FolderSavedReportListViewModel> logger;
        private readonly INavigationService navigationService;
        private readonly IPermissionReportManager reportManager;

        internal FolderSavedReportListViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IPermissionReportManager reportManager,
            ILogger<FolderSavedReportListViewModel> logger,
            string path)
            : base(path)
        {
            this.ReportTypeCaption = PermissionReport;
            this.ReportType = ReportType.SavedPermission;
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.Reports = new ObservableCollection<SavedReportItemViewModel>();
            this.OpenCommand = new RelayCommand(this.Open, p => this.Reports.Any(m => m.IsSelected));

            async Task<bool> CanDeleteAsync(object p)
            {
                bool result = this.Reports.Any(m => m.IsSelected);
                return await Task.FromResult(result);
            }

            this.DeleteCommand = new AsyncRelayCommand(this.DeleteAsync, CanDeleteAsync);

            async Task<bool> CanCompareAsync(object p)
            {
                bool result = this.Reports?.Where(m => m.IsSelected).Count() > 1;
                return await Task.FromResult(result);
            }

            this.CompareCommand = new AsyncRelayCommand(this.CompareAsync, CanCompareAsync);

            this.Exportable = false;

            this.Title = $"{PermissionResource.SavedReportsOfCaption} - {path}";
            this.DisplayName = PermissionResource.SavedReportsOfCaption;
            this.Header = new HeaderViewModel();

            this.RefreshContentAsync().FireAndForgetSafeAsync();

            this.eventAggregator.GetEvent<PermissionSavedEvent>().Subscribe(permission =>
            {
                this.Reports.Add(permission);
                this.SetHeaderText();
            });

            this.eventAggregator.GetEvent<SavedPermissionUpdatedEvent>().Subscribe(item => this.RefreshContentAsync().FireAndForgetSafeAsync());
            this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Subscribe(this.OnRemove);
        }

        public ObservableCollection<SavedReportItemViewModel> Reports { get; }

        public ICommand OpenCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand SelectionChangedCommand { get; }

        public ICommand CompareCommand { get; }

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

        private async Task RefreshContentAsync()
        {
            await Task.Run(async () =>
            {
                this.Header.ShowProgress();
                this.Header.SetText(CommonResource.LoadingText);

                await this.dispatcherService.InvokeAsync(() => this.Reports.Clear());

                try
                {
                    IList<SavedReportItemViewModel> list = this.reportManager.Get(this.SelectedFolderPath);
                    foreach (SavedReportItemViewModel item in list)
                    {
                        await this.dispatcherService.InvokeAsync(() => this.Reports.Add(item));
                    }

                    this.Header.EndProgress();
                    this.SetHeaderText();
                }
                catch (Exception e)
                {
                    this.Header.EndProgress();
                    const string message = "Failed to refresh content due to an unhandled error.";
                    this.logger.LogError(e, message);
                    this.dialogService.ShowMessage(message);
                }
            });
        }

        public override int GetHashCode()
        {
            return Tuple.Create(nameof(FolderSavedReportListViewModel), this.SelectedFolderPath).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is FolderSavedReportListViewModel vm && vm.GetHashCode() == this.GetHashCode();
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
            SavedReportItemViewModel selectedItem = this.Reports.FirstOrDefault(m => m.IsSelected);
            if (selectedItem == null)
            {
                return;
            }

            try
            {
                this.reportManager.Delete(selectedItem.Report.Id);
                await this.dispatcherService.InvokeAsync(() => this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Publish(selectedItem));

                this.logger.LogInformation("{Folder} dated {Date} has been removed from saved report.", selectedItem.Report.Folder, selectedItem.Report.Date);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to delete the selected report due to an unhandled error.");
            }
        }

        private async Task CompareAsync(object obj)
        {
            IOrderedEnumerable<SavedReportItemViewModel> selectedItems = this.Reports?.Where(m => m.IsSelected).OrderBy(m => m.Report.Date);

            if (selectedItems.Count() > 2)
            {
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(PermissionCompareResource.CompareItemsError));
                return;
            }

            try
            {
                var compareObjects = new CompareObjects<SavedReportItemViewModel>(selectedItems.First(), selectedItems.Last());
                await this.navigationService.NavigateWithAsync<ComparePermissionViewModel>(compareObjects);
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to compare the selected report items due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                this.dialogService.ShowMessage(errorMessage);
            }
        }

        private void SetHeaderText()
        {
            this.Header.SetText(string.Format(this.Reports.Count == 1 ? PermissionResource.AllSavedReportRowCountCaption : PermissionResource.AllSavedReportRowsCountCaption, this.Reports.Count));
        }

        private void OnRemove(SavedReportItemViewModel item)
        {
            SavedReportItemViewModel collectionItem = this.Reports.FirstOrDefault(m => m.Report.Id == item.Report.Id);
            if (collectionItem != null)
            {
                this.Reports.Remove(collectionItem);
                this.SetHeaderText();
            }
        }
    }
}