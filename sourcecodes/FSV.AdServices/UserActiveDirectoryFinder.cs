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
    using System.Data;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.IO;
    using System.Threading;
    using Abstractions;
    using Cache;
    using Microsoft.Extensions.Logging;
    using Models;

    /// <typeparam name="T">An object or value to pass during progress </typeparam>
    public class UserActiveDirectoryFinder<T> : IUserActiveDirectoryFinder
    {
        private readonly bool _excludeDisabledUsers;
        private readonly FsvColumnNames _fsvColumnNames;
        private readonly ActiveDirectoryScanResult<T> _scanResult;
        private readonly string _userName;

        private readonly UserPrincipal _userPrincipal;
        private readonly ILogger<UserActiveDirectoryFinder<T>> logger;

        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;

        private int _countingPermissions;

        /// <summary>
        ///     Initializes a new instance of ActiveDirectoryFinder.
        /// </summary>
        /// <param name="excludeDisabledUsers">Include or exclude disabled user from scan.</param>
        /// <param name="scanOptions">Details of scan options.</param>
        /// <param name="fsvColumnNames">Name of columns to use in result DataTable.</param>
        /// <param name="cancellationToken">A token to allow cancellation to </param>
        /// <param name="scanResult">An instance to keep the callables and result DataTable.</param>
        /// <param name="collectibleGroups">A collection of groups which have already been unboxed.</param>
        /// <param name="collectibleContexts">A collection of PrincipalContext to contain already opened contexts.</param>
        /// <param name="state">
        ///     An <see cref="ActiveDirectoryState" /> object that provides access to a global store (and also
        ///     caches).
        /// </param>
        /// <param name="logger">An <see cref="ILogger{TCategoryName}" /> that is used to trace log events.</param>
        internal UserActiveDirectoryFinder(
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            ILogger<UserActiveDirectoryFinder<T>> logger,
            string userName,
            bool excludeDisabledUsers,
            ActiveDirectoryScanOptions scanOptions,
            FsvColumnNames fsvColumnNames,
            ActiveDirectoryScanResult<T> scanResult)
        {
            this.ScanOptions = scanOptions ?? throw new ArgumentNullException(nameof(scanOptions));

            this._userName = !string.IsNullOrEmpty(userName) ? userName : throw new ArgumentNullException(nameof(userName));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._scanResult = scanResult ?? throw new ArgumentNullException(nameof(scanResult));
            this._fsvColumnNames = fsvColumnNames ?? throw new ArgumentNullException(nameof(fsvColumnNames));

            this._excludeDisabledUsers = excludeDisabledUsers;

            this._userPrincipal = utility.GetUserPrincipal(this._userName);
        }

        public ActiveDirectoryScanOptions ScanOptions { get; }

        public DirectoryInfo CurrentDirectory { get; set; } // TODO: this field should be removed, since it prevents thread-safety of the current class, or the field must become a readonly property

        public bool FindUser(
            string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            if (this._userPrincipal == null || this.CurrentDirectory == null)
            {
                return false;
            }

            try
            {
                string preferredDomain = this.utility.GetPreferredDomain(accountName);
                string currentUserSamAccountName = this._userPrincipal.SamAccountName;
                string accountNameWithoutDomain = this.utility.GetAccountNameWithoutDomain(accountName);
                string groupCacheKey = this.GetGroupCacheKey(preferredDomain, accountNameWithoutDomain);

                // If group exists in cache, find user account in it, and when available, add directly to result.
                if (this.state.ActiveDirectoryGroupPrincipalCache.HasGroupWithMember(groupCacheKey, currentUserSamAccountName))
                {
                    var origin = $"{fileSystemRight}, {aclRight}";
                    var principalInfo = UserPrincipalInfo.CreateFrom(this._userPrincipal);

                    return this.AddUserToList(
                        principalInfo,
                        preferredDomain,
                        origin,
                        originatingGroup,
                        cancellationToken);
                }

                bool isLocalGroup = string.IsNullOrEmpty(preferredDomain) && ActiveDirectoryUtility.IsSId(accountName);

                if (isLocalGroup)
                {
                    return this.ProcessLocalAccount(accountName, aclRight, fileSystemRight, host, localGroupName, originatingGroup, cancellationToken);
                }

                return this.ProcessDomainAccount(preferredDomain, accountName, aclRight, fileSystemRight, host, localGroupName, originatingGroup, cancellationToken);
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to find user due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private bool ProcessPrincipal(
            Principal principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                return principal.StructuralObjectClass switch
                {
                    "group" => this.ProcessGroupPrincipal(
                        principal as GroupPrincipal,
                        parentGroupName,
                        domain,
                        aclRight,
                        fileSystemRight,
                        host,
                        originatingGroup,
                        cancellationToken),
                    "user" => this.AddUserToList(
                        PrincipalInfo.CreateFrom(principal),
                        domain,
                        $"{fileSystemRight}, {aclRight}",
                        originatingGroup,
                        cancellationToken),
                    null => this.WhenPrincipalObjectClassIsNull(
                        principal,
                        parentGroupName,
                        domain,
                        aclRight,
                        fileSystemRight,
                        host,
                        originatingGroup,
                        cancellationToken),
                    _ => false
                };
            }
            catch (ActiveDirectoryServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to process principal due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryServiceException(errorMessage + " See inner exception for further details.", e);
            }
        }

        private bool AddUserToList(
            PrincipalInfo principalInfo,
            string domain,
            string origin,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (this._userPrincipal.Sid.Value != principalInfo.Sid)
            {
                return false;
            }

            using DirectoryEntryWrapper directoryEntryWrapper = principalInfo.ResolveDirectoryEntry();
            if (this._excludeDisabledUsers && ActiveDirectoryUtility.IsAccountDisabled(directoryEntryWrapper.GetEntry()))
            {
                return true; // User is found even if it is disabled. No need to continue the scan.
            }

            DataRow newRow = this._scanResult.Result.NewRow();

            newRow[this._fsvColumnNames.Folder] = this.CurrentDirectory.Name;
            newRow[this._fsvColumnNames.CompleteName] = this.CurrentDirectory.FullName;

            newRow[this._fsvColumnNames.OriginatingGroup] = originatingGroup;
            newRow[this._fsvColumnNames.Rigths] = this.ScanOptions.GetTranslatedRight(origin);

            if (!string.IsNullOrEmpty(this._fsvColumnNames.Domain))
            {
                newRow[this._fsvColumnNames.Domain] = domain.ToUpper();
            }

            this._scanResult.Result.Rows.Add(newRow);

            this._scanResult.OnProgress(++this._countingPermissions, this._scanResult.Passable);

            return true;
        }

        private bool ProcessGroupPrincipal(
            GroupPrincipal groupPrincipal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            if (!(bool)groupPrincipal.IsSecurityGroup)
            {
                return false;
            }

            bool skipScan = this.ScanOptions.CheckExclusionGroups(groupPrincipal.Name, groupPrincipal.Sid.Value);

            if (skipScan)
            {
                return false;
            }

            this.logger.LogDebug($"|Unbox Group: {groupPrincipal.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            string groupCacheKey = this.GetGroupCacheKey(domain, groupPrincipal.Name);
            var groupCacheInfo = GroupPrincipalInfo.CreateFrom(groupPrincipal);
            this.state.ActiveDirectoryGroupPrincipalCache.AddGroup(groupCacheKey, groupCacheInfo);

            return this.UnboxGroupMembers(groupCacheInfo, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
        }

        private bool WhenPrincipalObjectClassIsNull(
            Principal principal,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            if (principal.ContextType == ContextType.Domain)
            {
                if (!(principal is GroupPrincipal groupPrincipal))
                {
                    return false;
                }

                try
                {
                    if (!(groupPrincipal.GetUnderlyingObject() is DirectoryEntry entry))
                    {
                        return false;
                    }

                    if (!(entry.Invoke("Members") is IEnumerable members))
                    {
                        return false;
                    }

                    foreach (object member in members)
                    {
                        using var memberEntry = new DirectoryEntry(member);

                        string accountName = memberEntry.Path.Replace("WinNT://", string.Empty).Replace("/", "\\");

                        var localGroupName = $"{groupPrincipal.Name}@{host}";
                        bool result = this.FindUser(
                            accountName,
                            aclRight,
                            fileSystemRight,
                            host,
                            localGroupName,
                            originatingGroup,
                            cancellationToken);
                        if (result)
                        {
                            return true;
                        }
                    }
                }
                catch (ActiveDirectoryServiceException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to find user in group ({groupPrincipal.Name}) due to an unhandled error.";
                    this.logger.LogError(e, errorMessage);
                    throw new ActiveDirectoryServiceException(errorMessage + " See inner exception for further information.", e);
                }
            }
            else if (principal.ContextType == ContextType.Machine)
            {
                if (principal is UserPrincipal user)
                {
                    var userPrincipalInfo = UserPrincipalInfo.CreateFrom(user);
                    this.AddUserToList(
                        userPrincipalInfo,
                        domain,
                        $"{fileSystemRight}, {aclRight}",
                        originatingGroup,
                        cancellationToken);
                }
                else if (principal is GroupPrincipal group)
                {
                    return this.ProcessGroupPrincipal(
                        group,
                        parentGroupName,
                        domain,
                        aclRight,
                        fileSystemRight,
                        host,
                        originatingGroup,
                        cancellationToken);
                }
            }

            return false;
        }

        private bool ProcessDomainAccount(
            string preferredDomain,
            string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            string accountNameWithoutDomain = this.utility.GetAccountNameWithoutDomain(accountName);
            string groupCacheKey = this.GetGroupCacheKey(preferredDomain, accountNameWithoutDomain);

            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo adGroupCache))
            {
                string parentGroupName = adGroupCache.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                return this.ProcessGroupCache(adGroupCache, parentGroupName, preferredDomain, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
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

                    return this.ProcessPrincipal(
                        principal,
                        parentGroupName,
                        preferredDomain,
                        aclRight,
                        fileSystemRight,
                        host,
                        originatingGroup,
                        cancellationToken);
                }
            }

            return false;
        }

        private bool ProcessLocalAccount(
            string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            string accountNameWithoutDomain = this.utility.GetAccountNameWithoutDomain(accountName);
            string groupCacheKey = this.GetGroupCacheKey(host, accountNameWithoutDomain);

            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo adGroupCache))
            {
                string parentGroupName = adGroupCache.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                return this.ProcessGroupCache(adGroupCache, parentGroupName, host, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
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
                    return this.ProcessPrincipal(
                        principal,
                        localGroupName,
                        host,
                        aclRight,
                        fileSystemRight,
                        host,
                        originatingGroup,
                        cancellationToken);
                }
            }

            return false;
        }

        private bool UnboxGroupMembers(
            GroupPrincipalInfo group,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            foreach (PrincipalInfo item in group.Members)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string memberDomain = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(item.DistinguishedName);
                bool userFound = this.ProcessMemberCache(item, group.Name, memberDomain, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);

                if (userFound)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ProcessMemberCache(
            PrincipalInfo principalInfo,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (principalInfo.MemberType)
                {
                    case PrincipalType.Group:
                        return this.ProcessAdGroupMemberCache(
                            principalInfo.Name,
                            parentGroupName,
                            domain,
                            aclRight,
                            fileSystemRight,
                            host,
                            originatingGroup,
                            cancellationToken);
                    case PrincipalType.User:
                        return this.AddUserToList(
                            principalInfo,
                            domain,
                            $"{fileSystemRight}, {aclRight}",
                            originatingGroup,
                            cancellationToken);
                }
            }
            catch (ActiveDirectoryServiceException e)
            {
                this.logger.LogError(e, "Failed to process principal due to an Active Directory error.");
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to process principal due to an unhandled error.");
            }

            return false;
        }

        private bool ProcessAdGroupMemberCache(
            string adGroupName,
            string localGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            string groupCacheKey = this.GetGroupCacheKey(domain, adGroupName);
            if (this.state.ActiveDirectoryGroupPrincipalCache.TryGetGroup(groupCacheKey, out GroupPrincipalInfo adGroupCache))
            {
                string parentGroupName = adGroupCache.Name;
                if (!string.IsNullOrEmpty(localGroupName))
                {
                    parentGroupName = localGroupName;
                }

                return this.ProcessGroupCache(adGroupCache, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
            }

            var contextKey = $"DOM_{domain}";
            if (this.state.PrincipalContextsCache.TryGetContext(contextKey, out PrincipalContextInfo domainContext) == false)
            {
                domainContext = this.utility.GetContext(domain);
                this.state.PrincipalContextsCache.AddContext(contextKey, domainContext);
            }

            if (this.utility.TryGetPrincipal(domainContext, IdentityType.SamAccountName, adGroupName, out Principal principal))
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

                    return this.ProcessPrincipal(principal, parentGroupName, domain, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
                }
            }

            return false;
        }

        private bool ProcessGroupCache(
            GroupPrincipalInfo group,
            string parentGroupName,
            string domain,
            string aclRight,
            string fileSystemRight,
            string host,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            if (!group.IsSecurityGroup)
            {
                return false;
            }

            bool skipScan = this.ScanOptions.CheckExclusionGroups(group.Name, group.Sid);

            if (skipScan)
            {
                return false;
            }

            this.logger.LogDebug($"|Unbox Group: {group.Name} |Parent Group: {parentGroupName} |Domain: {domain}");

            return this.UnboxGroupMembers(group, aclRight, fileSystemRight, host, originatingGroup, cancellationToken);
        }

        private string GetGroupCacheKey(string domain, string accountName)
        {
            return $"{domain}-{accountName}".ToLower();
        }
    }
}