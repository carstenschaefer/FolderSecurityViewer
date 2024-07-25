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
    using System.DirectoryServices.AccountManagement;
    using System.DirectoryServices.ActiveDirectory;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Abstractions;
    using EnumOU;
    using Microsoft.Extensions.Logging;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class Searcher : ISearcher
    {
        private static readonly List<FsvDomain> _allDomains = new();
        private readonly ILogger<Searcher> logger;

        public Searcher(ILogger<Searcher> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.InitializeAllDomains();
        }

        public IEnumerable<string> SearchAdGroupAccounts(string search)
        {
            var result = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return result;
                }

                foreach (FsvDomain domain in _allDomains)
                {
                    using var principalContext = new PrincipalContext(ContextType.Domain, domain.NetBiosDomainName);

                    using var principal = new GroupPrincipal(principalContext)
                    {
                        Name = search
                    };

                    using var principalSearcher = new PrincipalSearcher
                    {
                        QueryFilter = principal
                    };

                    PrincipalSearchResult<Principal> principalSearchResult = principalSearcher.FindAll();
                    foreach (Principal princ in principalSearchResult)
                    {
                        if (!result.Contains(princ.SamAccountName))
                        {
                            result.Add(string.Concat(domain.NetBiosDomainName, @"\", princ.SamAccountName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to search Active-Directory group accounts due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                throw new ActiveDirectoryServiceException(errorMessage, ex);
            }

            return result;
        }

        public IEnumerable<string> SearchAdUserAccounts(string search)
        {
            var result = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return result;
                }

                foreach (FsvDomain domain in _allDomains)
                {
                    using var principalContext = new PrincipalContext(ContextType.Domain, domain.NetBiosDomainName);
                    using var principal = new UserPrincipal(principalContext)
                    {
                        SamAccountName = search
                    };

                    using var principalSearcher = new PrincipalSearcher
                    {
                        QueryFilter = principal
                    };

                    PrincipalSearchResult<Principal> principalSearchResult = principalSearcher.FindAll();
                    foreach (Principal princ in principalSearchResult)
                    {
                        if (!result.Contains(princ.SamAccountName))
                        {
                            result.Add(string.Concat(domain.NetBiosDomainName, @"\", princ.SamAccountName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to search for Active-Directory accounts due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further errors.", ex);
            }

            return result;
        }

        public async Task<IEnumerable<AdTreeViewModel>> SearchAdUserAccountsLogonDisplayName([JetBrains.Annotations.NotNull] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(search));
            }

            var result = new List<AdTreeViewModel>();

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return result;
                }

                foreach (FsvDomain domain in _allDomains)
                {
                    using var principalContext = new PrincipalContext(ContextType.Domain, domain.NetBiosDomainName);

                    IEnumerable<Principal> principals = await FindUsers(search, principalContext);

                    foreach (Principal principal in principals)
                    {
                        if (result.Any(m => m.DistinguishedName.Equals(principal.DistinguishedName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }

                        result.Add(new AdTreeViewModel
                        {
                            Type = principal is UserPrincipal ? TreeViewNodeType.User : TreeViewNodeType.Group,
                            SamAccountName = string.Concat(domain.NetBiosDomainName, @"\", principal.SamAccountName),
                            DisplayName = principal.DisplayName,
                            DistinguishedName = principal.DistinguishedName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to search for Active-Directory accounts using the given expression ({Expression}).", search);
                // throw new ActiveDirectoryServiceException("Failed to search for Active-Directory accounts using the given expression. See inner exception for further details.", ex);
            }

            return result;
        }

        public IEnumerable<string> SearchAdUserAccountsLdap(string search)
        {
            var result = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return result;
                }

                foreach (FsvDomain domain in _allDomains)
                {
                    result.AddRange(LdapUserSearcher(domain.Dn, search));
                }
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to search for Active-Directory user-accounts via LDAP.";
                this.logger.LogError(ex, errorMessage);
                throw new ActiveDirectoryServiceException($"{errorMessage} See inner exception for further details.", ex);
            }

            return result;
        }

        public IEnumerable<string> SearchAdAccounts(string search)
        {
            var result = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return result;
                }

                foreach (FsvDomain domain in _allDomains)
                {
                    using (var principalContext = new PrincipalContext(ContextType.Domain, domain.NetBiosDomainName))
                    {
                        using (var principalSearcher = new PrincipalSearcher())
                        {
                            using (Principal principal = new UserPrincipal(principalContext))
                            {
                                if (principal != null)
                                {
                                    principal.Name = search;
                                    principalSearcher.QueryFilter = principal;
                                }

                                PrincipalSearchResult<Principal> principalSearchResult = principalSearcher.FindAll();
                                if (principalSearchResult.Any())
                                {
                                    foreach (Principal princ in principalSearchResult)
                                    {
                                        if (!result.Contains(princ.Name))
                                        {
                                            result.Add(string.Concat(domain, @"\", princ.SamAccountName));
                                        }
                                    }

                                    continue;
                                }
                            }

                            using (Principal groupPrincipal = new GroupPrincipal(principalContext))
                            {
                                groupPrincipal.Name = search;
                                principalSearcher.QueryFilter = groupPrincipal;
                                IEnumerable<GroupPrincipal> groupPrincipalSearchResult = principalSearcher.FindAll().Select(m => m as GroupPrincipal);

                                if (groupPrincipalSearchResult.Any())
                                {
                                    foreach (GroupPrincipal princ in groupPrincipalSearchResult)
                                    {
                                        List<UserPrincipal> members = princ.Members.Where(m => m is UserPrincipal).Select(m => m as UserPrincipal).ToList();
                                        result = result.Union(members.Select(m => string.Concat(domain, @"\", m.SamAccountName))).ToList();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to search for Active-Directory accounts due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                throw new ActiveDirectoryServiceException(errorMessage, ex);
            }

            return result;
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public string GetNetBiosDomainName(string dnsDomainName)
        {
            if (string.IsNullOrWhiteSpace(dnsDomainName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dnsDomainName));
            }

            var rootDSE = new DirectoryEntry($"LDAP://{dnsDomainName}/RootDSE"); //{ AuthenticationType = AuthenticationTypes.Secure };

            PropertyValueCollection rootDseValueCollection = rootDSE.Properties["configurationNamingContext"];
            var configurationNamingContext = rootDseValueCollection.AsEnumerable().FirstOrDefault()?.ToString();

            var searchRoot = new DirectoryEntry($"LDAP://cn=Partitions,{configurationNamingContext}"); // { AuthenticationType = AuthenticationTypes.Secure };

            const string netBiosNamePropertyName = "netbiosname";

            var searcher = new DirectorySearcher(searchRoot) { SearchScope = SearchScope.OneLevel };

            searcher.PropertiesToLoad.Add(netBiosNamePropertyName);
            searcher.Filter = $"(&(objectcategory=Crossref)(dnsRoot={dnsDomainName})(netBIOSName=*))";

            SearchResult result = searcher.FindOne();
            if (result != null)
            {
                ResultPropertyValueCollection valueCollection = result.Properties[netBiosNamePropertyName];
                var netBiosDomainName = valueCollection.AsEnumerable().FirstOrDefault()?.ToString();
                return netBiosDomainName ?? string.Empty;
            }

            return string.Empty;
        }

        private void InitializeAllDomains()
        {
            this.logger.LogDebug("Init all Domains in Searcher");

            try
            {
                if (!_allDomains.Any())
                {
                    var currentForest = Forest.GetCurrentForest();
                    this.logger.LogDebug("Runs in Forest: {Forest}", currentForest.Name);

                    DomainCollection domainCollection = currentForest.Domains;

                    var domainLog = new StringBuilder();
                    foreach (Domain dom in domainCollection)
                    {
                        domainLog.Append(dom.Name + ", ");
                    }

                    this.logger.LogDebug($"Found Domains: {domainLog}");

                    foreach (Domain dom in domainCollection)
                    {
                        string netBiosName = this.GetNetBiosDomainName(dom.Name);

                        var fsvDomain = new FsvDomain
                        {
                            DnsDomainName = dom.Name,
                            NetBiosDomainName = netBiosName,
                            Dn = GetDnFromDns(dom.Name)
                        };

                        _allDomains.Add(fsvDomain);

                        this.logger.LogDebug($"Domain: {dom.Name} has these NetBios name: {netBiosName}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to initialize all domains.");
                throw new ActiveDirectoryServiceException("Failed to initialize all domains. See inner exception for further details.");
            }
        }

        private static async Task<IEnumerable<Principal>> FindUsers(string searchString, PrincipalContext domainContext)
        {
            var searchPrincipals = new List<IEnumerable<Principal>>(4)
            {
                await SearchPrincipalsAsync(new UserPrincipal(domainContext) { SamAccountName = searchString }),
                await SearchPrincipalsAsync(new UserPrincipal(domainContext) { DisplayName = searchString }),
                await SearchPrincipalsAsync(new GroupPrincipal(domainContext) { SamAccountName = searchString }),
                await SearchPrincipalsAsync(new GroupPrincipal(domainContext) { DisplayName = searchString })
            };

            return searchPrincipals.SelectMany(m => m);
        }

        private static async Task<IEnumerable<Principal>> SearchPrincipalsAsync(Principal queryPrincipal)
        {
            return await Task.Run(() =>
            {
                using var principalSearcher = new PrincipalSearcher();
                principalSearcher.QueryFilter = queryPrincipal;

                PrincipalSearchResult<Principal> items = principalSearcher.FindAll();
                return items.ToList();
            });
        }

        private static IEnumerable<string> LdapUserSearcher(string domain, string search)
        {
            var result = new List<string>();

            //string filter = string.Format("(&(objectCategory=person)(objectClass=user))(|(SAMAccountName={0})(distinguishedName={0}))", search);
            var filter = $"(&(objectCategory=person)(objectClass=user))(SAMAccountName={search})";

            using var root = new DirectoryEntry("LDAP://" + domain);
            using var searcher = new DirectorySearcher(root) { Filter = filter };
            searcher.PropertiesToLoad.Add("sAmAccountName");

            using SearchResultCollection adResult = searcher.FindAll();
            foreach (SearchResult entry in adResult)
            {
                ResultPropertyValueCollection sAmAccountNameProperty = entry.Properties["sAmAccountName"];
                string samAccountName = sAmAccountNameProperty?.ToString() ?? string.Empty;
                result.Add(string.Concat(domain, @"\", samAccountName));
            }

            return result;
        }

        private static string GetDnFromDns(string dnsDomainName)
        {
            return dnsDomainName.Split('.').Aggregate(string.Empty, (current, dc) => current + "dc=" + dc + ",").TrimEnd(',');
        }

        private class FsvDomain
        {
            public string DnsDomainName { get; set; }
            public string NetBiosDomainName { get; set; }
            public string Dn { get; set; }
        }
    }
}