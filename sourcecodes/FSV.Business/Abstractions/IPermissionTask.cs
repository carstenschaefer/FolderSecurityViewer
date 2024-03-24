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

namespace FSV.Business.Abstractions
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    public interface IPermissionTask : IWorkerTask
    {
        /// <summary>
        ///     Runs permission scan asynchronously.
        /// </summary>
        /// <param name="path">A path to scan.</param>
        /// <param name="progressCallback">A delegate to report progress of path scan.</param>
        /// <returns>A task with System.Data.DataTable result. The result is null if scan is cancelled.</returns>
        Task<DataTable> RunAsync(string path, Action<int> progressCallback);

        /// <summary>
        ///     Clears AD Cache.
        /// </summary>
        void ClearADCache();
    }
}