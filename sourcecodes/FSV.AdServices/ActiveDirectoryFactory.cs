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
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;

    public sealed class ActiveDirectoryFactory
    {
        private readonly IActiveDirectoryAbstractionService abstractionService;
        private readonly IConfigurationManager configurationManager;
        private readonly ILoggerFactory loggerFactory;
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;

        public ActiveDirectoryFactory(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IActiveDirectoryAbstractionService abstractionService,
            IPrincipalContextFactory principalContextFactory,
            ILoggerFactory loggerFactory)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.abstractionService = abstractionService ?? throw new ArgumentNullException(nameof(abstractionService));
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public ActiveDirectory Create(FsvResults fsvResults, FsvColumnNames fsvColumnNames)
        {
            ILogger<ActiveDirectory> activeDirectoryLogger = this.loggerFactory.CreateLogger<ActiveDirectory>();
            return new ActiveDirectory(this.configurationManager, this.state, this.utility, this.abstractionService, this.principalContextFactory, this.loggerFactory, activeDirectoryLogger, fsvResults, fsvColumnNames);
        }

        public ActiveDirectory Create(FsvResults fsvResults, FsvColumnNames fsvColumnNames, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(username));
            }

            ILogger<ActiveDirectory> activeDirectoryLogger = this.loggerFactory.CreateLogger<ActiveDirectory>();
            return new ActiveDirectory(this.configurationManager, this.state, this.utility, this.abstractionService, this.principalContextFactory, this.loggerFactory, activeDirectoryLogger, fsvResults, fsvColumnNames, username);
        }
    }
}