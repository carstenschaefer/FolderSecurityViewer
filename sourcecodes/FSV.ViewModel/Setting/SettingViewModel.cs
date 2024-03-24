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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using System.Windows.Input;
    using Abstractions;
    using Configuration.Abstractions;
    using Core;
    using Resources;

    public sealed class SettingViewModel : WorkspaceViewModel
    {
        private readonly ModelBuilder<AboutViewModel> aboutViewModelBuilder;

        private readonly IConfigurationManager configurationManager;

        private readonly ModelBuilder<ConfigurationViewModel> configurationViewModelBuilder;

        private readonly ModelBuilder<DatabaseViewModel> databaseViewModelBuilder;

        private readonly IDispatcherService dispatcherService;

        private readonly ModelBuilder<LogViewModel> logViewModelBuilder;

        private readonly ModelBuilder<NetworkViewModel> networkViewModelBuilder;

        private readonly ModelBuilder<ReportViewModel> reportViewModelBuilder;

        private readonly ModelBuilder<SoftwareUpdateViewModel> softwareUpdateViewModelBuilder;

        private ConfigurationViewModel configuration;

        private SettingWorkspaceViewModel currentWorkspace;

        private ICommand saveCommand;

        public SettingViewModel(
            IDispatcherService dispatcherService,
            IConfigurationManager configurationManager,
            ModelBuilder<ConfigurationViewModel> configurationViewModelBuilder,
            ModelBuilder<ReportViewModel> reportViewModelBuilder,
            ModelBuilder<DatabaseViewModel> databaseViewModelBuilder,
            ModelBuilder<NetworkViewModel> networkViewModelBuilder,
            ModelBuilder<LogViewModel> logViewModelBuilder,
            ModelBuilder<AboutViewModel> aboutViewModelBuilder,
            ModelBuilder<SoftwareUpdateViewModel> softwareUpdateViewModelBuilder)
        {
            this.configurationViewModelBuilder = configurationViewModelBuilder ?? throw new ArgumentNullException(nameof(configurationViewModelBuilder));
            this.reportViewModelBuilder = reportViewModelBuilder ?? throw new ArgumentNullException(nameof(reportViewModelBuilder));
            this.databaseViewModelBuilder = databaseViewModelBuilder ?? throw new ArgumentNullException(nameof(databaseViewModelBuilder));
            this.networkViewModelBuilder = networkViewModelBuilder ?? throw new ArgumentNullException(nameof(networkViewModelBuilder));
            this.logViewModelBuilder = logViewModelBuilder ?? throw new ArgumentNullException(nameof(logViewModelBuilder));
            this.aboutViewModelBuilder = aboutViewModelBuilder ?? throw new ArgumentNullException(nameof(aboutViewModelBuilder));
            this.softwareUpdateViewModelBuilder = softwareUpdateViewModelBuilder ?? throw new ArgumentNullException(nameof(softwareUpdateViewModelBuilder));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            this.DisplayName = HomeResource.SettingsCaption;
            this.SettingItems = new ObservableCollection<WorkspaceViewModel>();
            this.IsEnabled = !this.configurationManager.ConfigRoot.SettingLocked;

            this.InitAsync().FireAndForgetSafeAsync();
        }

        public IList<WorkspaceViewModel> SettingItems { get; }

        public SettingWorkspaceViewModel CurrentWorkspace
        {
            get => this.currentWorkspace;
            set => this.Set(ref this.currentWorkspace, value, nameof(this.CurrentWorkspace));
        }

        public ICommand SaveCommand => this.saveCommand ??= new AsyncRelayCommand(this.SaveAsync);

        public bool IsEnabled { get; }

        private async Task SaveAsync(object obj)
        {
            if (this.CurrentWorkspace.UsesSave)
            {
                // CurrentWorkspace.SaveCommand?.Execute(null);
                bool result = await this.CurrentWorkspace.Save();
                if (result)
                {
                    this.CancelCommand?.Execute(null);
                }
            }
        }

        private void SetCurrentWorkspace(WorkspaceViewModel workspaceViewModel)
        {
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(this.SettingItems);

            collectionView?.MoveCurrentTo(workspaceViewModel);
        }

        private async Task InitAsync()
        {
            await this.dispatcherService.BeginInvoke(() =>
            {
                this.RegisterTabViewModels();
                this.SetCurrentWorkspace(this.configuration);
            });
        }

        private void RegisterTabViewModels()
        {
            this.configuration = this.configurationViewModelBuilder.Build();
            this.SettingItems.Add(this.configuration);

            this.SettingItems.Add(this.reportViewModelBuilder.Build());
            this.SettingItems.Add(this.networkViewModelBuilder.Build());
            this.SettingItems.Add(this.databaseViewModelBuilder.Build());
            this.SettingItems.Add(this.logViewModelBuilder.Build());
            this.SettingItems.Add(this.softwareUpdateViewModelBuilder.Build());
            this.SettingItems.Add(this.aboutViewModelBuilder.Build());
        }
    }
}