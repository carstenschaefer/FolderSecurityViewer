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

    public class PermissionDetailReportRepository : BaseRepository, IPermissionDetailReportRepository
    {
        internal PermissionDetailReportRepository(FsvDataContext context) : base(context)
        {
        }

        public void Add(IList<PermissionReportDetail> list)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.PermissionReportDetails.AddRange(list);
            this.Context.SaveChanges();
        }

        public int GetTotalRows(int permissionReportId)
        {
            if (this.NoDb)
            {
                return 0;
            }

            return this.Context.PermissionReportDetails.Count(m => m.PermissionReportId == permissionReportId);
        }

        public IEnumerable<PermissionReportDetail> Get(int permissionReportId, int skip, int rows)
        {
            if (this.NoDb)
            {
                return new PermissionReportDetail[0];
            }

            if (skip > -1)
            {
                return this.Context.PermissionReportDetails.Where(m => m.PermissionReportId == permissionReportId).OrderBy(m => m.Id).Skip(skip).Take(rows);
            }

            return this.Context.PermissionReportDetails.Where(m => m.PermissionReportId == permissionReportId).OrderBy(m => m.Id);
        }

        public IEnumerable<PermissionReportDetail> Get<T>(int permissionReportId, string searchText, Expression<Func<PermissionReportDetail, T>> sortExpression, bool ascending, int skip, int rows, out int total)
        {
            if (this.NoDb)
            {
                total = 0;
                return new PermissionReportDetail[0];
            }

            IQueryable<PermissionReportDetail> items = this.Context.PermissionReportDetails.Where(m => m.PermissionReportId == permissionReportId);

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = $"%{searchText}%";
                items = items.Where(m =>
                    DbFunctions.Like(m.AccountName, searchText) ||
                    DbFunctions.Like(m.Department, searchText) ||
                    DbFunctions.Like(m.Division, searchText) ||
                    DbFunctions.Like(m.Email, searchText) ||
                    DbFunctions.Like(m.FirstName, searchText) ||
                    DbFunctions.Like(m.LastName, searchText) ||
                    DbFunctions.Like(m.OriginatingGroup, searchText) ||
                    DbFunctions.Like(m.Permissions, searchText));
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
                selectItems = selectItems.Skip(skip).Take(rows);
            }

            total = selectItems.FirstOrDefault()?.TotalRows ?? 0;

            return selectItems.AsEnumerable().Select(m => m.DetailReport);
        }

        public IEnumerable<PermissionReportDetail> GetAll(int permissionReportId)
        {
            if (this.NoDb)
            {
                return new PermissionReportDetail[0];
            }

            return this.Context.PermissionReportDetails.Where(m => m.PermissionReportId == permissionReportId).OrderBy(m => m.Id);
        }

        public void DeleteByReportId(int id)
        {
            if (this.NoDb)
            {
                return;
            }

            IQueryable<PermissionReportDetail> reportDetails = this.Context.PermissionReportDetails.Where(m => m.PermissionReportId == id);
            this.Context.PermissionReportDetails.RemoveRange(reportDetails);
            this.Context.SaveChanges();
        }
    }
}