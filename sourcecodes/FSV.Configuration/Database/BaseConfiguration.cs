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

    public abstract class BaseConfiguration
    {
        protected BaseConfiguration(DatabaseProvider databaseProvider)
        {
            this.DatabaseProvider =
                databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
        }

        /// <summary>
        ///     Gets or sets a value indicating the datasource.
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the current database configuration uses encryption, or not.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        ///     Gets a database provider to use the configuration for.
        /// </summary>
        public DatabaseProvider DatabaseProvider { get; }

        public abstract ProtectedConnectionString GetProtectedConnectionString();
    }
}