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
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdBrowser;
    using Core;
    using Events;
    using FileSystem.Interop.Abstractions;
    using Folder;
    using Owner;
    using Passables;
    using Permission;
    using Prism.Events;
    using Resources;
    using ShareReport;
    using Templates;
    using UserReport;

    public class LandingViewModel : HomeContentBaseViewModel
    {
        private readonly ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder;
        private readonly IDialogService dialogService;

        private readonly IEventAggregator eventAggregator;
        private readonly IFileManagementService fileManagementService;
        private readonly ModelBuilder<FolderTreeViewModel> folderTreeViewModelBuilder;
        private readonly INavigationService navigationService;
        private readonly ModelBuilder<ServersContainerViewModel> serversContainerViewModelBuilder;
        private readonly ModelBuilder<TemplateContainerViewModel> templateContainerViewModelBuilder;
        private ADBrowserViewModel _adBrowser;

        private bool _adRequired;
        private bool _adVisible;
        private ICommand _backCommand;

        private ICommand _cancelCommand;
        private ICommand _nextCommand;
        private bool _optionsVisible = true;
        private bool _saveAsTemplate;
        private ICommand _saveAsTemplateCommand;
        private bool _saveAsTemplateVisible;

        private LandingOptionViewModel _selectedOption;
        private ServersContainerViewModel _servers;
        private bool _serverVisible;
        private ICommand _startCommand;
        private ICommand _startNtfsPermissionReportCommand;
        private ICommand _startWithADCommand;
        private ICommand _startWithShareCommand;
        private TemplateContainerViewModel _templateContainer;
        private FolderTreeViewModel _tree;
        private bool _treeVisible;
        private ReportType CurrentReportType;

        public LandingViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IDialogService dialogService,
            IFileManagementService fileManagementService,
            ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder,
            ModelBuilder<ServersContainerViewModel> serversContainerViewModelBuilder,
            ModelBuilder<TemplateContainerViewModel> templateContainerViewModelBuilder,
            ModelBuilder<FolderTreeViewModel> folderTreeViewModelBuilder)
            : base(HomeResource.HomeCaption, "HomeIcon")
        {
            this.adBrowserViewModelBuilder = adBrowserViewModelBuilder ?? throw new ArgumentNullException(nameof(adBrowserViewModelBuilder));
            this.serversContainerViewModelBuilder = serversContainerViewModelBuilder ?? throw new ArgumentNullException(nameof(serversContainerViewModelBuilder));
            this.templateContainerViewModelBuilder = templateContainerViewModelBuilder ?? throw new ArgumentNullException(nameof(templateContainerViewModelBuilder));
            this.folderTreeViewModelBuilder = folderTreeViewModelBuilder ?? throw new ArgumentNullException(nameof(folderTreeViewModelBuilder));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));

            this.FillOptions();

            this.eventAggregator.GetEvent<TemplateStartedEvent>().Subscribe(this.Cancel);
            this.eventAggregator.GetEvent<DirectoryTreeOpenRequested>().Subscribe(this.TreeOpened);
        }

        public bool ADRequired
        {
            get => this._adRequired;
            set => this.Set(ref this._adRequired, value, nameof(this.ADRequired));
        }

        public FolderTreeViewModel Tree
        {
            get => this._tree;
            private set => this.Set(ref this._tree, value, nameof(this.Tree));
        }

        public ADBrowserViewModel ADBrowser
        {
            get => this._adBrowser;
            private set => this.Set(ref this._adBrowser, value, nameof(this.ADBrowser));
        }

        public ServersContainerViewModel Servers
        {
            get => this._servers;
            private set => this.Set(ref this._servers, value, nameof(this.Servers));
        }

        public TemplateContainerViewModel TemplateContainer
        {
            get => this._templateContainer;
            private set => this.Set(ref this._templateContainer, value, nameof(this.TemplateContainer));
        }

        public bool OptionsVisible
        {
            get => this._optionsVisible;
            set => this.Set(ref this._optionsVisible, value, nameof(this.OptionsVisible));
        }

        public bool ADBrowserVisible
        {
            get => this._adVisible;
            set => this.Set(ref this._adVisible, value, nameof(this.ADBrowserVisible));
        }

        public bool TreeVisible
        {
            get => this._treeVisible;
            set => this.Set(ref this._treeVisible, value, nameof(this.TreeVisible));
        }

        public bool ServersVisible
        {
            get => this._serverVisible;
            set => this.Set(ref this._serverVisible, value, nameof(this.ServersVisible));
        }

        public bool CanSaveAsTemplate
        {
            get => this._saveAsTemplate;
            set => this.Set(ref this._saveAsTemplate, value, nameof(this.CanSaveAsTemplate));
        }

        public bool SaveAsTemplateVisible
        {
            get => this._saveAsTemplateVisible;
            set => this.Set(ref this._saveAsTemplateVisible, value, nameof(this.SaveAsTemplateVisible));
        }

        public LandingOptionViewModel SelectedOption
        {
            get => this._selectedOption;
            set => this.Set(ref this._selectedOption, value, nameof(this.SelectedOption));
        }

        public IList<LandingOptionViewModel> Options { get; } = new ObservableCollection<LandingOptionViewModel>();
        public IList<LandingOptionViewModel> SavedReportOptions { get; } = new ObservableCollection<LandingOptionViewModel>();

        public ICommand CancelCommand => this._cancelCommand ??= new RelayCommand(this.Cancel);
        public ICommand NextCommand => this._nextCommand ??= new RelayCommand(this.Next, this.CanNext);
        public ICommand BackCommand => this._backCommand ??= new RelayCommand(this.Back, this.CanBack);
        public ICommand StartCommand => this._startCommand ??= new RelayCommand(this.Start, this.CanStart);
        public ICommand StartWithADCommand => this._startWithADCommand ??= new RelayCommand(this.StartWithAD, this.CanStartWithAD);
        public ICommand StartWithShareCommand => this._startWithShareCommand ??= new RelayCommand(this.StartWithShare, this.CanStartWithShare);

        public ICommand StartNtfsPermissionReportCommand =>
            this._startNtfsPermissionReportCommand ??= new RelayCommand(this.StartNtfsPermissionReport, this.CanStartNtfsPermissionReport);

        public ICommand SaveAsTemplateCommand => this._saveAsTemplateCommand ??= new AsyncRelayCommand(this.SaveAsTemplateAsync, this.CanSaveTemplateAsync);

        protected override void OnPropertyChange(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.SelectedOption) && this._selectedOption != null)
            {
                this._selectedOption.ShowCommand?.Execute(null);
            }
            else if (e.PropertyName == nameof(this.IsSelected) && !this.IsSelected)
            {
                this.Cancel(null);
            }

            base.OnPropertyChange(e);
        }

        private void FillOptions()
        {
            this.Options.Add(new LandingOptionViewModel(ReportViewModel.PermissionReport, "PermissionReportIcon", HomeResource.PermissionReportText, this.ShowPermissionReport));
            this.Options.Add(new LandingOptionViewModel(ReportViewModel.FolderReport, "FolderReportIcon", HomeResource.FolderReportText, this.ShowFolderReport));
            this.Options.Add(new LandingOptionViewModel(ReportViewModel.OwnerReport, "OwnerReportIcon", HomeResource.OwnerReportText, this.ShowOwnerReport));
            this.Options.Add(new LandingOptionViewModel(ReportViewModel.ShareReport, "ShareReportIcon", HomeResource.ShareReportText, this.ShowShareReport));
            this.Options.Add(new LandingOptionViewModel(ReportViewModel.UserReport, "UserReportIcon", HomeResource.UserReportText, this.ShowUserReport));

            this.SavedReportOptions.Add(new LandingOptionViewModel(ReportViewModel.PermissionReport, "PermissionReportIcon", HomeResource.SavedPermissionReportsText, this.ShowSavedPermissionReport));
            this.SavedReportOptions.Add(new LandingOptionViewModel(ReportViewModel.UserReport, "UserReportIcon", HomeResource.SavedUserReportsText, this.ShowSavedUserReport));
            this.SavedReportOptions.Add(new LandingOptionViewModel(ReportViewModel.Templates, "TemplatesIcon", HomeResource.TemplatesText, this.ShowTemplatesAsync));
        }

        private void ShowUserReport(object obj)
        {
            this.ShowAdTreeAsync(ReportType.User);
        }

        private void ShowSavedUserReport(object obj)
        {
            this.navigationService.NavigateWithAsync<SavedUserReportListViewModel>();
        }

        private void ShowShareReport(object obj)
        {
            this.ShowServer(ReportType.Share);
        }

        private void ShowOwnerReport(object obj)
        {
            this.ShowAdTreeAsync(ReportType.Owner);
        }

        private void ShowFolderReport(object obj)
        {
            this.ShowTree(ReportType.Folder);
        }

        private void ShowPermissionReport(object obj)
        {
            this.ShowTree(ReportType.Permission);
        }

        private void ShowSavedPermissionReport(object obj)
        {
            this.navigationService.NavigateWithAsync<AllSavedReportListViewModel>();
        }

        private void Cancel(object obj)
        {
            this.SelectedOption = null;

            this.ADRequired = false;
            this.ADBrowserVisible = false;
            this.TreeVisible = false;
            this.ServersVisible = false;
            this.OptionsVisible = true;
            this.CanSaveAsTemplate = false;

            this.TemplateContainer = null;
            this.ClearTree();
            this.ADBrowser = null;
        }

        private void ClearTree()
        {
            this.Tree?.Clear();
            this.Tree = null;
        }

        private void Next(object obj)
        {
            if (this.HasDirectoryError(this.Tree?.SelectedPath))
            {
                return;
            }

            this.ADBrowserVisible = true;
        }

        private bool CanNext(object obj)
        {
            return this.ADRequired && !string.IsNullOrEmpty(this.Tree.SelectedPath);
        }

        private void Back(object obj)
        {
            this.ADBrowserVisible = false;
        }

        private bool CanBack(object obj)
        {
            return this.ADRequired && this.ADBrowserVisible;
        }

        private void Start(object obj)
        {
            this.StartScan(this.CurrentReportType, this.Tree?.SelectedPath);
        }

        private bool CanStart(object obj)
        {
            return !string.IsNullOrEmpty(this.Tree?.SelectedPath);
        }

        private void StartWithAD(object obj)
        {
            this.Start(obj);
        }

        private bool CanStartWithAD(object obj)
        {
            return this.ADBrowser != null && (this.ADBrowser.CanReport || (this.ADBrowser.SelectedPrincipal != null && this.ADBrowser.SelectedPrincipal.Type == BasePrincipalViewModel.TypeUser));
        }

        private void StartWithShare(object obj)
        {
            this.Start(obj);
        }

        private bool CanStartWithShare(object obj)
        {
            return this.Servers != null &&
                   this.Servers.SelectedServer != SharedServerViewModel.Empty &&
                   (this.Servers.SelectedServer != null ||
                    (this.Servers.SelectedShare != null &&
                     this.Servers.SelectedShare.IsLoaded));
        }

        private void StartNtfsPermissionReport(object obj)
        {
            this.navigationService.NavigateWithAsync<PermissionsViewModel>(this.Servers.SelectedShare.Share);
            this.Cancel(null);
        }

        private bool CanStartNtfsPermissionReport(object obj)
        {
            return this.Servers != null && this.Servers.SelectedShare != null && this.Servers.SelectedShare.IsLoaded;
        }

        private void ShowTree(ReportType reportType)
        {
            this.CurrentReportType = reportType;

            this.SetTree();

            this.OptionsVisible = false;
            this.TreeVisible = true;
            this.ADBrowserVisible = false;
            this.ADRequired = false;
            this.ServersVisible = false;

            this.SetSaveAsTemplate(reportType);
        }

        private async void ShowAdTreeAsync(ReportType reportType)
        {
            this.CurrentReportType = reportType;
            this.OptionsVisible = false;
            this.ADRequired = true;
            this.ADBrowserVisible = false;
            this.ServersVisible = false;
            this.TreeVisible = true;

            this.SetTree();

            if (this.ADBrowser == null)
            {
                this.ADBrowser = this.adBrowserViewModelBuilder.Build(ADBrowserType.Principals);
                await this._adBrowser.InitializeAsync();
            }

            this.SetSaveAsTemplate(reportType);
        }

        private void SetSaveAsTemplate(ReportType reportType)
        {
            switch (reportType)
            {
                case ReportType.Permission:
                case ReportType.Owner:
                case ReportType.User:
                    this.SaveAsTemplateVisible = true;
                    break;
                default:
                    this.SaveAsTemplateVisible = false;
                    break;
            }
        }

        private async void ShowServer(ReportType reportType)
        {
            this.OptionsVisible = false;
            this.CurrentReportType = reportType;
            this.TreeVisible = false;
            this.ADBrowserVisible = false;
            this.ServersVisible = true;

            if (this.Servers != null)
            {
                return;
            }

            this.Servers = this.serversContainerViewModelBuilder.Build();
            await this.Servers.FillServersAsync();
        }

        private async Task ShowTemplatesAsync(object _)
        {
            this.OptionsVisible = false;
            this.TreeVisible = false;
            this.ADBrowserVisible = false;
            this.ServersVisible = false;

            if (this.TemplateContainer == null)
            {
                this.TemplateContainer = this.templateContainerViewModelBuilder.Build();
                this.TemplateContainer.ClearSelections();
                await this.TemplateContainer.InitializeAsync();
            }
        }

        private void SetTree()
        {
            if (this.Tree == null)
            {
                this.Tree = this.folderTreeViewModelBuilder.Build();
                this.Tree.Standalone = false;
                this.Tree.InitializeAsync();
            }
        }

        private void TreeOpened(DirectoryTreeOpenRequestedData e)
        {
            ReportType reportType = e.ReportType == ReportType.Unknown ? this.CurrentReportType : e.ReportType;
            switch (reportType)
            {
                case ReportType.Owner:
                case ReportType.User:
                    this.Next(null);
                    break;
                default:
                    this.StartScan(reportType, e.Path);
                    break;
            }
        }

        private async Task SaveTemplateAsync(TemplateType templateType)
        {
            if (!this.CanSaveAsTemplate)
            {
                return;
            }

            TemplateContainerViewModel templateContainer = this.templateContainerViewModelBuilder.Build();
            await templateContainer.InitializeAsync();
            if (!templateContainer.ShowTemplateEditor(templateType, this.Tree.SelectedPath, this.ADBrowser?.SelectedPrincipal.AccountName, true))
            {
                this.CanSaveAsTemplate = false;
            }
        }

        private bool HasDirectoryError(string path)
        {
            try
            {
                this.ThrowIfDirectoryNotExists(path);
                this.ThrowIfAccessIsDenied(path);

                this.eventAggregator.GetEvent<HomeFloaterCloseRequested>().Publish();

                return false;
            }
            catch (Exception ex)
            {
                this.dialogService.ShowMessage(ex.Message);
                return true;
            }
        }

        private void StartScan(ReportType reportType, string path)
        {
            if (reportType != ReportType.Share && this.HasDirectoryError(path))
            {
                return;
            }

            switch (reportType)
            {
                case ReportType.Permission:
                    this.navigationService.NavigateWithAsync<PermissionsViewModel>(path);
                    break;
                case ReportType.Folder:
                    this.navigationService.NavigateWithAsync<FolderViewModel>(path);
                    break;
                case ReportType.Owner:
                    this.navigationService.NavigateWithAsync<OwnerReportViewModel>(
                        new UserPath(this.ADBrowser.CanReport ? this.ADBrowser.PrincipalName : this.ADBrowser.SelectedPrincipal.AccountName,
                            path));
                    break;
                case ReportType.User:
                    string selectedPrincipalAccountName = this.ADBrowser.CanReport ? this.ADBrowser.PrincipalName : this.ADBrowser.SelectedPrincipal.AccountName;
                    var userPath = new UserPath(selectedPrincipalAccountName, path);
                    this.navigationService.NavigateWithAsync<UserReportViewModel>(userPath);
                    break;
                case ReportType.Share:
                    SharedServerViewModel server = this.Servers.GetCurrentServer();
                    this.navigationService.NavigateWithAsync(server);
                    break;
                case ReportType.SavedPermission:
                    this.navigationService.NavigateWithAsync<FolderSavedReportListViewModel>(path);
                    break;
                case ReportType.SavedUser:
                    this.navigationService.NavigateWithAsync<SavedFolderUserReportListViewModel>(new UserPath(this.ADBrowser.SelectedPrincipal.AccountName, path));
                    break;
            }

            this.Cancel(null);
        }

        private async Task SaveAsTemplateAsync(object obj)
        {
            switch (this.CurrentReportType)
            {
                case ReportType.Permission:
                    await this.SaveTemplateAsync(TemplateType.PermissionReport);
                    break;
                case ReportType.Owner:
                    await this.SaveTemplateAsync(TemplateType.OwnerReport);
                    break;
                case ReportType.User:
                    await this.SaveTemplateAsync(TemplateType.UserReport);
                    break;
            }
        }

        private Task<bool> CanSaveTemplateAsync(object b)
        {
            return Task.FromResult(
                !string.IsNullOrEmpty(this.Tree?.SelectedPath) && (this.ADBrowser == null || (this.ADBrowser != null && this.ADBrowser.CanReport) || (this.ADBrowser.SelectedPrincipal != null && this.ADBrowser.SelectedPrincipal.Type == BasePrincipalViewModel.TypeUser))
            );
        }

        private void ThrowIfAccessIsDenied(string path)
        {
            if (this.fileManagementService.IsAccessDenied(path))
            {
                throw new IOException(string.Format(ErrorResource.AccessDenied, path));
            }
        }

        private void ThrowIfDirectoryNotExists(string path)
        {
            if (!this.fileManagementService.GetDirectoryExist(path))
            {
                throw new DirectoryNotFoundException(string.Format(ErrorResource.DirectoryNotExist, path));
            }
        }
    }
}