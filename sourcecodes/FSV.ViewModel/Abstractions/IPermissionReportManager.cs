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

namespace FSV.ViewModel.Abstractions
{
    using System.Collections.Generic;
    using System.Data;
    using Database.Models;
    using Permission;

    public interface IPermissionReportManager
    {
        SavedReportItemViewModel Add(string user, string description, string folder, DataTable allPermissions, bool encrypt);
        void Update(PermissionReport report);
        void Delete(int id);

        /// <summary>
        ///     Gets a list of permission reports of given path.
        /// </summary>
        /// <param name="path">A path of directory.</param>
        /// <returns></returns>
        IList<SavedReportItemViewModel> Get(string path);

        IList<SavedReportItemViewModel> GetAll(string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total);

        /// <summary>
        ///     Gets paged list of permission detail items.
        /// </summary>
        /// <param name="id">A saved report id.</param>
        /// <param name="skip">Number of rows to skip.</param>
        /// <param name="pageSize">Number of rows to fetch</param>
        /// <param param name="total">Total rows available in database.</param>
        /// <returns></returns>
        IEnumerable<SavedReportDetailItemViewModel> GetAll(int id, int skip, int pageSize, out int total);

        IEnumerable<SavedReportDetailItemViewModel> GetAll(int id, string searchText, string sortColumn, bool ascending, int skip, int numberOfRows, out int total);

        /// <summary>
        ///     Gets all rows of permission detail items in DataTable.
        /// </summary>
        /// <param name="id">A report id.</param>
        /// <returns>A datatable.</returns>
        DataTable GetAll(int id);
    }
}