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

namespace FSV.Database.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using Abstractions;
    using Models;

    public class UserPermissionDetailReportRepository : BaseRepository, IUserPermissionDetailReportRepository
    {
        internal UserPermissionDetailReportRepository(FsvDataContext context) : base(context)
        {
        }

        public void Add(IList<UserPermissionReportDetail> userPermissionReportDetails)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.UserPermissionReportDetails.AddRange(userPermissionReportDetails);
            this.Context.SaveChanges();
        }

        public void DeleteAll(int reportId)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.UserPermissionReportDetails
                .RemoveRange(this.Context.UserPermissionReportDetails.Where(m => m.UserPermissionReportId == reportId).ToList());
            this.Context.SaveChanges();
        }

        public IList<UserPermissionReportDetail> GetAll(int reportId)
        {
            if (this.NoDb)
            {
                return new UserPermissionReportDetail[0];
            }

            return this.Context.UserPermissionReportDetails.Where(m => m.UserPermissionReportId == reportId).ToList();
        }

        public IList<UserPermissionReportDetail> Get(int reportId, string searchKey, Expression<Func<UserPermissionReportDetail, string>> sortExpression, bool ascending, int skip, int pageSize, out int total)
        {
            if (this.NoDb)
            {
                total = 0;
                return new UserPermissionReportDetail[0];
            }

            IQueryable<UserPermissionReportDetail> items = this.Context.UserPermissionReportDetails.Where(m => m.UserPermissionReportId == reportId);

            if (!string.IsNullOrEmpty(searchKey))
            {
                searchKey = $"%{searchKey}%";
                items = items.Where(m =>
                    DbFunctions.Like(m.SubFolder, searchKey) ||
                    DbFunctions.Like(m.Domain, searchKey) ||
                    DbFunctions.Like(m.CompleteName, searchKey) ||
                    DbFunctions.Like(m.OriginatingGroup, searchKey) ||
                    DbFunctions.Like(m.Permissions, searchKey));
            }

            if (sortExpression == null)
            {
                items = ascending ? items.OrderBy(m => m.Id) : items.OrderByDescending(m => m.Id);
            }
            else
            {
                items = ascending ? items.OrderBy(sortExpression) : items.OrderByDescending(sortExpression);
            }

            var selectItems = items.Select(m => new
            {
                DetailReport = m,
                TotalRows = items.Count()
            });

            if (skip > -1)
            {
                selectItems = selectItems.Skip(skip).Take(pageSize);
            }

            total = selectItems.FirstOrDefault()?.TotalRows ?? 0;

            return selectItems.Select(m => m.DetailReport).ToList();
        }
    }
}