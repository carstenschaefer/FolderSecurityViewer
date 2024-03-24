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
    using Abstractions;
    using Configuration.Database;
    using Internal;
    using Microsoft.Extensions.Logging;

    public static class DatabaseManagerExtensions
    {
        internal static FsvDataContext GetContext(this IDatabaseManager dbManager, ILogger log)
        {
            if (dbManager == null)
            {
                throw new ArgumentNullException(nameof(dbManager));
            }

            Database database = dbManager.GetDatabase(log);
            if (database == null)
            {
                throw new InvalidOperationException("Failed to find database.");
            }


            if (database.GetConnection() is DbConnection connection)
            {
                return new FsvDataContext(database, connection);
            }

            throw new InvalidOperationException("No database connection available.");
        }

        private static Database GetDatabase(this IDatabaseManager dbManager, ILogger log)
        {
            BaseConfiguration config = dbManager.GetCurrentDatabase();

            if (config == null)
            {
                return null;
            }

            if (config.DatabaseProvider.Equals(DatabaseProviders.SqlServer) && config is SqlServerConfiguration sqlServerConfiguration)
            {
                return new SqlServer(log, sqlServerConfiguration);
            }

            if (config.DatabaseProvider.Equals(DatabaseProviders.SQLite) && config is SQLiteConfiguration sqLiteConfiguration)
            {
                return new SQLite(log, sqLiteConfiguration);
            }

            return null;
        }

        public static void ChangeDatabase(this IDatabaseManager dbManager, BaseConfiguration configuration, ILogger log)
        {
            if (dbManager == null)
            {
                throw new ArgumentNullException(nameof(dbManager));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            dbManager.InitDatabase(configuration);
            Database database = dbManager.GetDatabase(log);
            database?.CreateDb();
        }

        public static void CreateNewTables(this IDatabaseManager dbManager, ILogger log)
        {
            dbManager.GetDatabase(log)?.CreateDb();
        }
    }
}