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
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using System.Threading;
    using Abstractions;
    using Cache;
    using Configuration;
    using Microsoft.Extensions.Logging;
    using Models;

    /// <summary>
    /// </summary>
    /// <typeparam name="T">An object or value to pass during progress </typeparam>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ActiveDirectoryFinder<T> : IActiveDirectoryFinder
    {
        private readonly bool _excludeDisabledUsers;
        private readonly FsvColumnNames _fsvColumnNames;

        private readonly ActiveDirectoryScanResult<T> _scanResult;
        private readonly ILogger<ActiveDirectoryFinder<T>> logger;
        private readonly IActiveDirectoryState state;
        private readonly object syncObject = new();
        private readonly IActiveDirectoryUtility utility;

        private int _countingPermissions;

        /// <summary>
        ///     Initializes a new instance of ActiveDirectoryFinder.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger{TCategoryName}" /> that is used to trace log-events.</param>
        /// <param name="excludeDisabledUsers">Include or exclude disabled user from scan.</param>
        /// <param name="scanOptions">Details of scan options.</param>
        /// <param name="fsvColumnNames">Name of columns to use in result DataTable.</param>
        /// <param name="cancellationToken">A token to allow cancellation to </param>
        /// <param name="scanResult">An instance to keep the callables and result DataTable.</param>
        /// <param name="state">
        ///     An <see cref="IActiveDirectoryState" /> object that provides access to a global store (and also
        ///     caches).
        /// </param>
        internal ActiveDirectoryFinder(
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            ILogger<ActiveDirectoryFinder<T>> logger,
            bool excludeDisabledUsers,
            ActiveDirectoryScanOptions scanOptions,
            FsvColumnNames fsvColumnNames,
            ActiveDirectoryScanResult<T> scanResult)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._fsvColumnNames = fsvColumnNames;
            this._scanResult = scanResult ?? throw new ArgumentNullException(nameof(scanResult));

            this._excludeDisabledUsers = excludeDisabledUsers;

            this.ScanOptions = scanOptions ?? throw new ArgumentNullException(nameof(scanOptions));
        }

        public ActiveDirectoryScanOptions ScanOptions { get; }

        public void FindAdObject(
            string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            try
            {
                var parentGroupsList = new List<string>();

                string preferredDomain = this.utility.GetPreferredDomain(accountName);
                bool isLocalGroup = string.IsNullOrEmpty(preferredDomain) && ActiveDirectoryUtility.IsSId(accountName);

                if (isLocalGroup)
                {
                    this.ProcessLocalAccount(accountName, localGroupName, host, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                }
                else
                {
                    this.ProcessDomainAccount(preferredDomain, accountName, aclRight, fileSystemRight, host, localGroupName, originatingGroup, parentGroupsList, cancellationToken);
                }
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to find AD-object due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private void ProcessPrincipal(
            Principal principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                switch (principal.StructuralObjectClass)
                {
                    case "group":
                    {
                        var groupPrincipal = principal as GroupPrincipal;
                        var groupPrincipalInfo = GroupPrincipalInfo.CreateFrom(groupPrincipal);
                        this.ProcessGroupPrincipal(
                            groupPrincipalInfo,
                            parentGroupName,
                            domain,
                            aclRight,
                            fileSystemRight,
                            host,
                            originatingGroup,
                            parentGroupsList,
                            cancellationToken);
                        break;
                    }
                    case "user":
                    {
                        var principalInfo = PrincipalInfo.CreateFrom(principal);
                        this.AddUserToList(
                            principalInfo,
                            domain,
                            $"{fileSystemRight}, {aclRight}",
                            originatingGroup,
                            parentGroupsList, cancellationToken);
                        break;
                    }
                    case null:
                    {
                        var principalInfo = PrincipalInfo.CreateFrom(principal);
                        this.ProcessNullPrincipalObjectClass(
                            principalInfo,
                            parentGroupName,
                            domain,
                            aclRight,
                            fileSystemRight,
                            host,
                            originatingGroup,
                            parentGroupsList,
                            cancellationToken);
                        break;
                    }
                }
            }
            catch (ActiveDirectoryServiceException e)
            {
                const string errorMessage = "Failed to process principal due to an Active Directory error.";
                this.logger.LogError(e, errorMessage);
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to process principal due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
            }
        }

        private void AddUserToList(
            PrincipalInfo principalInfo,
            string domain,
            string origin,
            string originatingGroup,
            IEnumerable<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            using DirectoryEntryWrapper directoryEntryWrapper = principalInfo.ResolveDirectoryEntry();
            DirectoryEntry directoryEntry = directoryEntryWrapper.GetEntry();

            if (this._excludeDisabledUsers && ActiveDirectoryUtility.IsAccountDisabled(directoryEntry))
            {
                return;
            }

            DataRow newRow = this._scanResult.Result.NewRow();

            foreach (ConfigItem item in this.ScanOptions.PermissionGridColumns)
            {
                if (item.DisplayName.Equals(this._fsvColumnNames.OriginatingGroup)
                    || item.DisplayName.Equals(this._fsvColumnNames.Rigths)
                    || item.DisplayName.Equals(this._fsvColumnNames.Domain))
                {
                    continue;
                }

                object newVal;
                //property does not exist: ex. Not the best way to solve this...
                try
                {
                    newVal = directoryEntry != null ? directoryEntry.InvokeGet(item.Name) : principalInfo.Name;
                }
                catch (Exception)
                {
                    newVal = $"AD Property Name {item.Name} does not exist";
                }

                if (newVal == null)
                {
                    continue;
                }

                var valToSet = string.Empty;
                if (!(newVal is string) && newVal is IEnumerable innerValues)
                {
                    foreach (object innerVal in innerValues)
                    {
                        valToSet += $"{innerVal}|";
                    }
                }
                else
                {
                    valToSet = newVal.ToString();
                }

                newRow[item.DisplayName] = valToSet;
            }

            newRow[this._fsvColumnNames.OriginatingGroup] = originatingGroup;
            newRow[this._fsvColumnNames.Rigths] = this.ScanOptions.GetTranslatedRight(origin);
            newRow[FsvColumnConstants.PermissionGiListColumnName] = parentGroupsList.ToList();

            if (!string.IsNullOrEmpty(this._fsvColumnNames.Domain))
            {
                newRow[this._fsvColumnNames.Domain] = domain.ToUpper();
            }

            lock (this.syncObject)
            {
                this._scanResult.Result.Rows.Add(newRow);
            }

            this._scanResult.OnProgress(++this._countingPermissions, this._scanResult.Passable);
        }

        private void ProcessGroupPrincipal(
            GroupPrincipalInfo groupPrincipal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupList,
            CancellationToken cancellationToken)
        {
            if (!groupPrincipal.IsSecurityGroup)
            {
                return;
            }

            bool skipScan = this.ScanOptions.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid);

            if (skipScan)
            {
                return;
            }

            this.logger.LogDebug($"|Unbox Group: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            string groupCacheKey = this.GetGroupCacheKey(domain, groupPrincipal.Name);
            this.state.ActiveDirectoryGroupPrincipalCache.AddGroup(groupCacheKey, groupPrincipal);

            this.UnboxGroupMembers(groupPrincipal, aclRight, fileSystemRight, host, originatingGroup, parentGroupList, cancellationToken);
        }

        private void ProcessNullPrincipalObjectClass(
            PrincipalInfo principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            switch (principal.ContextType)
            {
                case ContextType.Domain:
                {
                    if (!(principal is GroupPrincipalInfo groupPrincipal))
                    {
                        return;
                    }

                    try
                    {
                        using DirectoryEntryWrapper entryWrapper = groupPrincipal.ResolveDirectoryEntry();
                        DirectoryEntry entry = entryWrapper.GetEntry();
                        if (!(entry.Invoke("Members") is IEnumerable members))
                        {
                            return;
                        }

                        foreach (object member in members)
                        {
                            using var memberEntry = new DirectoryEntry(member);
                            string accountName = memberEntry.Path.Replace("WinNT://", string.Empty).Replace("/", "\\");
                            var localGroupName = $"{groupPrincipal.Name}@{host}";
                            this.FindAdObject(accountName, aclRight, fileSystemRight, host, localGroupName, originatingGroup, cancellationToken);
                        }
                    }
                    catch (ActiveDirectoryServiceException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        var errorMessage = $"Failed to find account in group ({groupPrincipal.Name}) due to an unhandled error.";
                        this.logger.LogError(e, errorMessage);
                        throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
                    }

                    break;
                }
                case ContextType.Machine:
                    switch (principal)
                    {
                        case UserPrincipalInfo user:
                            this.AddUserToList(user, domain, $"{fileSystemRight}, {aclRight}", originatingGroup, parentGroupsList, cancellationToken);
                            break;
                        case GroupPrincipalInfo group:
                            this.ProcessGroupPrincipal(group, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                            break;
                    }

                    break;
            }
        }

        private void ProcessGroupCache(
            GroupPrincipalInfo groupPrincipal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupList,
            CancellationToken cancellationToken)
        {
            if (!groupPrincipal.IsSecurityGroup)
            {
                return;
            }

            bool skipScan = this.ScanOptions.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid);

            if (skipScan)
            {
                return;
            }

            this.logger.LogDebug($"|Unbox Group: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            this.UnboxGroupMembers(groupPrincipal, aclRight, fileSystemRight, host, originatingGroup, parentGroupList, cancellationToken);
        }

        private void ProcessDomainAccount(
            string preferredDomain,
            string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            IEnumerable<string> parentGroupList,
            CancellationToken cancellationToken)
        {
            string accountNameWithoutDomain = this.utility.GetAccountNameWithoutDomain(accountName);
            string groupCacheKey = this.GetGroupCacheKey(preferredDomain, accountNameWithoutDomain);

            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo groupPrincipalInfo))
            {
                string parentGroupName = groupPrincipalInfo.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                this.ProcessGroupCache(groupPrincipalInfo, parentGroupName, preferredDomain, aclRight, fileSystemRight, host, originatingGroup, parentGroupList, cancellationToken);
                return;
            }

            var contextKey = $"DOM_{preferredDomain}";
            if (this.state.PrincipalContextsCache.TryGetContext(contextKey, out PrincipalContextInfo domainContext) == false)
            {
                domainContext = this.utility.GetContext(preferredDomain);
                this.state.PrincipalContextsCache.AddContext(contextKey, domainContext);
            }

            if (this.utility.TryGetPrincipal(domainContext, IdentityType.SamAccountName, accountName, out Principal principal))
            {
                using (principal)
                {
                    var parentGroupName = string.Empty;
                    if (!string.IsNullOrEmpty(localGroupName))
                    {
                        parentGroupName = localGroupName;
                    }
                    else if (principal.StructuralObjectClass == "group")
                    {
                        parentGroupName = principal.Name;
                    }

                    this.ProcessPrincipal(principal, parentGroupName, preferredDomain, aclRight, fileSystemRight, host, originatingGroup, parentGroupList, cancellationToken);
                }
            }
        }

        private void ProcessLocalAccount(
            string accountName,
            string localGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IList<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            string accountNameWithoutDomain = this.utility.GetAccountNameWithoutDomain(accountName);
            string groupCacheKey = this.GetGroupCacheKey(domain, accountNameWithoutDomain);

            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo adGroupCache))
            {
                string parentGroupName = adGroupCache.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                this.ProcessGroupCache(adGroupCache, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                return;
            }

            var contextKey = $"MAC_{host}";
            if (this.state.PrincipalContextsCache.TryGetContext(contextKey, out PrincipalContextInfo machineContext) == false)
            {
                machineContext = this.utility.GetMachineContext(host);
                this.state.PrincipalContextsCache.AddContext(contextKey, machineContext);
            }

            if (this.utility.TryGetPrincipal(machineContext, IdentityType.Sid, accountName, out Principal principal))
            {
                using (principal)
                {
                    this.ProcessPrincipal(principal, localGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                }
            }
        }

        private void ProcessMemberCache(
            PrincipalInfo principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IList<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                switch (principal.MemberType)
                {
                    case PrincipalType.Group:
                        this.ProcessAdGroupMemberCache(
                            principal,
                            parentGroupName,
                            domain,
                            aclRight,
                            fileSystemRight,
                            host,
                            originatingGroup,
                            parentGroupsList,
                            cancellationToken);
                        break;
                    case PrincipalType.User:
                    {
                        this.AddUserToList(
                            principal,
                            domain,
                            $"{fileSystemRight}, {aclRight}",
                            originatingGroup,
                            parentGroupsList,
                            cancellationToken);
                        break;
                    }
                }
            }
            catch (ActiveDirectoryServiceException e)
            {
                const string errorMessage = "Failed to process principal due to an Active Directory error.";
                this.logger.LogError(e, errorMessage);
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to process principal due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
            }
        }

        private void ProcessAdGroupMemberCache(
            PrincipalInfo principalInfo,
            string localGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupsList,
            CancellationToken cancellationToken)
        {
            string groupName = principalInfo.Name;
            string groupCacheKey = this.GetGroupCacheKey(domain, groupName);
            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo adGroupCache))
            {
                string parentGroupName = adGroupCache.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                this.ProcessGroupCache(adGroupCache, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                return;
            }

            var contextKey = $"DOM_{domain}";
            if (this.state.PrincipalContextsCache.TryGetContext(contextKey, out PrincipalContextInfo domainContext) == false)
            {
                domainContext = this.utility.GetContext(domain);
                this.state.PrincipalContextsCache.AddContext(contextKey, domainContext);
            }

            if (this.utility.TryGetPrincipal(domainContext, IdentityType.SamAccountName, groupName, out Principal principal))
            {
                using (principal)
                {
                    var parentGroupName = string.Empty;
                    if (!string.IsNullOrEmpty(localGroupName))
                    {
                        parentGroupName = localGroupName;
                    }
                    else if (principal.StructuralObjectClass == "group")
                    {
                        parentGroupName = principal.Name;
                    }

                    this.ProcessPrincipal(principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, parentGroupsList, cancellationToken);
                }
            }
        }

        private void UnboxGroupMembers(
            GroupPrincipalInfo groupPrincipal,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            IEnumerable<string> parentGroupList,
            CancellationToken cancellationToken)
        {
            foreach (PrincipalInfo item in groupPrincipal.Members)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);

                var mutableParentGroupList = new List<string>(parentGroupList) { groupPrincipal.Name };
                this.ProcessMemberCache(item, groupPrincipal.Name, memberDomain, aclRight, fileSystemRight, host, originatingGroup, mutableParentGroupList.AsReadOnly(), cancellationToken);
            }
        }

        private string GetGroupCacheKey(string domain, string accountName)
        {
            return $"{domain}-{accountName}".ToLower();
        }
    }
}