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

namespace FSV.Database.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Models;

    public interface IUserPermissionReportRepository
    {
        int Add(UserPermissionReport model);
        IEnumerable<UserPermissionReport> GetAll<T>(string searchKey, Expression<Func<UserPermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows, out int totalRows);
        IEnumerable<UserPermissionReport> GetAll<T>(string folder, string user, string searchKey, Expression<Func<UserPermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows, out int totalRows);
        void Update(UserPermissionReport report);
        UserPermissionReport Get(int reportId);
        void Delete(int reportId);
    }
}