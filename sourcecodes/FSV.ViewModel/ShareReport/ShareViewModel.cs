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
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Permission;
    using Resources;
    using ShareServices.Models;

    public class ShareViewModel : WorkspaceViewModel
    {
        public static readonly ShareViewModel Loading = new(CommonResource.LoadingText, true);
        public static readonly ShareViewModel Empty = new(SharedServersResource.NoSharesAvailableCaption, false);
        public static readonly ShareViewModel EnumerationFailed = new(SharedServersResource.EnumerationFailedCaption, false);

        private readonly INavigationService navigationService;
        private ICommand _permissionReportCommand;

        private bool _selected;

        internal ShareViewModel(INavigationService navigationService, ShareItem share, string server)
        {
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.Element = share ?? throw new ArgumentNullException(nameof(share));

            this.DisplayName = share.Name;
            this.Path = share.Path;
            this.Share = $"\\\\{server}\\{this.DisplayName}";

            this.IsLoaded = true;
        }

        private ShareViewModel(string name, bool isWorking)
        {
            this.DisplayName = name;
            if (isWorking)
            {
                this.DoProgress();
            }
        }

        public ICommand PermissionReportCommand => this._permissionReportCommand ??= new RelayCommand(this.StartPermissionReport, p => this.IsLoaded);

        public string Path { get; }
        public string Share { get; }
        public bool IsLoaded { get; }

        public bool Selected
        {
            get => this._selected;
            set => this.Set(ref this._selected, value, nameof(this.Selected));
        }

        internal ShareItem Element { get; }

        private void StartPermissionReport(object p)
        {
            this.navigationService.NavigateWithAsync<PermissionsViewModel>(this.Share);
        }
    }
}