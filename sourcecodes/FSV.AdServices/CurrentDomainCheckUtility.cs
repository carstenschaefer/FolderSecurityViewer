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
    using System.Diagnostics.CodeAnalysis;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
    using System.Linq;
    using Abstractions;
    using Microsoft.Extensions.Logging;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class CurrentDomainCheckUtility : ICurrentDomainCheckUtility
    {
        private readonly ILogger<CurrentDomainCheckUtility> logger;

        public CurrentDomainCheckUtility(ILogger<CurrentDomainCheckUtility> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsLocalAccountLoggedIn()
        {
            string userDomainName = this.GetUserDomainName();
            return string.IsNullOrEmpty(userDomainName);
        }

        public IEnumerable<string> GetAllDomainsOfForest()
        {
            using var forest = Forest.GetCurrentForest();
            return (from Domain domain in forest.Domains select domain.Name)
                .ToList()
                .AsReadOnly();
        }

        public string GetComputerDomainName()
        {
            try
            {
                var computerDomain = Domain.GetComputerDomain();
                return computerDomain?.Name;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to get computer domain name.");
            }

            return null;
        }

        public bool IsComputerJoinedAndConnectedToDomain()
        {
            try
            {
                var domain = Domain.GetComputerDomain();
                this.logger.LogInformation("The current domain of the machine is {DomainName}.", domain);
                return domain.Name?.Any() ?? false;
            }
            catch (ActiveDirectoryObjectNotFoundException e)
            {
                this.logger.LogError(e, "Failed to get computer domain name.");
                return false;
            }
        }

        public int CountUsersForestRoot()
        {
            var forest = Forest.GetCurrentForest();
            string rootName = forest.RootDomain.Name;

            using var root = new DirectoryEntry($"GC://{rootName}");
            using var searcher = new DirectorySearcher(root)
            {
                Filter = "(&(objectCategory=person)(objectClass=user)(!userAccountControl:1.2.840.113556.1.4.803:=2))",
                SearchScope = SearchScope.Subtree,
                PageSize = 10000,
                SizeLimit = 100000
            };

            using SearchResultCollection result = searcher.FindAll();
            return result.Count;
        }

        private string GetUserDomainName()
        {
            try
            {
                string domain = Environment.UserDomainName;
                string machineName = Environment.MachineName;

                if (machineName.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                return domain;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to get user domain name.");
                return string.Empty;
            }
        }
    }
}