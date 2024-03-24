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

namespace FSV.AdServices.AbstractionLayer
{
    using System;
    using AdServices.Abstractions;
    using Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    public class ActiveDirectoryAbstractionLayerModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            services.AddTransient<IActiveDirectoryAbstractionService, ActiveDirectoryAbstractionService>();

            services.AddTransient<IAdAuthentication, AdAuthentication>();
            services.AddSingleton<IActiveDirectoryFinderFactory, ActiveDirectoryFinderFactory>();
            services.AddTransient<ActiveDirectory>();
            services.AddTransient<ActiveDirectoryFactory>();
            services.AddTransient<IActiveDirectoryGroupOperations, ActiveDirectoryGroupOperations>();
            services.AddTransient<IActiveDirectoryUtility, ActiveDirectoryUtility>();
            services.AddSingleton<IActiveDirectoryState, ActiveDirectoryState>();
            services.AddSingleton<IActiveDirectoryDomainsCache, ActiveDirectoryDomainsCache>();
            services.AddSingleton<IActiveDirectoryGroupsCache, ActiveDirectoryGroupsCache>();
            services.AddSingleton<IActiveDirectoryGroupInfoCache, ActiveDirectoryGroupInfoCache>();
            services.AddSingleton<IPrincipalContextCache, PrincipalContextCache>();
            services.AddTransient<ICurrentDomainCheckUtility, CurrentDomainCheckUtility>();
            services.AddTransient<IDomainInformationService, DomainInformationService>();
            services.AddTransient<IPrincipalContextFactory, PrincipalContextFactory>();

            services.AddTransient<ISearcher, Searcher>();
            services.AddTransient<Func<ISearcher>>(provider => provider.GetRequiredService<ISearcher>);
        }
    }
}