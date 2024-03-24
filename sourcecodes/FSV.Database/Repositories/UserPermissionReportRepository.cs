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

    public class UserPermissionReportRepository : BaseRepository, IUserPermissionReportRepository
    {
        internal UserPermissionReportRepository(FsvDataContext context) : base(context)
        {
        }

        public int Add(UserPermissionReport model)
        {
            if (this.NoDb)
            {
                return 0;
            }

            this.Context.UserPermissionReports.Add(model);
            this.Context.SaveChanges();
            return model.Id;
        }

        public IEnumerable<UserPermissionReport> GetAll<T>(string searchKey, Expression<Func<UserPermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows, out int totalRows)
        {
            if (this.NoDb)
            {
                totalRows = 0;
                return new UserPermissionReport[0];
            }

            IQueryable<UserPermissionReport> items = this.Context.UserPermissionReports.AsQueryable();

            if (!string.IsNullOrEmpty(searchKey))
            {
                searchKey = $"%{searchKey}%";
                items = items.Where(m => DbFunctions.Like(m.User, searchKey) || DbFunctions.Like(m.Folder, searchKey) || DbFunctions.Like(m.ReportUser, searchKey) || DbFunctions.Like(m.Description, searchKey));
            }

            if (ascending)
            {
                items = items.OrderBy(orderExpression);
            }
            else
            {
                items = items.OrderByDescending(orderExpression);
            }

            var selectItems = items.Select(m => new
            {
                Report = m,
                TotalRows = items.Count()
            });

            if (numberOfRows > 0)
            {
                selectItems = selectItems.Skip(skip).Take(numberOfRows);
            }

            totalRows = selectItems.FirstOrDefault()?.TotalRows ?? 0;

            return selectItems.Select(m => m.Report).AsEnumerable();
        }

        public IEnumerable<UserPermissionReport> GetAll<T>(string folder, string user, string searchKey, Expression<Func<UserPermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows, out int totalRows)
        {
            if (this.NoDb)
            {
                totalRows = 0;
                return new UserPermissionReport[0];
            }

            IQueryable<UserPermissionReport> items = this.Context.UserPermissionReports.Where(m => m.Folder == folder && m.ReportUser == user);

            if (!string.IsNullOrEmpty(searchKey))
            {
                searchKey = $"%{searchKey}%";
                items = items.Where(m => DbFunctions.Like(m.User, searchKey) || DbFunctions.Like(m.Description, searchKey));
            }

            if (ascending)
            {
                items = items.OrderBy(orderExpression);
            }
            else
            {
                items = items.OrderByDescending(orderExpression);
            }

            var selectItems = items.Select(m => new
            {
                Report = m,
                TotalRows = items.Count()
            });

            if (numberOfRows > 0)
            {
                selectItems = selectItems.Skip(skip).Take(numberOfRows);
            }

            totalRows = selectItems.FirstOrDefault()?.TotalRows ?? 0;

            return selectItems.Select(m => m.Report).AsEnumerable();
        }

        public void Update(UserPermissionReport report)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.Entry(report).State = EntityState.Modified;
            this.Context.SaveChanges();
        }

        public UserPermissionReport Get(int reportId)
        {
            if (this.NoDb)
            {
                return null;
            }

            return this.Context.UserPermissionReports.FirstOrDefault(m => m.Id == reportId);
        }

        public void Delete(int reportId)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.UserPermissionReports.Remove(this.Get(reportId));
            this.Context.SaveChanges();
        }
    }
}