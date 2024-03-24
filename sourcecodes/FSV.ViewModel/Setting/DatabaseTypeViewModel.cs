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
    using Abstractions;
    using Configuration.Database;

    public abstract class DatabaseTypeViewModel : WorkspaceViewModel
    {
        internal DatabaseTypeViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            DatabaseProvider databaseProvider)
        {
            this.DatabaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            this.DispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        protected IDispatcherService DispatcherService { get; }
        protected IDialogService DialogService { get; }

        public DatabaseProvider DatabaseProvider { get; }

        /// <summary>
        ///     Sets a conf
        /// </summary>
        /// <param name="config"></param>
        public abstract void SetConfig(BaseConfiguration config);

        /// <summary>
        ///     Gets a configuration object to saved it in xml file.
        /// </summary>
        /// <returns>FSV.Configuration.Database.BaseConfiguration</returns>
        public abstract BaseConfiguration GetConfig();
    }
}