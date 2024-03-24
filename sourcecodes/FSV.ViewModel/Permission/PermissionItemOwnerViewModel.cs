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
    using System.Threading.Tasks;
    using FileSystem.Interop.Abstractions;
    using Resources;

    public class PermissionItemOwnerViewModel : PermissionItemBase
    {
        private readonly IOwnerService ownerService;
        private string _ownerName;

        public PermissionItemOwnerViewModel(IOwnerService ownerService, string folderPath) : base(folderPath)
        {
            this.ownerService = ownerService ?? throw new ArgumentNullException(nameof(ownerService));
            this.DisplayName = PermissionResource.OwnerCaption;
            this.Icon = "OwnerIcon";

            this.LoadOwnerName();
        }

        public string OwnerName
        {
            get => this._ownerName;
            private set => this.Set(ref this._ownerName, value, nameof(this.OwnerName));
        }

        private void LoadOwnerName()
        {
            Task.Run(() => this.OwnerName = this.ownerService.GetNative(this.FolderPath));
        }
    }
}