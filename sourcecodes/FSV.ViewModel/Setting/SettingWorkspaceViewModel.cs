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
    using System.Threading.Tasks;
    using Abstractions;

    public abstract class SettingWorkspaceViewModel : WorkspaceViewModel
    {
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private bool _isSelected;

        protected SettingWorkspaceViewModel(IDispatcherService dispatcherService, IDialogService dialogService)
            : this(dispatcherService, dialogService, false, false)
        {
        }

        protected SettingWorkspaceViewModel(bool usesSave, bool usesClose) : this(null, null, usesSave, usesClose)
        {
        }

        protected SettingWorkspaceViewModel(IDispatcherService dispatcherService, IDialogService dialogService, bool usesSave, bool usesClose)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.UsesSave = usesSave;
            this.UsesClose = usesClose;
        }

        public bool UsesClose { get; }

        public bool UsesSave { get; internal set; }

        public bool IsSelected
        {
            get => this._isSelected;
            set => this.Set(ref this._isSelected, value, nameof(this.IsSelected));
        }

        public bool IsEnabled { get; protected set; } = true;

        public Type CurrentType => this.GetType();

        /// <summary>
        ///     Saves current setting, and returns whether the setting window should close or remain open.
        /// </summary>
        /// <returns>True to close Setting dialog; otherwise false.</returns>
        internal virtual async Task<bool> Save()
        {
            return await Task.FromResult(true);
        }
    }
}