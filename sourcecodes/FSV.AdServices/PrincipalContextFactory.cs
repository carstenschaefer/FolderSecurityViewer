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

namespace FSV.AdServices
{
    using System;
    using System.DirectoryServices.AccountManagement;
    using Abstractions;
    using Cache;
    using Microsoft.Extensions.Logging;

    public sealed class PrincipalContextFactory : IPrincipalContextFactory
    {
        private readonly ILogger<PrincipalContextFactory> logger;

        public PrincipalContextFactory(ILogger<PrincipalContextFactory> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates a new <see cref="PrincipalContext" /> instance from the current object.
        /// </summary>
        public PrincipalContext CreateContext(PrincipalContextInfo principalContextInfo)
        {
            if (principalContextInfo == null)
            {
                throw new ArgumentNullException(nameof(principalContextInfo));
            }

            string name = principalContextInfo.Name;

            try
            {
                return principalContextInfo.CreateContext();
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to get PrincipalContext for domain ({name}) due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
            }

            return null;
        }
    }
}