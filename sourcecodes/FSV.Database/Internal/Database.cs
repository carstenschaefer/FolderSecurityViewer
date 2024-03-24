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
    using System.Data;
    using System.Data.Common;
    using System.Resources;
    using Configuration.Database;

    internal abstract class Database
    {
        [Obsolete("Use Tables variable. Will be removed in future versions.")]
        protected static readonly string TablePermissionReport = "PermissionReports";

        [Obsolete("Use Tables variable. Will be removed in future versions.")]
        protected static readonly string TablePermissionReportDetail = "PermissionReportDetails";

        [Obsolete("Use Tables variable. Will be removed in future versions.")]
        protected static readonly string TableUserPermissionReport = "UserPermissionReports";

        [Obsolete("Use Tables variable. Will be removed in future versions.")]
        protected static readonly string TableUserPermissionReportDetail = "UserPermissionReportDetails";

        protected static readonly string[] Tables = new string[4]
        {
            "PermissionReports", "PermissionReportDetails", "UserPermissionReports", "UserPermissionReportDetails"
        };

        protected Database(BaseConfiguration config, ResourceManager resource)
        {
            this.Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ResourceManager Resource { get; }

        public BaseConfiguration Config { get; }

        internal abstract IDbConnection GetConnection();

        internal abstract DbParameter CreateParameter();

        internal abstract void CreateDb();

        internal string GetString(string name)
        {
            return this.Resource?.GetString(name);
        }
    }
}