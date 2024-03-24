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
    using Abstractions;
    using Configuration.Abstractions;
    using Configuration.Database;

    public class DatabaseManager : IDatabaseManager
    {
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private BaseConfiguration config;

        public DatabaseManager(IDatabaseConfigurationManager dbConfigurationManager)
        {
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
        }

        public void InitializeCurrentDatabase()
        {
            this.InitDatabase(this.dbConfigurationManager.Config);
        }

        /// <summary>
        ///     Use this to set change database based on user selected configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public void InitDatabase(BaseConfiguration configuration)
        {
            if (configuration == null || configuration.DatabaseProvider.Equals(DatabaseProviders.None))
            {
                string paramName = nameof(configuration);
                throw new ArgumentException(paramName, $"Either {paramName} is null or provider does not support database operations.");
            }

            this.config = configuration;
        }

        public BaseConfiguration GetCurrentDatabase()
        {
            return this.config;
        }
    }
}