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

namespace FSV.ViewModel.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using AdBrowser;
    using AdMembers;
    using AdServices;
    using AdServices.Abstractions;
    using AdServices.EnumOU;
    using JetBrains.Annotations;
    using Models;

    public class AdBrowserService : IAdBrowserService
    {
        private readonly IActiveDirectoryGroupOperations adGroupOperations;
        private readonly ModelBuilder<AdTreeViewModel, IPrincipalViewModel, ComputerPrincipalViewModel> computerPrincipalModelBuilder;
        private readonly ModelBuilder<AdTreeViewModel, IPrincipalViewModel, PrincipalViewModel> principalViewModelBuilder;
        private readonly Func<ISearcher> searchFactory;

        public AdBrowserService(
            IActiveDirectoryGroupOperations adGroupOperations,
            ModelBuilder<AdTreeViewModel, IPrincipalViewModel, ComputerPrincipalViewModel> computerPrincipalModelBuilder,
            ModelBuilder<AdTreeViewModel, IPrincipalViewModel, PrincipalViewModel> principalViewModelBuilder,
            [NotNull] Func<ISearcher> searchFactory)
        {
            this.adGroupOperations = adGroupOperations ?? throw new ArgumentNullException(nameof(adGroupOperations));
            this.computerPrincipalModelBuilder = computerPrincipalModelBuilder ?? throw new ArgumentNullException(nameof(computerPrincipalModelBuilder));
            this.principalViewModelBuilder = principalViewModelBuilder ?? throw new ArgumentNullException(nameof(principalViewModelBuilder));
            this.searchFactory = searchFactory ?? throw new ArgumentNullException(nameof(searchFactory));
        }

        public async Task<IEnumerable<IPrincipalViewModel>> GetComputerPrincipalsAsync([NotNull] string text, [NotNull] IPrincipalViewModel parent)
        {
            if (parent is null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));
            }

            IEnumerable<ComputerPrincipalViewModel> queryOUs = await this.QueryComputersAsync(text, EnumOu.PrincialType.Ou, parent);
            IEnumerable<ComputerPrincipalViewModel> queryComputers = await this.QueryComputersAsync(text, EnumOu.PrincialType.Computer, parent);

            return queryOUs.Concat(queryComputers);
        }

        public async Task<IEnumerable<GroupMemberItemViewModel>> GetMembershipListAsync(string name, QueryType queryType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            IEnumerable<AdGroupMember> adGroupMembers = await this.adGroupOperations.GetMembershipListAsync(name, queryType);
            return adGroupMembers.Select(m => new GroupMemberItemViewModel(m));
        }

        public async Task<IEnumerable<GroupMemberItemViewModel>> GetMembersOfGroupAsync(string groupName, QueryType queryType)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException($"'{nameof(groupName)}' cannot be null or whitespace", nameof(groupName));
            }

            IEnumerable<AdGroupMember> adGroupMembers = await this.adGroupOperations.GetMemberOfGroupAsync(groupName, queryType);
            return adGroupMembers.Select(m => new GroupMemberItemViewModel(m));
        }

        public async Task<IEnumerable<IPrincipalViewModel>> GetPrincipalsAsync([NotNull] string text, [NotNull] IPrincipalViewModel parent)
        {
            if (parent is null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));
            }

            IEnumerable<PrincipalViewModel> queryOUs = await this.QueryPrincipalsAsync(text, EnumOu.PrincialType.Ou, parent);
            IEnumerable<PrincipalViewModel> queryGroups = await this.QueryPrincipalsAsync(text, EnumOu.PrincialType.Group, parent);
            IEnumerable<PrincipalViewModel> queryUsers = await this.QueryPrincipalsAsync(text, EnumOu.PrincialType.User, parent);

            return queryOUs.Concat(queryGroups).Concat(queryUsers);
        }

        public async Task<IEnumerable<AdTreeViewModel>> FindUsersAndGroupsAsync(string principalName)
        {
            string name = GetPrincipalNameWithoutDomain(principalName);
            ISearcher searcher = this.searchFactory();
            return await searcher.SearchAdUserAccountsLogonDisplayName(name);
        }

        private async Task<IEnumerable<ComputerPrincipalViewModel>> QueryComputersAsync(string text, EnumOu.PrincialType principalType, IPrincipalViewModel parent)
        {
            return await Task.Run(() => EnumOu.QueryOU(text, principalType, EnumOu.SearchLevel.OneLevel).Select(item => this.computerPrincipalModelBuilder.Build(item, parent)));
        }

        private async Task<IEnumerable<PrincipalViewModel>> QueryPrincipalsAsync(string text, EnumOu.PrincialType principalType, IPrincipalViewModel parent)
        {
            return await Task.Run(() => EnumOu.QueryOU(text, principalType, EnumOu.SearchLevel.OneLevel).Select(item => this.principalViewModelBuilder.Build(item, parent)));
        }

        private static string GetPrincipalNameWithoutDomain(string name)
        {
            if (name.Contains("*"))
            {
                return name;
            }

            string[] nameParts = name.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            return nameParts.Length > 1 ? nameParts[1] : name;
        }
    }
}