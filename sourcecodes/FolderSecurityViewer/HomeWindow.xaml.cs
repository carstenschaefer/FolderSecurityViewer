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

namespace FolderSecurityViewer
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Controls;
    using FSV.Configuration.Abstractions;
    using FSV.Extensions.WindowConfiguration;
    using FSV.Extensions.WindowConfiguration.Abstractions;
    using FSV.ViewModel.Abstractions;
    using FSV.ViewModel.Home;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Interaction logic for HomeWindow.xaml
    /// </summary>
    public partial class HomeWindow : CustomWindow
    {
        private readonly IDialogService _dialogService;
        private readonly HomeViewModel _homeViewModel;
        private readonly ILogger<HomeWindow> logger;
        private readonly IWindowConfigurationManager windowConfigurationManager;

        public HomeWindow(
            IDialogService dialogService,
            HomeViewModel viewModel,
            IWindowConfigurationManager windowConfigurationManager,
            ILogger<HomeWindow> logger)
        {
            this._dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this._homeViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            this.windowConfigurationManager = windowConfigurationManager ?? throw new ArgumentNullException(nameof(windowConfigurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.TryRestoreWindowPositionAndSize();

            this._dialogService.SetOwner(this);
            this.DataContext = viewModel;

            this.InitializeComponent();

            this.Loaded += this.HandleLoaded;
            this.Unloaded += this.HandleUnloaded;
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.HandleLoaded;
            this.Unloaded -= this.HandleUnloaded;
        }

        private async void HandleLoaded(object sender, RoutedEventArgs e)
        {
            await this._homeViewModel.InitializeAsync();
            if (this._homeViewModel.HasError)
            {
                this.Close();
            }
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            try
            {
                Position position = this.AsPosition();
                this.windowConfigurationManager.SetPosition(position);
                await this.windowConfigurationManager.SaveAsync();
            }
            catch (ConfigurationException ex)
            {
                this.logger.LogError(ex, "Failed to save window bounds.");
            }

            base.OnClosing(e);
        }

        private void TryRestoreWindowPositionAndSize()
        {
            try
            {
                this.windowConfigurationManager.Initialize();
                Position position = this.windowConfigurationManager.GetPosition();
                this.SetBoundsFromPosition(position);
            }
            catch (ConfigurationException ex)
            {
                this.SetBoundsFromPosition(Position.Empty);
                this.logger.LogError(ex, "Failed to load window bounds. Default bounds set.");
            }
        }
    }
}