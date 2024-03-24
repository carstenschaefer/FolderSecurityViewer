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
    using Microsoft.Extensions.Logging;

    public class DatabaseUnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDatabaseManager dbManager;
        private readonly ILogger<DatabaseUnitOfWorkFactory> logger;

        public DatabaseUnitOfWorkFactory(
            IDatabaseManager dbManager,
            ILogger<DatabaseUnitOfWorkFactory> logger)
        {
            this.dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates a new <see cref="IUnitOfWork" /> instance using the current configured database provider.
        /// </summary>
        /// <returns>Returns a new <see cref="IUnitOfWork" /> instance.</returns>
        public IUnitOfWork Create()
        {
            try
            {
                FsvDataContext context = this.dbManager.GetContext(this.logger);
                return new UnitOfWork(context);
            }
            catch (NotSupportedException e)
            {
                var errorMessage = $"Failed to create {nameof(UnitOfWork)} instance due to an unhandled error. This error might be caused by an invalid ADO.NET provider configuration.";
                this.logger.LogError(e, errorMessage);
                throw new UnitOfWorkException($"{errorMessage} See inner exception for further details.", e);
            }
        }
    }
}