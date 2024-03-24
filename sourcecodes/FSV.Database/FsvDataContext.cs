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

namespace FSV.Database
{
    using System;
    using System.Data.Common;
    using System.Data.Entity;
    using Configuration.Database;
    using Models;
    using SQLite.CodeFirst;
    using Database = Internal.Database;

    internal class FsvDataContext : DbContext
    {
        private readonly Database databaseInfo;

        public FsvDataContext(Database databaseInfo, DbConnection connection) : base(connection, true)
        {
            this.databaseInfo = databaseInfo ?? throw new ArgumentNullException(nameof(databaseInfo));
            this.Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<PermissionReport> PermissionReports { get; set; }
        public DbSet<PermissionReportDetail> PermissionReportDetails { get; set; }

        public DbSet<UserPermissionReport> UserPermissionReports { get; set; }
        public DbSet<UserPermissionReportDetail> UserPermissionReportDetails { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            BaseConfiguration databaseInfoConfig = this.databaseInfo.Config;
            if (databaseInfoConfig.DatabaseProvider.Equals(DatabaseProviders.SQLite))
            {
                var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<FsvDataContext>(modelBuilder);
                System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
            }
            else
            {
                base.OnModelCreating(modelBuilder);
            }
        }
    }
}