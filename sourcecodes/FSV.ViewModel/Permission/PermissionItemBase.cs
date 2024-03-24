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
    using System.Threading.Tasks;
    using Core;

    /// <summary>
    ///     Abstract class for items displayed at bottom of permission report.
    /// </summary>
    public abstract class PermissionItemBase : WorkspaceViewModel
    {
        private bool _isSelected;

        protected PermissionItemBase(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            this.FolderPath = folderPath;

            this.TitleCommands = new ObservableCollection<CommandViewModel>();
        }

        public string Icon { get; protected set; }
        public string FolderPath { get; }
        public string TourId { get; protected set; }
        public bool CanResize { get; protected set; }

        public IList<CommandViewModel> TitleCommands { get; }

        public bool IsSelected
        {
            get => this._isSelected;
            set
            {
                if (value == this._isSelected)
                {
                    return;
                }

                this._isSelected = value;
                this.RaisePropertyChanged(nameof(this.IsSelected));
                this.OnSelectedAsync(this._isSelected).FireAndForgetSafeAsync();
            }
        }

        protected virtual async Task OnSelectedAsync(bool selected)
        {
            await Task.CompletedTask;
        }
    }
}