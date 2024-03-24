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
    using System.Threading.Tasks;
    using Database.Models;
    using UserReport;

    public interface IUserPermissionReportManager
    {
        UserPermissionReport Add(string user, string description, string folder, string reportUser, DataTable allFolders, bool encrypt);
        void Update(UserPermissionReport report);
        void Delete(int reportId);
        DataTable GetAll(int id);
        IEnumerable<UserPermissionReport> GetAll(string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total);
        IEnumerable<UserPermissionReport> GetAll(string folder, string user, string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total);
        IEnumerable<SavedFolderItemViewModel> GetAllReportItems(int reportId, string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total);
        Task<IEnumerable<UserReportDetailListItemViewModel>> GetReportAsync(int userReportId);
    }
}