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
    ///     Represents all saved permissions of specific folder. The list is shown as a tab in bottom of permission report.
    /// </summary>
    public class PermissionItemSavedReportsViewModel : PermissionItemBase, ISortable
    {
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;

        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<PermissionItemSavedReportsViewModel> logger;
        private readonly INavigationService navigationService;
        private readonly IPermissionReportManager reportManager;
        private bool _isLoaded;

        internal PermissionItemSavedReportsViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IPermissionReportManager reportManager,
            ILogger<PermissionItemSavedReportsViewModel> logger,
            string folderPath
        ) : base(folderPath)
        {
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.Icon = "LoadReportIcon";

            this.CanResize = true;

            this.Reports = new ObservableCollection<SavedReportItemViewModel>();
            this.OpenCommand = new RelayCommand(this.Open, p => this.Reports.Any(m => m.IsSelected));
            this.DeleteCommand = new RelayCommand(this.Delete, p => this.Reports.Any(m => m.IsSelected));

            async Task<bool> CanExecuteCompareAsync(object p)
            {
                bool result = this.Reports?.Where(m => m.IsSelected).Count() > 1;
                return await Task.FromResult(result);
            }

            this.CompareCommand = new AsyncRelayCommand(this.CompareAsync, CanExecuteCompareAsync);

            this.DisplayName = PermissionResource.PermissionSavedReportCaption;

            this.eventAggregator.GetEvent<PermissionSavedEvent>().Subscribe(this.OnSave);
            this.eventAggregator.GetEvent<SavedPermissionUpdatedEvent>().Subscribe(async x => await this.OnDescriptionUpdated(x));
            this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Subscribe(this.OnRemove);
        }

        public ObservableCollection<SavedReportItemViewModel> Reports { get; }

        public ICommand OpenCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand SelectionChangedCommand { get; }

        public ICommand CompareCommand { get; }

        public async Task CompareAsync(SavedReportItemViewModel item1, SavedReportItemViewModel item2)
        {
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
                this.DoProgress();

                await this.dispatcherService.InvokeAsync(() => this.Reports.Clear());

                try
                {
                    foreach (SavedReportItemViewModel item in this.reportManager.Get(this.FolderPath))
                    {
                        await this.dispatcherService.InvokeAsync(() => this.Reports.Add(item));
                    }

                    this.StopProgress();
                    this._isLoaded = true;
                }
                catch (Exception ex)
                {
                    this.StopProgress();

                    const string errorMessage = "Failed to refresh content due to an unhandled error.";
                    this.logger.LogError(ex, errorMessage);
                    this.dialogService.ShowMessage(errorMessage);
                }
            });
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

        private void Delete(object obj)
        {
            if (!this.dialogService.AskRemove())
            {
                return;
            }

            SavedReportItemViewModel selectedItem = this.Reports.FirstOrDefault(m => m.IsSelected);
            if (selectedItem == null)
            {
                return;
            }

            int reportId = selectedItem.Report?.Id ?? 0;

            try
            {
                this.reportManager.Delete(reportId);
                this.eventAggregator.GetEvent<SavedPermissionRemovedEvent>().Publish(selectedItem);
                this.logger.LogInformation("The {Folder} dated {Date} has been removed from saved report.", selectedItem.Report.Folder, selectedItem.Report.Date);
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to delete the selected report ({Id}) due to an unhandled error.";
                this.logger.LogError(ex, errorMessage, reportId);
            }
        }

        private async Task CompareAsync(object obj)
        {
            List<SavedReportItemViewModel> selectedItems = this.Reports?.Where(m => m.IsSelected)
                .OrderBy(m => m.Report.Date).ToList();

            if (selectedItems.Count > 2)
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

        protected override async Task OnSelectedAsync(bool selected)
        {
            if (!this._isLoaded)
            {
                await this.RefreshContentAsync();
            }
        }

        private void OnRemove(SavedReportItemViewModel item)
        {
            SavedReportItemViewModel collectionItem = this.Reports.FirstOrDefault(m => m.Report.Id == item.Report.Id);
            if (collectionItem != null)
            {
                this.Reports.Remove(collectionItem);
            }
        }

        private void OnSave(SavedReportItemViewModel item)
        {
            this.Reports.Add(item);
        }

        private async Task OnDescriptionUpdated(SavedReportItemViewModel item)
        {
            await this.RefreshContentAsync();
        }

        #region "ISortable members"

        public string GetExportSortColumn()
        {
            throw new NotImplementedException();
        }

        public string SortColumn
        {
            get => string.Empty;
            set => throw new NotImplementedException();
        }

        public SortOrder SortDirection
        {
            get => SortOrder.Indeterminate;
            set => throw new NotImplementedException();
        }

        public ICommand SortCommand => throw new NotImplementedException();

        #endregion
    }
}