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
    using System.Collections;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using Abstractions;
    using Cache;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;

    public sealed class FindAdObjectOperation
    {
        private readonly IConfigurationManager configurationManager;
        private readonly ActiveDirectoryListBuilder listBuilder;
        private readonly ILogger<FindAdObjectOperation> logger;
        private readonly Parameters parameters;
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;

        public FindAdObjectOperation(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IPrincipalContextFactory principalContextFactory,
            ILogger<FindAdObjectOperation> logger,
            Parameters parameters)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            this.listBuilder = new ActiveDirectoryListBuilder(configurationManager, parameters.FsvResults, parameters.FsvColumnNames);
        }

        public void Execute(Func<bool> isCancellationPending)
        {
            if (isCancellationPending == null)
            {
                throw new ArgumentNullException(nameof(isCancellationPending));
            }

            string accountName = this.parameters.AccountName;
            string host = this.parameters.Host;
            string localGroupName = this.parameters.LocalGroupName;
            string aclRight = this.parameters.AclRight;
            string fileSystemRight = this.parameters.FileSystemRight;
            string originatingGroup = this.parameters.OriginatingGroup;

            try
            {
                string preferredDomain = this.utility.GetPreferredDomain(accountName);
                var parentGroupsList = new List<string>();
                var isLocalGroup = false;

                if (string.IsNullOrEmpty(preferredDomain))
                    // assume that group is local
                {
                    if (ActiveDirectoryUtility.IsSId(accountName))
                    {
                        isLocalGroup = true;
                    }
                }

                if (isLocalGroup)
                {
                    PrincipalContextInfo principalContextInfo = this.utility.GetMachineContext(host);
                    using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);
                    using Principal principal = Principal.FindByIdentity(principalContext, IdentityType.Sid, accountName);
                    if (principal != null)
                    {
                        this.UnboxGroup(isCancellationPending, principal, localGroupName, host, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList);
                    }
                }
                else
                {
                    PrincipalContextInfo principalContextInfo = this.utility.GetContext(preferredDomain);
                    using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);
                    using Principal principal = Principal.FindByIdentity(principalContext, IdentityType.SamAccountName, accountName);

                    if (principal == null)
                    {
                        return;
                    }

                    var parentGroupName = string.Empty;
                    if (principal.StructuralObjectClass == "group")
                    {
                        parentGroupName = principal.Name;
                    }

                    if (!string.IsNullOrEmpty(localGroupName))
                    {
                        parentGroupName = localGroupName;
                    }

                    // TODO: verify whether parentGroupName should be used instead of localGroupName
                    this.UnboxGroup(isCancellationPending, principal, localGroupName, preferredDomain, aclRight, fileSystemRight,
                        host, originatingGroup, parentGroupsList);
                }
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to find AD-object due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private void UnboxGroup(
            Func<bool> isCancellationPending,
            Principal principal, string parentGroupName, string domain,
            string aclRight, string fileSystemRight,
            string host, string originatingGroup,
            List<string> parentGroupsList)
        {
            try
            {
                if (isCancellationPending())
                {
                    return;
                }

                switch (principal.StructuralObjectClass)
                {
                    case "group":
                        this.UnboxStructuralObjectGroup(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList);
                        break;

                    case "user":
                        this.listBuilder.AddToList(principal, domain, parentGroupName, $"{fileSystemRight}, {aclRight}", true, originatingGroup, parentGroupsList);
                        break;

                    case null:
                        this.UnboxStructuralObject(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList);
                        break;
                }
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to unbox group due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private void UnboxStructuralObject(
            Func<bool> isCancellationPending,
            Principal principal, string parentGroupName, string domain, string aclRight, string fileSystemRight, string host, string originatingGroup,
            List<string> parentGroupsList)
        {
            if (principal.ContextType == ContextType.Domain)
            {
                this.UnboxStructuralObjectDomainContext(isCancellationPending, principal, aclRight, fileSystemRight, host, originatingGroup);
            }
            else if (principal.ContextType == ContextType.Machine)
            {
                this.UnboxStructuralObjectMachineContext(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList);
            }
        }

        private void UnboxStructuralObjectMachineContext(Func<bool> isCancellationPending, Principal principal, string parentGroupName, string domain, string aclRight, string fileSystemRight, string host, string originatingGroup, List<string> parentGroupsList)
        {
            if (principal.GetType() == typeof(UserPrincipal))
            {
                this.listBuilder.AddToList(principal, domain, parentGroupName, $"{fileSystemRight}, {aclRight}", false, originatingGroup, parentGroupsList);
            }

            if (principal.GetType() != typeof(GroupPrincipal))
            {
                return;
            }

            if (!(principal is GroupPrincipal groupPrincipal) || !(bool)groupPrincipal.IsSecurityGroup)
            {
                return;
            }

            bool skipScan = this.parameters.FsvResults.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid.Value);

            if (skipScan)
            {
                return;
            }

            string groupKey = groupPrincipal.Name.ToLower();
            if (this.parameters.FsvResults.AlreadyScannedGroups.Contains(groupKey))
            {
                return;
            }


            var friendlyDomainName = string.Empty;
            var historyItem = new ScanHistoryItem
            {
                Domain = domain,
                GroupName = groupPrincipal.Name,
                ParentGroupName = parentGroupName,
                FriendlyDomainName = friendlyDomainName,
                Duration = new TimeSpan(DateTime.Now.Ticks),
                MemberCount = groupPrincipal.Members.Count()
            };

            this.logger.LogDebug($"|Unbox Group: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            foreach (Principal item in groupPrincipal.Members)
            {
                if (isCancellationPending())
                {
                    break;
                }

                string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);

                this.UnboxGroup(isCancellationPending, item, groupPrincipal.Name, memberDomain,
                    aclRight, fileSystemRight, host, originatingGroup,
                    parentGroupsList);
            }

            historyItem.Duration = new TimeSpan(DateTime.Now.Ticks) - historyItem.Duration;

            this.parameters.FsvResults.WorkerResults.ScanHistory.Add(historyItem);

            this.parameters.FsvResults.AlreadyScannedGroups.Add(groupKey);
        }

        private void UnboxStructuralObjectDomainContext(Func<bool> isCancellationPending, Principal principal, string aclRight, string fileSystemRight, string host, string originatingGroup)
        {
            if (!(principal is GroupPrincipal group))
            {
                return;
            }

            try
            {
                var entry = group.GetUnderlyingObject() as DirectoryEntry;
                if (!(entry?.Invoke("Members") is IEnumerable members))
                {
                    return;
                }

                foreach (object member in members)
                {
                    using var memberEntry = new DirectoryEntry(member);
                    string accountName = memberEntry.Path.Replace("WinNT://", string.Empty).Replace("/", "\\");

                    var localGroupName = $"{group.Name}@{host}";
                    var operationParameters = new Parameters(accountName,
                        aclRight, fileSystemRight, host, localGroupName, originatingGroup,
                        this.parameters.FsvResults, this.parameters.FsvColumnNames);

                    var operation = new FindAdObjectOperation(this.configurationManager, this.state, this.utility, this.principalContextFactory, this.logger, operationParameters);
                    operation.Execute(isCancellationPending);
                }
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to unbox group ({group.Name}) due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private void UnboxStructuralObjectGroup(
            Func<bool> isCancellationPending,
            Principal principal,
            string parentGroupName, string domain, string aclRight, string fileSystemRight, string host, string originatingGroup,
            List<string> parentGroupsList)
        {
            if (!(principal is GroupPrincipal groupPrincipal) || !(bool)groupPrincipal.IsSecurityGroup)
            {
                return;
            }

            FsvResults fsvResults = this.parameters.FsvResults;
            bool skipScan = fsvResults.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid.Value);

            if (skipScan)
            {
                return;
            }

            var groupKey = $"{groupPrincipal.Name}@{domain.ToLower()}";
            if (fsvResults.AlreadyScannedGroups.Contains(groupKey))
            {
                return;
            }


            string currDomFriendlyName = this.state.ActiveDirectoryDomainCache.GetDomainFriendlyName(domain);
            var historyItem = new ScanHistoryItem
            {
                Domain = domain,
                GroupName = groupPrincipal.Name,
                ParentGroupName = parentGroupName,
                FriendlyDomainName = currDomFriendlyName,
                Duration = new TimeSpan(DateTime.Now.Ticks),
                MemberCount = groupPrincipal.Members.Count()
            };

            this.logger.LogDebug($"|Unbox Group: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            // get the members
            foreach (Principal item in groupPrincipal.Members)
            {
                if (isCancellationPending())
                {
                    break;
                }

                string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);
                parentGroupsList.Add(groupPrincipal.Name);
                this.UnboxGroup(isCancellationPending, item, groupPrincipal.Name, memberDomain,
                    aclRight, fileSystemRight, host, originatingGroup,
                    parentGroupsList);

                parentGroupsList.Remove(groupPrincipal.Name);
            }

            historyItem.Duration = new TimeSpan(DateTime.Now.Ticks) - historyItem.Duration;

            fsvResults.WorkerResults.ScanHistory.Add(historyItem);
            fsvResults.AlreadyScannedGroups.Add(groupKey);
        }

        public class Parameters
        {
            public Parameters(string accountName, string aclRight, string fileSystemRight, string host, string localGroupName, string originatingGroup, FsvResults fsvResults, FsvColumnNames fsvColumnNames)
            {
                this.AccountName = accountName;
                this.AclRight = aclRight;
                this.FileSystemRight = fileSystemRight;
                this.Host = host;
                this.LocalGroupName = localGroupName;
                this.OriginatingGroup = originatingGroup;

                this.FsvResults = fsvResults ?? throw new ArgumentNullException(nameof(fsvResults));
                this.FsvColumnNames = fsvColumnNames ?? throw new ArgumentNullException(nameof(fsvColumnNames));
            }

            public string AccountName { get; }
            public string AclRight { get; }
            public string FileSystemRight { get; }
            public string Host { get; }
            public string LocalGroupName { get; }
            public string OriginatingGroup { get; }
            public FsvResults FsvResults { get; }
            public FsvColumnNames FsvColumnNames { get; }
        }
    }
}