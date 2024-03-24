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

    public sealed class FindAdObjectForUserOperation
    {
        private readonly IConfigurationManager configurationManager;
        private readonly ActiveDirectoryListBuilder listBuilder;
        private readonly ILogger<FindAdObjectForUserOperation> logger;
        private readonly Parameters parameters;
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryState state;
        private readonly UserPrincipal user;
        private readonly IActiveDirectoryUtility utility;

        public FindAdObjectForUserOperation(
            IConfigurationManager configurationManager,
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            IPrincipalContextFactory principalContextFactory,
            ILogger<FindAdObjectForUserOperation> logger,
            UserPrincipal user,
            Parameters parameters)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.user = user ?? throw new ArgumentNullException(nameof(user));

            this.listBuilder = new ActiveDirectoryListBuilder(this.configurationManager, parameters.FsvResults, parameters.FsvColumnNames);
        }

        public bool Execute(Func<bool> isCancellationRequested)
        {
            if (isCancellationRequested == null)
            {
                throw new ArgumentNullException(nameof(isCancellationRequested));
            }

            string directoryName = this.parameters.DirectoryName;
            string accountName = this.parameters.AccountName;
            string aclRight = this.parameters.AclRight;
            string fileSystemRight = this.parameters.FileSystemRight;
            string localGroupName = this.parameters.LocalGroupName;
            string originatingGroup = this.parameters.OriginatingGroup;
            string host = this.parameters.Host;

            string preferredDomain = this.utility.GetPreferredDomain(accountName);

            this.logger.LogDebug("|FindAdObjectForUser: Starts for: {AccountName} (domain: ) {PreferredDomain} |Folder: {DirectoryName}", accountName, preferredDomain, directoryName);

            if (this.state.ActiveDirectoryGroupsCache.HasGroup(accountName))
            {
                if (this.state.ActiveDirectoryGroupsCache.HasGroupWithAccountName(accountName, this.user.SamAccountName))
                {
                    var origin = $"{fileSystemRight}, {aclRight}";
                    const bool useDirEntry = true;
                    this.listBuilder.AddToList(this.user, directoryName, preferredDomain, localGroupName, origin, useDirEntry, originatingGroup);
                    this.logger.LogDebug("|FindAdObjectForUser (in cached groups): Added to list: {AccountName}", accountName);
                    return true;
                }

                return false;
            }

            try
            {
                var isLocalGroup = false;

                if (string.IsNullOrEmpty(preferredDomain))
                {
                    isLocalGroup = ActiveDirectoryUtility.IsSId(accountName); // assume that group is local
                }

                if (isLocalGroup)
                {
                    PrincipalContextInfo principalContextInfo = this.utility.GetMachineContext(host);
                    using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);
                    using Principal principal = Principal.FindByIdentity(principalContext, IdentityType.Sid, accountName);
                    if (principal != null)
                    {
                        return this.UnboxGroupForUser(isCancellationRequested, principal, localGroupName, host, aclRight, fileSystemRight, host, originatingGroup);
                    }
                }
                else
                {
                    PrincipalContextInfo principalContextInfo = this.utility.GetContext(preferredDomain);
                    using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);
                    using Principal principal = Principal.FindByIdentity(principalContext, IdentityType.SamAccountName, accountName);
                    if (principal != null)
                    {
                        return this.UnboxGroupForUser(isCancellationRequested, principal, localGroupName, preferredDomain, aclRight, fileSystemRight, host, originatingGroup);
                    }
                }

                this.logger.LogDebug($"|FindAdObjectForUser: {accountName}|Started Unboxing: {localGroupName}");
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to find AD-object for user {accountName} due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.");
            }

            return false;
        }

        private bool UnboxGroupForUser(
            Func<bool> isCancellationPending,
            Principal principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup)
        {
            if (isCancellationPending())
            {
                return false;
            }

            string directoryName = this.parameters.DirectoryName;

            try
            {
                switch (principal.StructuralObjectClass)
                {
                    case "group":
                        return this.UnboxStructuralObjectGroup(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup);

                    case "user":
                        return this.UnboxStructuralObjectUser(principal, parentGroupName, domain, aclRight, fileSystemRight, originatingGroup, directoryName);

                    case null:
                        return this.UnboxStructuralObject(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, directoryName);
                }
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to unbox group for user due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.");
            }

            return false;
        }

        private bool UnboxStructuralObject(
            Func<bool> isCancellationPending,
            Principal principal, string parentGroupName, string domain,
            string aclRight, string fileSystemRight, string host,
            string originatingGroup, string directoryName)
        {
            return principal.ContextType switch
            {
                ContextType.Domain => this.UnboxStructuralObjectDomainContext(isCancellationPending, principal, aclRight, fileSystemRight, host, originatingGroup, directoryName),
                ContextType.Machine => this.UnboxStructuralObjectMachineContext(isCancellationPending, principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, directoryName),
                _ => false
            };
        }

        private bool UnboxStructuralObjectMachineContext(
            Func<bool> isCancellationPending,
            Principal principal, string parentGroupName, string domain, string aclRight, string fileSystemRight, string host, string originatingGroup, string directoryName)
        {
            Type principalType = principal.GetType();

            if (principalType == typeof(UserPrincipal))
            {
                if (this.user.Sid != principal.Sid)
                {
                    return false;
                }

                var origin = $"{fileSystemRight}, {aclRight}";
                this.listBuilder.AddToList(principal, directoryName, domain, parentGroupName, origin, false, originatingGroup);
                return true;
            }

            if (principalType == typeof(GroupPrincipal))
            {
                if (!(principal is GroupPrincipal groupPrincipal) || !(bool)groupPrincipal.IsSecurityGroup)
                {
                    return false;
                }

                bool skipScan = this.parameters.FsvResults.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid.Value);
                if (skipScan)
                {
                    return false;
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

                this.logger.LogDebug($"|Unbox Group for User: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

                // get the members
                foreach (Principal item in groupPrincipal.Members)
                {
                    if (isCancellationPending())
                    {
                        break;
                    }

                    using (item)
                    {
                        string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);
                        return this.UnboxGroupForUser(isCancellationPending, item, groupPrincipal.Name, memberDomain, aclRight, fileSystemRight, host, originatingGroup);
                    }
                }

                historyItem.Duration = new TimeSpan(DateTime.Now.Ticks) - historyItem.Duration;

                this.parameters.FsvResults.WorkerResults.ScanHistory.Add(historyItem);
                this.parameters.FsvResults.AlreadyScannedGroups.Add(parentGroupName.ToLower());
            }

            return false;
        }

        private bool UnboxStructuralObjectDomainContext(Func<bool> isCancellationPending, Principal principal, string aclRight, string fileSystemRight, string host, string originatingGroup, string directoryName)
        {
            if (!(principal is GroupPrincipal groupPrincipal))
            {
                return false;
            }

            try
            {
                var entry = groupPrincipal.GetUnderlyingObject() as DirectoryEntry;
                if (entry?.Invoke("Members") is IEnumerable members)
                {
                    foreach (object member in members)
                    {
                        using var memberEntry = new DirectoryEntry(member);
                        string accountName = memberEntry.Path.Replace("WinNT://", string.Empty).Replace("/", "\\");

                        var localGroupName = $"{groupPrincipal.Name}@{host}";
                        var operationParameters = new Parameters(directoryName, accountName, aclRight, fileSystemRight, host, localGroupName, originatingGroup, this.parameters.FsvResults, this.parameters.FsvColumnNames);
                        var operation = new FindAdObjectForUserOperation(this.configurationManager, this.state, this.utility, this.principalContextFactory, this.logger, this.user, operationParameters);

                        return operation.Execute(isCancellationPending);
                    }
                }
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to unbox group ({groupPrincipal.Name}) due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.");
            }

            return false;
        }

        private bool UnboxStructuralObjectUser(
            Principal principal, string parentGroupName, string domain, string aclRight, string fileSystemRight, string originatingGroup, string directoryName)
        {
            if (this.user.Sid != principal.Sid)
            {
                return false;
            }

            var origin = $"{fileSystemRight}, {aclRight}";
            this.listBuilder.AddToList(principal, directoryName, domain, parentGroupName, origin, true, originatingGroup);
            return true;
        }

        private bool UnboxStructuralObjectGroup(
            Func<bool> isCancellationPending,
            Principal principal, string parentGroupName,
            string domain, string aclRight, string fileSystemRight, string host, string originatingGroup)
        {
            if (!(principal is GroupPrincipal groupPrincipal) || !groupPrincipal.IsSecurityGroup.HasValue || !groupPrincipal.IsSecurityGroup.Value)
            {
                return false;
            }

            bool skipScan = this.parameters.FsvResults.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid.Value);
            if (skipScan)
            {
                return false;
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

            this.logger.LogDebug($"|Unbox Group for User: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            List<string> accountNames = groupPrincipal.Members.Select(m => m.SamAccountName).ToList();
            this.state.ActiveDirectoryGroupsCache.AddAccountNames(originatingGroup, accountNames);

            foreach (Principal item in groupPrincipal.Members)
            {
                if (isCancellationPending())
                {
                    break;
                }

                string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);
                return this.UnboxGroupForUser(isCancellationPending, item, groupPrincipal.Name, memberDomain, aclRight, fileSystemRight, host, originatingGroup);
            }

            historyItem.Duration = new TimeSpan(DateTime.Now.Ticks) - historyItem.Duration;

            this.parameters.FsvResults.WorkerResults.ScanHistory.Add(historyItem);
            this.parameters.FsvResults.AlreadyScannedGroups.Add(parentGroupName.ToLower());

            return false;
        }

        public class Parameters
        {
            public Parameters(
                string directoryName, string accountName, string aclRight, string fileSystemRight, string host,
                string localGroupName, string originatingGroup,
                FsvResults fsvResults, FsvColumnNames fsvColumnNames)
            {
                this.DirectoryName = directoryName;
                this.AccountName = accountName;
                this.AclRight = aclRight;
                this.FileSystemRight = fileSystemRight;
                this.Host = host;
                this.LocalGroupName = localGroupName;
                this.OriginatingGroup = originatingGroup;

                this.FsvResults = fsvResults ?? throw new ArgumentNullException(nameof(fsvResults));
                this.FsvColumnNames = fsvColumnNames ?? throw new ArgumentNullException(nameof(fsvColumnNames));
            }

            public string DirectoryName { get; }
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