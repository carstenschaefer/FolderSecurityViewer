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

namespace FSV.ViewModel.Home
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Core;
    using Events;
    using Folder;
    using Owner;
    using Permission;
    using Prism.Events;
    using Resources;
    using Services;
    using ShareReport;
    using UserReport;

    public class ReportContainerViewModel : HomeContentBaseViewModel
    {
        private readonly IDatabaseConfigurationManager _databaseConfigurationManager;
        private readonly IDialogService _dialogService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ObservableCollection<ReportViewModel> _reports = new();
        private readonly IEventAggregator eventAggregator;
        private readonly IExportService exportService;
        private readonly INavigationService navigationService;

        private ICommand _closeAllCommand;
        private ICommand _exportAllCommand;
        private ICommand _pathItemCommand;

        private WorkspaceViewModel _selectedItem;

        public ReportContainerViewModel(
            INavigationService navigationService,
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            ISavedReportService savedReportService,
            IDatabaseConfigurationManager databaseConfigurationManager,
            IExportService exportService,
            IEventAggregator eventAggregator) :
            base(HomeResource.ReportsCaption, "ReportsIcon")
        {
            if (savedReportService == null)
            {
                throw new ArgumentNullException(nameof(savedReportService));
            }

            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this._dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this._dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this._databaseConfigurationManager = databaseConfigurationManager ?? throw new ArgumentNullException(nameof(databaseConfigurationManager));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            this.RegisterViewModelsWithNavigationService();

            this.AddSavedReportsInViewCollection(savedReportService).FireAndForgetSafeAsync();
        }

        public IReadOnlyCollection<ReportViewModel> Items => this._reports;

        public ICommand CloseAllCommand => this._closeAllCommand ??= new RelayCommand(this.CloseAll, this.CanClose);
        public ICommand ExportAllCommand => this._exportAllCommand ??= new RelayCommand(this.Export, this.CanExport);
        public ICommand PathItemCommand => this._pathItemCommand ??= new RelayCommand(this.OpenFolders);

        public WorkspaceViewModel SelectedItem
        {
            get => this._selectedItem;
            set => this.Set(ref this._selectedItem, value, nameof(this.SelectedItem));
        }

        private void RegisterViewModelsWithNavigationService()
        {
            this.navigationService.AddFor<SharedServerViewModel>(this.AddShareReport);
            this.navigationService.AddFor<PermissionsViewModel>(this.AddReport);
            this.navigationService.AddFor<UserReportViewModel>(this.AddReport);
            this.navigationService.AddFor<FolderViewModel>(this.AddReport);
            this.navigationService.AddFor<OwnerReportViewModel>(this.AddReport);
            this.navigationService.AddFor<ComparePermissionViewModel>(this.AddReport);
            this.navigationService.AddFor<CompareUserReportViewModel>(this.AddReport);

            this.navigationService.AddFor<SavedReportDetailListViewModel>(this.AddReport);
            this.navigationService.AddFor<SavedUserReportViewModel>(this.AddSavedUserReportAsync);
            this.navigationService.AddFor<FolderSavedReportListViewModel>(this.AddReport);
            this.navigationService.AddFor<SavedFolderUserReportListViewModel>(this.AddReport);

            this.navigationService.AddFor<AllSavedReportListViewModel>(this.AddReport);
            this.navigationService.AddFor<SavedUserReportListViewModel>(this.AddReport);

            this.navigationService.AddFor<GroupPermissionsViewModel>(this.AddReport);
        }

        private bool CheckAndAddReport(ReportViewModel report)
        {
            ReportViewModel existingReport = this._reports.FirstOrDefault(m => m.Equals(report));
            if (existingReport == null)
            {
                if (report.Closable)
                {
                    report.Closing += this.ReportClosing;
                }

                this._reports.Add(report);
                this._reports.SetCurrentWorkspace(report);
                return false;
            }

            this._reports.SetCurrentWorkspace(existingReport);
            return true;
        }

        private void AddReport(ReportViewModel report)
        {
            this.CheckAndAddReport(report);
        }

        private void AddShareReport(SharedServerViewModel viewModel)
        {
            if (viewModel == SharedServerViewModel.Empty)
                // No need to add an Empty entry in tabs.
            {
                return;
            }

            this.AddReport(viewModel);
        }

        private async Task AddSavedUserReportAsync(SavedUserReportViewModel viewModel)
        {
            if (!this.CheckAndAddReport(viewModel))
            {
                await viewModel.RefreshContentAsync();
            }
        }

        private void CloseAll(object obj)
        {
            List<ReportViewModel> reports = this._reports.Where(m => m.Closable && (obj == null || (obj is ReportType reportType && m.ReportType == reportType))).ToList();

            for (int i = reports.Count - 1; i >= 0; i--)
            {
                reports[i].CancelCommand.Execute(null);
            }
        }

        private bool CanClose(object obj)
        {
            return obj == null || (obj is ReportType reportType && this.Items.Any(m => m.ReportType == reportType));
        }

        private void Export(object obj)
        {
            if (obj is ReportType reportType)
            {
                IEnumerable<ReportViewModel> reports = this.Items.Where(m => m.ReportType == reportType && m.Exportable);
                if (reports.Any())
                {
                    switch (reportType)
                    {
                        case ReportType.Permission:
                        case ReportType.SavedPermission:
                        case ReportType.ComparePermission:
                            this.ExportData<PermissionReportBaseViewModel>(reports, this.exportService.ExportPermissionReports);
                            break;
                        case ReportType.Folder:
                            this.ExportData<FolderViewModel>(reports, this.exportService.ExportFolderReports);
                            break;
                        case ReportType.Owner:
                            this.ExportData<OwnerReportViewModel>(reports, this.exportService.ExportOwnerReports);
                            break;
                        case ReportType.User:
                        case ReportType.SavedUser:
                        case ReportType.CompareUser:
                            this.ExportData<UserReportBaseViewModel>(reports, this.exportService.ExportUserReports);
                            break;
                    }
                }
            }
        }

        private bool CanExport(object obj)
        {
            return obj is ReportType reportType && this.Items.Any(m => m.ReportType == reportType && m.Exportable);
        }

        private void ReportClosing(object s, CloseCommandEventArgs e)
        {
            var report = s as ReportViewModel;
            report.Closing -= this.ReportClosing;

            this._reports.Remove(report);
        }

        private async Task AddSavedReportsInViewCollection(ISavedReportService savedReportService)
        {
            if (!this._databaseConfigurationManager.HasConfiguredDatabaseProvider())
            {
                return;
            }

            try
            {
                SavedReportItems savedItems = await savedReportService.GetSavedReports();
                await this._dispatcherService.InvokeAsync(() =>
                {
                    this._reports.Add(savedItems.Permissions);
                    this._reports.Add(savedItems.UserPermissions);
                });
            }
            catch (Exception ex)
            {
                await this._dispatcherService.InvokeAsync(() => this._dialogService.ShowMessage(ex.Message));
            }
        }

        private void ExportData<T>(IEnumerable<ReportViewModel> reports, Action<IEnumerable<T>> action)
        {
            IEnumerable<T> typeReports = reports.OfType<T>();
            if (!typeReports.Any())
            {
                this._dialogService.ShowMessage(ExportResource.NoReportsToExportText);
            }

            action(typeReports);
        }

        private void OpenFolders(object p)
        {
            if (p is not null && p is string path)
            {
                this.eventAggregator.GetEvent<HomeFolderTreeOpenEvent>().Publish(path);
            }
        }
    }
}