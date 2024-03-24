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
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Security.Principal;
    using Abstractions;
    using Cache;
    using Microsoft.Extensions.Logging;

    public class ActiveDirectoryUtility : IActiveDirectoryUtility
    {
        private readonly ILogger<ActiveDirectoryUtility> logger;
        private readonly IPrincipalContextFactory principalContextFactory;
        private readonly IActiveDirectoryState state;

        public ActiveDirectoryUtility(
            IPrincipalContextFactory principalContextFactory,
            IActiveDirectoryState state,
            ILogger<ActiveDirectoryUtility> logger)
        {
            this.principalContextFactory = principalContextFactory ?? throw new ArgumentNullException(nameof(principalContextFactory));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PrincipalContextInfo GetMachineContext(string server)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(server));
            }

            return new PrincipalContextInfo(ContextType.Machine, server);
        }

        public PrincipalContextInfo GetContext(string domain = null)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return new PrincipalContextInfo(ContextType.Machine);
            }

            return new PrincipalContextInfo(ContextType.Domain, domain);
        }

        public string GetPreferredDomain(string name)
        {
            var preferredDomain = string.Empty;
            string[] accountSplit = name.Split('\\');

            // get preferred Domain
            string found;
            if (accountSplit.Length == 2)
            {
                string accountUpperCase = accountSplit[0].ToUpper();
                if (this.state.ActiveDirectoryDomainCache.TryGetDomain(accountUpperCase, out found))
                {
                    preferredDomain = found;
                }
                else
                {
                    string currentDomain = this.GetCurrentDomain().ToUpper();
                    if (this.state.ActiveDirectoryDomainCache.TryGetDomain(currentDomain, out found))
                    {
                        string currDom = found;
                        this.logger.LogDebug($"Builtin Group {name} located in {currDom}");
                        preferredDomain = currDom;
                    }
                }
            }
            else
            {
                string currentDomain = this.GetCurrentDomain().ToUpper();
                if (this.state.ActiveDirectoryDomainCache.TryGetDomain(currentDomain, out found))
                {
                    preferredDomain = found;
                }
            }

            return preferredDomain;
        }

        public string GetCurrentDomain()
        {
            try
            {
                return Environment.GetEnvironmentVariable("USERDOMAIN");
            }
            catch (Exception e)
            {
                const string errorMessage = "Error getting current domain due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
            }

            return string.Empty;
        }

        public UserPrincipal GetUserPrincipal(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(username));
            }

            string preferredDomain = this.GetPreferredDomain(username);

            var isLocalGroup = false;
            if (string.IsNullOrEmpty(preferredDomain))
            {
                isLocalGroup = IsSId(username);
            }

            if (isLocalGroup)
            {
                var principalContext = new PrincipalContext(ContextType.Machine);
                return Principal.FindByIdentity(principalContext, IdentityType.Sid, username) as UserPrincipal;
            }

            PrincipalContextInfo principalContextInfo = this.GetContext(preferredDomain);
            PrincipalContext context = this.principalContextFactory.CreateContext(principalContextInfo);
            return Principal.FindByIdentity(context, IdentityType.SamAccountName, username) as UserPrincipal;
        }

        public string GetNetBiosNameofDomain(string dnsForestDomainName, string dnsDomainName)
        {
            if (string.IsNullOrWhiteSpace(dnsForestDomainName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dnsForestDomainName));
            }

            if (string.IsNullOrWhiteSpace(dnsDomainName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dnsDomainName));
            }

            this.logger.LogDebug($"ENTER GetNetBIOSNameofDomain(). DNS Forest Domain Name: {dnsForestDomainName} DNS Domain Name: {dnsDomainName}");

            string cnDomain = this.ConvertDomainIntoCn(dnsForestDomainName);

            var containerLdapPath = $"LDAP://CN=Partitions,CN=Configuration,{cnDomain}";
            var netBiosName = string.Empty;

            SearchResultCollection searchResults = null;

            try
            {
                using var adSearch = new DirectorySearcher
                {
                    SearchRoot = new DirectoryEntry(containerLdapPath),
                    Filter = $"(&(objectClass=crossRef)(dnsRoot={dnsDomainName})(!cn=Enterprise*))",
                    SearchScope = SearchScope.Subtree,
                    SizeLimit = 200
                };

                // Info: in accordance to this documentation, the casing of the property name is correct: https://docs.microsoft.com/en-us/windows/win32/adschema/a-netbiosname
                const string netBiosNamePropertyName = "nETBIOSName";
                adSearch.PropertiesToLoad.AddRange(new[] { netBiosNamePropertyName, "cn" });

                try
                {
                    searchResults = adSearch.FindAll();
                }
                catch (Exception e)
                {
                    const string errorMessage = "Connection to configuration container in AD was not successful while getting NetBIOS Name of domain.";
                    this.logger.LogError(e, errorMessage);
                }

                if (searchResults.Count != 0)
                {
                    SearchResult firstResult = searchResults[0];
                    if (firstResult.Properties.Contains("cn"))
                    {
                    }

                    if (firstResult.Properties.Contains(netBiosNamePropertyName))
                    {
                        netBiosName = firstResult.Properties[netBiosNamePropertyName][0].ToString();
                    }
                    else
                    {
                        netBiosName = string.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                const string messageTemplate = "A general error occured while getting the NetBIOS name of the domain ({Domain}).";
                this.logger.LogError(e, messageTemplate, dnsDomainName);
            }
            finally
            {
                searchResults?.Dispose();
            }

            return netBiosName;
        }

        public string GetAccountNameWithoutDomain(string name)
        {
            if (!name.Contains("\\"))
            {
                return name;
            }

            string[] accountParts = name.Split('\\');
            return accountParts.Length > 1 ? accountParts[1] : name;
        }

        public bool TryGetPrincipal(
            PrincipalContextInfo principalContext,
            IdentityType identityType,
            string accountName,
            out Principal principal)
        {
            if (principalContext == null)
            {
                throw new ArgumentNullException(nameof(principalContext));
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            }

            try
            {
                PrincipalContext context = this.principalContextFactory.CreateContext(principalContext);
                principal = Principal.FindByIdentity(context, identityType, accountName);
                return principal != null;
            }
            catch (ArgumentException e)
            {
                var errorMessage = $"Failed to find {accountName}.";
                this.logger.LogError(e, errorMessage);
                principal = null;
                return false;
            }
        }

        public static bool IsSId(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sid));
            }

            if (sid.Split('-').Length != 8)
            {
                return false;
            }

            try
            {
                new SecurityIdentifier(sid);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAccountDisabled(DirectoryEntry user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            const string uac = "userAccountControl";
            string[] propertyNames = { uac };
            user.RefreshCache(propertyNames);

            if (user.NativeGuid == null)
            {
                return false;
            }

            static bool IsFlagSpecified(UserFlags allFlags, UserFlags specificFlag)
            {
                return (allFlags & specificFlag) == specificFlag;
            }

            if (user.Properties[uac]?.Value != null)
            {
                var userFlags = (UserFlags)user.Properties[uac].Value;
                return IsFlagSpecified(userFlags, UserFlags.AccountDisabled);
            }

            return false;
        }

        private string ConvertDomainIntoCn(string dnsDomainName)
        {
            var cnDomain = string.Empty;
            string[] domains = dnsDomainName.Split('.');

            try
            {
                for (var i = 0; i <= domains.Length - 1; i++)
                {
                    cnDomain = $"{cnDomain}DC={domains[i]}";
                    if (i < domains.Length - 1)
                    {
                        cnDomain += ",";
                    }
                }
            }
            catch (Exception e)
            {
                const string errorMessage = "An error occured while converting domain name into CN ({Domain}).";
                this.logger.LogError(e, errorMessage, dnsDomainName);
            }

            return cnDomain;
        }
    }
}