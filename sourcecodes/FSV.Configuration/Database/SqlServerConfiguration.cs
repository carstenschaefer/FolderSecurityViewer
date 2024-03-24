﻿// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
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
    using System.Data.SqlClient;
    using System.Security;
    using Crypto.Abstractions;

    public sealed class SqlServerConfiguration : BaseConfiguration
    {
        private readonly ISecure secure;

        public SqlServerConfiguration(ISecure secure) : this(ProtectedConnectionString.IntegratedSecurity, secure)
        {
        }

        public SqlServerConfiguration(ProtectedConnectionString connectionString, ISecure secure) : base(
            DatabaseProviders.SqlServer)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            this.secure = secure ?? throw new ArgumentNullException(nameof(secure));

            this.ParseConnectionString(connectionString);
        }

        public string Database { get; set; }

        public bool IntegratedSecurity { get; set; }

        public string UserID { get; set; }

        public SecureString Password { get; set; }

        private void ParseConnectionString(ProtectedConnectionString connectionString)
        {
            string decryptedConnectionString = connectionString.GetDecryptedConnectionString();
            var builder = new SqlConnectionStringBuilder(decryptedConnectionString);

            this.DataSource = builder.DataSource;
            this.Database = builder.InitialCatalog;
            this.IntegratedSecurity = connectionString == ProtectedConnectionString.IntegratedSecurity ||
                                      builder.IntegratedSecurity;

            if (!this.IntegratedSecurity)
            {
                this.UserID = builder.UserID;
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    this.Password = this.secure.DecryptToSecureString(builder.Password, false);
                }
            }
        }

        public override ProtectedConnectionString GetProtectedConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = this.DataSource,
                InitialCatalog = this.Database,
                IntegratedSecurity = this.IntegratedSecurity
            };

            if (!this.IntegratedSecurity)
            {
                builder.UserID = this.UserID;
                builder.Password = this.secure.EncryptFromSecureString(this.Password, false);
            }

            return ProtectedConnectionString.Create(builder.ToString());
        }
    }
}