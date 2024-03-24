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
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class NetworkViewModel : SettingWorkspaceViewModel
    {
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly ILogger<NetworkViewModel> logger;

        public NetworkViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IConfigurationManager configurationManager,
            ILogger<NetworkViewModel> logger) : base(dispatcherService, dialogService, true, true)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.DisplayName = ConfigurationResource.NetworkCaption;

            this.IsEnabled = !configurationManager.ConfigRoot.SettingLocked;
        }

        public bool NoProxy
        {
            get => NetworkConfigurationManager.ProxyType == ProxyType.None;
            set
            {
                NetworkConfigurationManager.ProxyType = ProxyType.None;
                this.RaisePropertyChanged(() => this.NoProxy, () => this.UseCustomProxySettings, () => this.UseDefaultProxySettings);
                if (value)
                {
                    this.ProxyServer = string.Empty;
                    this.ProxyPort = 0;
                    this.ProxyUser = string.Empty;
                    this.ProxyPassword = string.Empty;
                    this.UseCredentials = false;
                }
            }
        }

        public bool UseDefaultProxySettings
        {
            get => NetworkConfigurationManager.ProxyType == ProxyType.Default;
            set
            {
                NetworkConfigurationManager.ProxyType = ProxyType.Default;
                //this.RaisePropertyChanged(() => this.UseDefaultProxySettings);
                this.RaisePropertyChanged(() => this.NoProxy, () => this.UseCustomProxySettings, () => this.UseDefaultProxySettings);

                if (value)
                {
                    this.ProxyServer = string.Empty;
                    this.ProxyPort = 0;
                }
            }
        }

        public bool UseCustomProxySettings
        {
            get => NetworkConfigurationManager.ProxyType == ProxyType.Custom;
            set
            {
                NetworkConfigurationManager.ProxyType = ProxyType.Custom;
                //this.RaisePropertyChanged(() => this.UseCustomProxySettings);
                this.RaisePropertyChanged(() => this.NoProxy, () => this.UseCustomProxySettings, () => this.UseDefaultProxySettings);
            }
        }

        public string ProxyServer
        {
            get => NetworkConfigurationManager.ProxyServer;
            set
            {
                NetworkConfigurationManager.ProxyServer = value;
                this.RaisePropertyChanged(() => this.ProxyServer);
            }
        }

        public int ProxyPort
        {
            get => NetworkConfigurationManager.ProxyPort;
            set
            {
                NetworkConfigurationManager.ProxyPort = value;
                this.RaisePropertyChanged(() => this.ProxyPort);
            }
        }

        public bool UseCredentials
        {
            get => NetworkConfigurationManager.UseCredentials;
            set
            {
                NetworkConfigurationManager.UseCredentials = value;
                this.RaisePropertyChanged(() => this.UseCredentials);
            }
        }

        public string ProxyUser
        {
            get => NetworkConfigurationManager.Username;
            set
            {
                NetworkConfigurationManager.Username = value;
                this.RaisePropertyChanged(() => this.ProxyUser);
            }
        }

        public string ProxyPassword
        {
            get => NetworkConfigurationManager.Password;
            set
            {
                NetworkConfigurationManager.Password = value;
                this.RaisePropertyChanged(() => this.ProxyPassword);
            }
        }

        internal override async Task<bool> Save()
        {
            this.DoProgress();

            return await Task.Run(async () =>
            {
                try
                {
                    NetworkConfigurationManager.Save();
                    return true;
                }
                catch (Exception ex)
                {
                    const string errorMessage = "Failed to the network configuration due to an unhandled error.";
                    this.logger.LogError(ex, errorMessage);
                    await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(errorMessage));
                    return false;
                }
                finally
                {
                    this.StopProgress();
                }
            });
        }
    }
}