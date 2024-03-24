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

namespace FSV.Database.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SQLite;
    using System.IO;
    using System.Resources;
    using Configuration.Database;
    using Microsoft.Extensions.Logging;

    internal sealed class SQLite : Database
    {
        private readonly ILogger log;

        internal SQLite(
            ILogger log,
            SQLiteConfiguration config) : base(config, new ResourceManager(typeof(Queries.SQLite)))
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            if (!config.DatabaseProvider.Equals(DatabaseProviders.SQLite))
            {
                throw new ArgumentException(nameof(config), "Invalid provider in configuration.");
            }
        }

        internal override IDbConnection GetConnection()
        {
            var connection = new SQLiteConnection { ConnectionString = "data source=" + this.Config.DataSource };
            return connection;
        }

        internal override void CreateDb()
        {
            if (!File.Exists(this.Config.DataSource))
            {
                SQLiteConnection.CreateFile(this.Config.DataSource);
            }

            using (var connection = new SQLiteConnection())
            {
                connection.ConnectionString = "data source=" + this.Config.DataSource;
                connection.Open();

                this.log.LogInformation("Database changed into {DataSource}", this.Config.DataSource);

                this.CreateSqlTables(connection);
            }
        }

        private void CreateSqlTables(IDbConnection connection)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";

                var existingTables = new List<string>(2);

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingTables.Add(reader[0].ToString());
                    }
                }

                foreach (string table in Tables)
                {
                    if (!existingTables.Contains(table))
                    {
                        command.CommandText = this.GetString(table);
                        command.ExecuteNonQuery();

                        this.log.LogInformation("{Table} table created successfully.", table);
                    }
                }
            }
        }

        internal override DbParameter CreateParameter()
        {
            return new SQLiteParameter();
        }
    }
}