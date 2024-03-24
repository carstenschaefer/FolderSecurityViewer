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
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using System.Text;
    using AbstractionLayer.Abstractions;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;

    public partial class ActiveDirectory : IDisposable
    {
        private readonly FsvColumnNames _fsvColumnNames;
        private readonly FsvResults _fsvResults;
        private readonly IActiveDirectoryAbstractionService abstractionService;
        private readonly IConfigurationManager configurationManager;
        private readonly ILogger<ActiveDirectory> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;
        private string username;

        private UserPrincipal userPrincipal;

        public ActiveDirectory(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IActiveDirectoryAbstractionService abstractionService,
            IPrincipalContextFactory principalContextFactory,
            ILoggerFactory loggerFactory,
            ILogger<ActiveDirectory> logger)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.abstractionService = abstractionService ?? throw new ArgumentNullException(nameof(abstractionService));
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.TryInitiateDomainEnumeration();
        }

        public ActiveDirectory(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IActiveDirectoryAbstractionService abstractionService,
            IPrincipalContextFactory principalContextFactory,
            ILoggerFactory loggerFactory,
            ILogger<ActiveDirectory> logger,
            FsvResults fsvResults, FsvColumnNames fsvColumnNames) : this(configurationManager, state, utility, abstractionService, principalContextFactory, loggerFactory, logger)
        {
            this._fsvResults = fsvResults;
            this._fsvColumnNames = fsvColumnNames;
        }

        public ActiveDirectory(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IActiveDirectoryAbstractionService abstractionService,
            IPrincipalContextFactory principalContextFactory,
            ILoggerFactory loggerFactory,
            ILogger<ActiveDirectory> logger,
            FsvResults fsvResults, FsvColumnNames fsvColumnNames, string userName) : this(configurationManager, state,
            utility, abstractionService, principalContextFactory, loggerFactory, logger, fsvResults, fsvColumnNames)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));
            }

            this.SetUserName(userName);
        }

        public void Dispose()
        {
            this.userPrincipal?.Dispose();
        }

        private void SetUserName(string value)
        {
            if (this.username != null && this.username.Equals(value))
            {
                return;
            }

            this.username = value;
            this.userPrincipal = this.utility.GetUserPrincipal(this.username);
        }

        public void FindAdObject(
            Func<bool> isCancellationPending,
            string accountName,
            string aclRight, string fileSystemRight,
            string host,
            string localGroupName, string originatingGroup)
        {
            if (isCancellationPending == null)
            {
                throw new ArgumentNullException(nameof(isCancellationPending));
            }

            var parameters = new FindAdObjectOperation.Parameters(
                accountName,
                aclRight, fileSystemRight, host, localGroupName, originatingGroup,
                this._fsvResults, this._fsvColumnNames);

            ILogger<FindAdObjectOperation> operationLogger = this.loggerFactory.CreateLogger<FindAdObjectOperation>();
            var operation = new FindAdObjectOperation(this.configurationManager, this.state, this.utility, this.principalContextFactory, operationLogger,
                parameters);

            operation.Execute(isCancellationPending);
        }

        public bool FindAdObjectForUser(Func<bool> isCancellationPending, string directoryName, string accountName,
            string aclRight, string fileSystemRight, string host, string localGroupName, string originatingGroup)
        {
            if (isCancellationPending == null)
            {
                throw new ArgumentNullException(nameof(isCancellationPending));
            }

            if (this.userPrincipal == null || string.IsNullOrEmpty(directoryName))
            {
                return false;
            }

            if (this._fsvResults == null)
            {
                throw new InvalidOperationException("The current service instance has not been initialized using a valid results object.");
            }

            if (this._fsvColumnNames == null)
            {
                throw new InvalidOperationException("The current service instance has no column-lists.");
            }

            var parameters = new FindAdObjectForUserOperation.Parameters(
                directoryName, accountName,
                aclRight, fileSystemRight, host, localGroupName, originatingGroup,
                this._fsvResults, this._fsvColumnNames);

            ILogger<FindAdObjectForUserOperation> operationLogger = this.loggerFactory.CreateLogger<FindAdObjectForUserOperation>();
            var operation = new FindAdObjectForUserOperation(
                this.configurationManager, this.state, this.utility, this.principalContextFactory, operationLogger,
                this.userPrincipal, parameters);

            return operation.Execute(isCancellationPending);
        }

        public bool TryInitiateDomainEnumeration()
        {
            this.logger.LogDebug("Init all Domains in class ActiveDirectory");

            try
            {
                if (this.state.ActiveDirectoryDomainCache.HasDomains() == false)
                {
                    using IForest currentForest = this.abstractionService.GetCurrentForest();

                    this.logger.LogDebug($"Runs in Forest: {currentForest.Name}");

                    var domainLog = new StringBuilder();
                    foreach (IDomain domain in currentForest.GetDomains())
                    {
                        string domainName = domain.Name;
                        string friendlyName = this.utility.GetNetBiosNameofDomain(currentForest.Name, domainName);

                        var domainInfo = new CachedDomainInfo(friendlyName, domainName);
                        this.state.ActiveDirectoryDomainCache.AddDomain(domainInfo);

                        domainLog.Append($"{domain.Name}, ");
                        this.logger.LogDebug($"Available Domain: {domainName} -> FriendlyName: {friendlyName}");
                    }

                    this.logger.LogDebug($"Found Domains: {domainLog}");

                    return true;
                }
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to enumerate domains due to an unhandled error.";
                this.logger.LogCritical(e, errorMessage);
            }

            return false;
        }

        internal static string GetOuFromDn(string dn)
        {
            if (string.IsNullOrWhiteSpace(dn))
            {
                throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace", nameof(dn));
            }

            IEnumerable<string> elements = dn.Split(',').Where(s => s.StartsWith("OU"));
            IEnumerable<string> names = elements.Select(s => s.Split('=')[1]);
            return string.Join("/", names);
        }
    }
}