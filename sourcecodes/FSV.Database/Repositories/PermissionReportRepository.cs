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

    public class PermissionReportRepository : BaseRepository, IPermissionReportRepository
    {
        internal PermissionReportRepository(FsvDataContext context) : base(context)
        {
        }

        private string SearchKey { get; set; }

        public int Add(PermissionReport model)
        {
            if (this.NoDb)
            {
                return 0;
            }

            this.Context.PermissionReports.Add(model);
            this.Context.SaveChanges();
            return model.Id;
        }

        public IEnumerable<PermissionReport> GetAll(string folder)
        {
            if (this.NoDb)
            {
                return new PermissionReport[0];
            }

            return this.Context.PermissionReports.Where(m => m.Folder == folder).ToList();
        }

        public PermissionReport Get(int id)
        {
            if (this.NoDb)
            {
                return null;
            }

            return this.Context.PermissionReports.FirstOrDefault(m => m.Id == id);
        }

        public void Update(PermissionReport report)
        {
            if (this.NoDb)
            {
                return;
            }

            this.Context.Entry(report).State = EntityState.Modified;
            this.Context.SaveChanges();
        }

        public void Delete(int id)
        {
            if (this.NoDb)
            {
                return;
            }

            PermissionReport report = this.Context.PermissionReports.FirstOrDefault(m => m.Id == id);
            if (report != null)
            {
                this.Context.PermissionReports.Remove(report);
                this.Context.SaveChanges();
            }
        }

        [Obsolete]
        public IQueryable<PermissionReport> GetAll()
        {
            if (this.NoDb)
            {
                return new PermissionReport[0].AsQueryable();
            }

            return this.Context.PermissionReports;
        }

        public IEnumerable<PermissionReport> GetAll(int skip, int numberOfRows)
        {
            if (this.NoDb)
            {
                return new PermissionReport[0];
            }

            IQueryable<PermissionReport> items = this.Context.PermissionReports.OrderBy(m => m.Id).AsQueryable();
            if (numberOfRows > 0)
            {
                items = items.Skip(skip).Take(numberOfRows);
            }

            return items;
        }

        public IEnumerable<PermissionReport> GetAll<T>(Expression<Func<PermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows)
        {
            if (this.NoDb)
            {
                return new PermissionReport[0];
            }

            IQueryable<PermissionReport> items = this.Context.PermissionReports.AsQueryable();

            if (ascending)
            {
                items = this.Context.PermissionReports.OrderBy(orderExpression);
            }
            else
            {
                items = this.Context.PermissionReports.OrderByDescending(orderExpression);
            }

            if (numberOfRows > 0)
            {
                items = items.Skip(skip).Take(numberOfRows);
            }

            return items;
        }

        public IEnumerable<PermissionReport> GetAll<T>(string searchKey, Expression<Func<PermissionReport, T>> orderExpression, bool ascending, int skip, int numberOfRows, out int totalRows)
        {
            if (this.NoDb)
            {
                totalRows = 0;
                return new PermissionReport[0];
            }

            IQueryable<PermissionReport> items = this.Context.PermissionReports.AsQueryable();

            if (!string.IsNullOrEmpty(searchKey))
            {
                searchKey = $"%{searchKey}%";
                items = items.Where(m => DbFunctions.Like(m.Folder, searchKey)
                                         || DbFunctions.Like(m.User, searchKey)
                                         || DbFunctions.Like(m.Description, searchKey));
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

            return selectItems.AsEnumerable()
                .Select(m => m.Report);
        }
    }
}