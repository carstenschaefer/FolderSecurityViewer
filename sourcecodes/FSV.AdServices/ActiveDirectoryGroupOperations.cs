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
    using System.Threading.Tasks;
    using Abstractions;
    using Cache;
    using Models;

    internal class ActiveDirectoryGroupOperations : IActiveDirectoryGroupOperations
    {
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryUtility utility;

        public ActiveDirectoryGroupOperations(
            IActiveDirectoryUtility utility,
            IPrincipalContextFactory principalContextFactory)
        {
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
        }

        public async Task<IEnumerable<AdGroupMember>> GetMembershipListAsync(string name, QueryType queryType)
        {
            return await Task.Run(() => this.GetMembershipList(name, queryType));
        }

        public async Task<IEnumerable<AdGroupMember>> GetMemberOfGroupAsync(string groupName, QueryType queryType)
        {
            return await Task.Run(() => this.GetMemberOfGroup(groupName, queryType));
        }

        private IEnumerable<AdGroupMember> GetMembershipList(string name, QueryType queryType)
        {
            IdentityType idType = queryType == QueryType.SamAccountName ? IdentityType.SamAccountName : IdentityType.DistinguishedName;

            string currentDomain = this.utility.GetCurrentDomain();

            PrincipalContextInfo principalContextInfo = this.utility.GetContext(currentDomain);

            if (principalContextInfo is null)
            {
                throw new ActiveDirectoryServiceException($"Could not fetch PrincipalContext for domain {currentDomain}");
            }

            using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);
            using Principal principal = Principal.FindByIdentity(principalContext, idType, name);

            if (principal == null)
            {
                throw new ActiveDirectoryServiceException($"Principal {name} could not be found.");
            }

            static AdGroupMember CreateMembershipAdGroupMember(Principal principal)
            {
                return new AdGroupMember
                {
                    SamAccountName = principal.SamAccountName,
                    DisplayName = principal.Name,
                    DistinguishedName = principal.DistinguishedName,
                    DomainName = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(principal.DistinguishedName),
                    Ou = ActiveDirectory.GetOuFromDn(principal.DistinguishedName),
                    IsGroup = true
                };
            }

            return principal.GetGroups()
                .Where(m => m.StructuralObjectClass != null)
                .Select(CreateMembershipAdGroupMember)
                .ToList();
        }

        private IEnumerable<AdGroupMember> GetMemberOfGroup(string groupName, QueryType queryType)
        {
            var members = new List<AdGroupMember>();
            IdentityType idType = queryType == QueryType.SamAccountName ? IdentityType.SamAccountName : IdentityType.DistinguishedName;

            string currentDomain = this.utility.GetCurrentDomain();
            PrincipalContextInfo principalContextInfo = this.utility.GetContext(currentDomain);
            using PrincipalContext principalContext = this.principalContextFactory.CreateContext(principalContextInfo);

            if (principalContext is null)
            {
                throw new ActiveDirectoryServiceException($"Could not generate PrincipalContext for domain {currentDomain}");
            }

            using GroupPrincipal principal = GroupPrincipal.FindByIdentity(principalContext, idType, groupName);

            if (principal is null)
            {
                throw new ActiveDirectoryServiceException($"Group {groupName} could not be found.");
            }

            PrincipalSearchResult<Principal> memberOfGroup = principal.GetMembers();

            foreach (Principal p in memberOfGroup)
            {
                string structuralObjectClass = p.StructuralObjectClass;
                if (structuralObjectClass == null)
                {
                    continue;
                }

                if (structuralObjectClass.Equals("user") && p is UserPrincipal userPrincipal)
                {
                    var user = new AdGroupMember
                    {
                        SamAccountName = userPrincipal.SamAccountName,
                        DisplayName = userPrincipal.DisplayName,
                        DistinguishedName = userPrincipal.DistinguishedName,
                        DomainName = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(userPrincipal.DistinguishedName),
                        Ou = ActiveDirectory.GetOuFromDn(userPrincipal.DistinguishedName),
                        IsGroup = false
                    };
                    members.Add(user);
                }
                else if (structuralObjectClass.Equals("group") && p is GroupPrincipal groupPrincipal)
                {
                    var group = new AdGroupMember
                    {
                        SamAccountName = groupPrincipal.SamAccountName,
                        DisplayName = groupPrincipal.Name,
                        DistinguishedName = groupPrincipal.DistinguishedName,
                        DomainName = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(groupPrincipal.DistinguishedName),
                        Ou = ActiveDirectory.GetOuFromDn(groupPrincipal.DistinguishedName),
                        IsGroup = true
                    };
                    members.Add(group);
                }
            }

            return members;
        }
    }
}