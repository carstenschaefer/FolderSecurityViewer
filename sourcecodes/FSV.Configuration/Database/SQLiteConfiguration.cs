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

namespace FSV.Configuration.Database
{
    using System;
    using System.Data.SQLite;

    public sealed class SQLiteConfiguration : BaseConfiguration
    {
        public SQLiteConfiguration() : this(ProtectedConnectionString.IntegratedSecurity)
        {
        }

        public SQLiteConfiguration(ProtectedConnectionString connectionString) : base(DatabaseProviders.SQLite)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            this.ParseConnectionString(connectionString);
        }

        private void ParseConnectionString(ProtectedConnectionString connectionString)
        {
            var builder = new SQLiteConnectionStringBuilder(connectionString.GetDecryptedConnectionString());
            string dataSource = builder.DataSource;
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                dataSource = ConfigPath.GetUserFilePath("fsv.db");
            }

            this.DataSource = dataSource;
        }

        public override ProtectedConnectionString GetProtectedConnectionString()
        {
            var builder = new SQLiteConnectionStringBuilder { DataSource = this.DataSource };
            return ProtectedConnectionString.Create(builder.ConnectionString);
        }
    }
}