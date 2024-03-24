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
    using System.Data.SqlClient;
    using System.Resources;
    using System.Security;
    using Configuration.Database;
    using Microsoft.Extensions.Logging;

    internal sealed class SqlServer : Database
    {
        private readonly SqlServerConfiguration config;
        private readonly ILogger log;

        internal SqlServer(
            ILogger log,
            SqlServerConfiguration config) : base(config, new ResourceManager(typeof(Queries.SqlServer)))
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (!config.DatabaseProvider.Equals(DatabaseProviders.SqlServer))
            {
                throw new ArgumentException(nameof(config), "Invalid provider in configuration.");
            }
        }

        internal override IDbConnection GetConnection()
        {
            System.Data.Entity.Database.SetInitializer<FsvDataContext>(null);

            var connection = new SqlConnection
            {
                ConnectionString = this.GetConnectionStringBuilder().ConnectionString
            };


            if (!this.config.IntegratedSecurity)
            {
                connection.Credential = new SqlCredential(this.config.UserID, this.config.Password);
            }

            return connection;
        }

        internal override void CreateDb()
        {
            using var connection = new SqlConnection();

            SqlConnectionStringBuilder builder = this.GetConnectionStringBuilder();

            builder.InitialCatalog = string.Empty;

            connection.ConnectionString = builder.ConnectionString;
            if (!this.config.IntegratedSecurity)
            {
                connection.Credential = new SqlCredential(this.config.UserID, this.config.Password);
            }

            // At this point, the connection string doesn't have Initial Catalog. The open method call may generate these errors:
            //  1. Invalid Data Source (-1). The connection object attempts to create connection but fails due to invalid server reference (ip and instance).
            //  2. Login Failed (18456). The user id or password is incorrect. Occurs when integrated security is false.
            //  3. Database doesn't exist (911).
            //  4. Database exists but user doesn't have access to db (916).
            connection.Open();
            // Connection created, means a user is valid.

            connection.ChangeDatabase(this.config.Database);
            // If ChangeDatabase fails, the user doesn't have permission to use the database or it may simply not exist.

            this.log.LogInformation("Changed the database into {Database} on {DataSource}.", this.config.Database, this.config.DataSource);

            //ChangeDatabase(connection, Config.Database);
            this.CreateSqlTables(connection);
        }

        // Not required at the moment. A sys admin will create db.
        private void ChangeDatabase(SqlConnection connection, string databaseName)
        {
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = "sp_databases";
            command.CommandType = CommandType.StoredProcedure;

            var hasDb = false;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(0).Equals(databaseName))
                    {
                        hasDb = true;
                        break;
                    }
                }

                reader.Close();
            }

            if (!hasDb)
            {
                //command.CommandText = "CREATE DATABASE " + databaseName;
                //command.CommandType = CommandType.Text;
                //command.ExecuteNonQuery();
            }

            connection.ChangeDatabase(databaseName);
        }

        private SqlConnectionStringBuilder GetConnectionStringBuilder()
        {
            var builder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = this.config.DataSource,
                ["Integrated Security"] = this.config.IntegratedSecurity,
                ["Initial Catalog"] = this.config.Database
            };

            if (!this.config.IntegratedSecurity)
            {
                builder.PersistSecurityInfo = true;
            }

            return builder;
        }

        private void CreateSqlTables(IDbConnection connection)
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = @"EXEC sp_tables 
                                        @table_name = '%',
	                                    @table_owner = 'dbo',
	                                    @table_type = ""'TABLE'""; ";

            var existingTables = new List<string>(2);

            using (IDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingTables.Add(reader["TABLE_NAME"].ToString());
                }

                reader.Close();
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

        internal override DbParameter CreateParameter()
        {
            return new SqlParameter();
        }

        internal static void Test(IDictionary<string, object> connectionStrings)
        {
            var builder = new SqlConnectionStringBuilder();
            builder.Add("Data Source", connectionStrings["Data Source"]);
            builder.Add("Integrated Security", connectionStrings["Integrated Security"]);

            using var connection = new SqlConnection(builder.ConnectionString);
            if (!string.IsNullOrEmpty(connectionStrings["User Id"] as string) && connectionStrings["Password"] != null)
            {
                connection.Credential = new SqlCredential(connectionStrings["User Id"].ToString(), connectionStrings["Password"] as SecureString);
            }

            connection.Open();

            connection.ChangeDatabase(connectionStrings["Initial Catalog"] as string);
            connection.Close();
        }
    }
}