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
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdBrowser;
    using Core;
    using Events;
    using Prism.Events;
    using Resources;
    using Setting;

    public class HomeViewModel : ViewModelBase
    {
        private readonly ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder;
        private readonly IDialogService dialogService;

        private readonly IEventAggregator eventAggregator;
        private readonly IFlyOutService flyOutService;
        private readonly ModelBuilder<FolderTreeViewModel> folderTreeViewModelBuilder;
        private readonly ModelBuilder<LandingViewModel> landingViewModelBuilder;
        private readonly INavigationService navigationService;
        private readonly ModelBuilder<ReportContainerViewModel> reportContainerViewModelBuilder;
        private readonly ModelBuilder<SplashViewModel> splashViewModelBuilder;
        private readonly IStartUpSequence startUp;
        private ICommand _floaterCloseCommand;
        private WorkspaceViewModel _floaterContent;
        private bool _floaterVisible;

        private ICommand _flyoutBackCommand;
        private ICommand _flyoutCloseCommand;

        private ADBrowserViewModel adTree;
        private bool disposed;
        private FlyoutViewModel flyOutContent;

        private FolderTreeViewModel folderTree;

        public HomeViewModel(
            IStartUpSequence startUp,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IDialogService dialogService,
            IFlyOutService flyOutService,
            ModelBuilder<SplashViewModel> splashViewModelBuilder,
            ModelBuilder<ReportContainerViewModel> reportContainerViewModelBuilder,
            ModelBuilder<LandingViewModel> landingViewModelBuilder,
            ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder,
            ModelBuilder<FolderTreeViewModel> folderTreeViewModelBuilder)
        {
            this.startUp = startUp ?? throw new ArgumentNullException(nameof(startUp));
            this.splashViewModelBuilder = splashViewModelBuilder ?? throw new ArgumentNullException(nameof(splashViewModelBuilder));
            this.reportContainerViewModelBuilder = reportContainerViewModelBuilder ?? throw new ArgumentNullException(nameof(reportContainerViewModelBuilder));
            this.landingViewModelBuilder = landingViewModelBuilder ?? throw new ArgumentNullException(nameof(landingViewModelBuilder));
            this.adBrowserViewModelBuilder = adBrowserViewModelBuilder ?? throw new ArgumentNullException(nameof(adBrowserViewModelBuilder));
            this.folderTreeViewModelBuilder = folderTreeViewModelBuilder ?? throw new ArgumentNullException(nameof(folderTreeViewModelBuilder));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));

            this.DisplayName = CommonResource.AppTitle;

            this.Items = new ObservableCollection<ViewModelBase>();
            this.Commands = new ObservableCollection<CommandViewModel>();

            this.navigationService.Navigate += this.HandleNavigationServiceNavigate;
            this.flyOutService.ContentAdded += this.OnFlyOutContentAdded;
        }

        public IList<ViewModelBase> Items { get; }
        public IList<CommandViewModel> Commands { get; }
        public bool HasError { get; }

        public bool FloaterVisible
        {
            get => this._floaterVisible;
            private set => this.Set(ref this._floaterVisible, value, nameof(this.FloaterVisible));
        }

        public FlyoutViewModel FlyOutContent
        {
            get => this.flyOutContent;
            set => this.Set(ref this.flyOutContent, value, nameof(this.FlyOutContent));
        }

        public WorkspaceViewModel FloaterContent
        {
            get => this._floaterContent;
            private set => this.Set(ref this._floaterContent, value, nameof(this.FloaterContent));
        }

        public ICommand FlyoutBackCommand => this._flyoutBackCommand ??= new RelayCommand(this.BackFlyOut, this.CanBackFlyOut);

        public ICommand FlyoutCloseCommand => this._flyoutCloseCommand ??= new RelayCommand(this.CloseFlyOut);
        public ICommand FloaterCloseCommand => this._floaterCloseCommand ??= new RelayCommand(this.CloseFloater);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.navigationService.Navigate -= this.HandleNavigationServiceNavigate;
                this.eventAggregator.GetEvent<HomeFloaterCloseRequested>().Unsubscribe(this.OnFloaterCloseRequested);
                this.eventAggregator.GetEvent<HomeFolderTreeOpenEvent>().Unsubscribe(this.OnOpenTree);
                this.disposed = true;
            }
        }

        private void HandleNavigationServiceNavigate(object sender, NavigationEventArgs e)
        {
            this.Items.SetCurrentWorkspace(1);
        }

        private void OnFlyOutContentAdded(object _L_, FlyoutViewModel e)
        {
            this.FlyOutContent = e;
        }

        private void OnFloaterCloseRequested()
        {
            this.CloseFloater(null);
        }

        private void OpenSettings(object obj)
        {
            this.dialogService.ShowDialog<SettingViewModel>();
        }

        private async void OpenAdTreeAsync(object obj)
        {
            if (this.adTree == null)
            {
                this.adTree = this.adBrowserViewModelBuilder.Build(ADBrowserType.Principals);
                await this.adTree.InitializeAsync();
            }

            this.FloaterContent = this.adTree;
            this.FloaterVisible = true;
        }

        private async Task OpenTreeAsync(object obj)
        {
            if (this.folderTree == null)
            {
                this.folderTree = this.folderTreeViewModelBuilder.Build();
                this.folderTree.Standalone = true;
            }

            if (obj is not null && obj is string path)
            {
                this.folderTree.SelectedPath = path;
            }

            await this.folderTree.InitializeAsync();

            this.FloaterContent = this.folderTree;
            this.FloaterVisible = true;
        }

        private void CloseFlyOut(object obj)
        {
            this.FlyOutContent = null;
            this.flyOutService.RemoveAll();
        }

        private bool CanBackFlyOut(object obj)
        {
            return !this.flyOutService.LastInHistory;
        }

        private void BackFlyOut(object o_O)
        {
            this.FlyOutContent = this.flyOutService.GetPrevious();
        }

        private void CloseFloater(object obj)
        {
            this.folderTree?.Clear();

            this.FloaterVisible = false;
            this.FloaterContent = null;
        }

        public async Task InitializeAsync()
        {
            SplashViewModel splashViewModel = this.splashViewModelBuilder.Build();

            this.Items.Add(splashViewModel);
            this.Items.SetCurrentWorkspace(splashViewModel);

            await this.startUp.LoadAppSettings();

            this.Items.Remove(splashViewModel);

            ReportContainerViewModel reportContainer = this.reportContainerViewModelBuilder.Build();
            LandingViewModel landingContainer = this.landingViewModelBuilder.Build();

            this.Items.Add(landingContainer);
            this.Items.Add(reportContainer);

            this.Items.SetCurrentWorkspace(landingContainer);

            this.Commands.Add(new CommandViewModel(this.OpenSettings, HomeResource.SettingsCaption, "SettingsIcon", HomeResource.SettingsCaption));
            this.Commands.Add(new CommandViewModel(this.OpenTreeAsync, HomeResource.DirectoryTreeCaption, "TreeIcon", HomeResource.DirectoryTreeText));
            this.Commands.Add(new CommandViewModel(this.OpenAdTreeAsync, HomeResource.ADBrowserTreeCaption, "DomainTreeIcon", HomeResource.ADBrowserTreeText));

            this.eventAggregator.GetEvent<HomeFloaterCloseRequested>().Subscribe(this.OnFloaterCloseRequested);
            this.eventAggregator.GetEvent<HomeFolderTreeOpenEvent>().Subscribe(this.OnOpenTree);
        }

        private async void OnOpenTree(string path)
        {
            await this.OpenTreeAsync(path);
        }
    }
}