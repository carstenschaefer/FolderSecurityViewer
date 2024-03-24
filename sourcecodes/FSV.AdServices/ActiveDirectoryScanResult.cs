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

namespace FSV.AdServices
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using Models;

    /// <summary>
    ///     Provides methods and properties to use during scan or after scan is complete.
    /// </summary>
    /// <typeparam name="T">An object or a value to pass as second argument of </typeparam>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ActiveDirectoryScanResult<T> : IActiveDirectoryResult, IActiveDirectoryScanActions<T>
    {
        public ActiveDirectoryScanResult(Action<int, T> onProgress) : this(onProgress, default)
        {
        }


        public ActiveDirectoryScanResult(Action<int, T> onProgress, T passable)
        {
            this.OnProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));
            this.Passable = passable;

            this.Result = new DataTable(FsvColumnConstants.PermissionTableName);
        }

        /// <summary>
        ///     A DataTable container to keep records of scan.
        /// </summary>
        public DataTable Result { get; set; }

        /// <summary>
        ///     A callable to invoke when progress state changed during scan.
        /// </summary>
        public Action<int, T> OnProgress { get; }

        /// <summary>
        ///     Gets or sets a value or an object to pass as second argument of OnProgress and OnComplete actions.
        /// </summary>
        public T Passable { get; set; }
    }
}