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

namespace FSV.ViewModel.ShareReport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Passables;
    using Permission;
    using Resources;
    using ShareServices.Abstractions;

    public class ShareDetailViewModel : WorkspaceViewModel
    {
        private readonly IDispatcherService dispatcherService;
        private readonly INavigationService navigationService;
        private readonly IShareScannerFactory shareScannerFactory;

        private string _errorMessage;
        private ICommand _permissionReportCommand;

        internal ShareDetailViewModel(
            IDispatcherService dispatcherService,
            INavigationService navigationService,
            IShareScannerFactory shareScannerFactory,
            ServerShare serverShare)
        {
            if (serverShare == null)
            {
                throw new ArgumentNullException(nameof(serverShare));
            }

            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.shareScannerFactory = shareScannerFactory ?? throw new ArgumentNullException(nameof(shareScannerFactory));

            this.ServerName = serverShare.ServerName;
            this.DisplayName = serverShare.ShareName;

            this.Load().FireAndForgetSafeAsync();
        }

        private bool Loaded { get; set; }

        public ICommand PermissionReportCommand => this._permissionReportCommand ??= new RelayCommand(this.Report, p => this.Loaded && !string.IsNullOrEmpty(this.SharePath));

        public int ClientConnections { get; private set; }

        public string Description { get; private set; }

        public uint MaxUsers { get; private set; }
        public string Path { get; private set; }
        public string ServerName { get; }
        public IList<ShareDetailTrusteeViewModel> Trustees { get; private set; }

        public string ErrorMessage
        {
            get => this._errorMessage;
            set => this.DoSet(ref this._errorMessage, value, nameof(this.ErrorMessage));
        }

        public string SharePath { get; internal set; }

        private async Task Load()
        {
            this.DoProgress();

            this.ErrorMessage = string.Empty;

            try
            {
                IShareScanner shareScanner = this.shareScannerFactory.CreateShareScanner();

                Share shareDetail = await shareScanner.GetShareAsync(this.ServerName, this.DisplayName);
                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.Path = shareDetail.Path;
                    this.Description = shareDetail.Description;
                    this.ClientConnections = shareDetail.ClientConnections;
                    this.MaxUsers = shareDetail.MaxUsers;
                    this.Trustees = shareDetail.Trustees.Select(m => new ShareDetailTrusteeViewModel(m)).ToList();

                    this.RaisePropertyChanged(nameof(this.Description), nameof(this.MaxUsers), nameof(this.ClientConnections), nameof(this.Path), nameof(this.Trustees));
                    this.Loaded = true;
                });
            }
            catch (ShareLibException ex)
            {
                if (ex.ErrorCode == 53 || ex.ErrorCode == 2310)
                {
                    this.ErrorMessage = SharedServersResource.ShareNotFoundError;
                }
            }
            finally
            {
                this.StopProgress();
            }
        }

        private void Report(object obj)
        {
            this.navigationService.NavigateWithAsync<PermissionsViewModel>(this.SharePath);
        }
    }
}