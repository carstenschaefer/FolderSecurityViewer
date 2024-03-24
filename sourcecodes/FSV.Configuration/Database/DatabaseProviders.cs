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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Defines supported database providers.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class DatabaseProviders
    {
        public const string SQLiteProviderName = "System.Data.SQLite";
        public const string SqlServerProviderName = "System.Data.SqlClient";

        public static DatabaseProvider None { get; } = new("None");
        public static DatabaseProvider SQLite { get; } = new(SQLiteProviderName);
        public static DatabaseProvider SqlServer { get; } = new(SqlServerProviderName);

        /// <summary>
        ///     Gets a list of <see cref="DatabaseProvider" /> objects representing all known providers.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DatabaseProvider> GetKnownProviders()
        {
            return new[]
            {
                SQLite,
                SqlServer
            };
        }
    }
}