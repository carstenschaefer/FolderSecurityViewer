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
    using System.Diagnostics.CodeAnalysis;
    using Abstractions;

    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public sealed class ActiveDirectoryState : IActiveDirectoryState
    {
        public ActiveDirectoryState(
            IActiveDirectoryDomainsCache activeDirectoryDomainsCache,
            IActiveDirectoryGroupsCache activeDirectoryGroupsCache,
            IActiveDirectoryGroupInfoCache activeDirectoryGroupPrincipalCache,
            IPrincipalContextCache principalContextsCache)
        {
            this.ActiveDirectoryDomainCache = activeDirectoryDomainsCache ?? throw new ArgumentNullException(nameof(activeDirectoryDomainsCache));
            this.ActiveDirectoryGroupsCache = activeDirectoryGroupsCache ?? throw new ArgumentNullException(nameof(activeDirectoryGroupsCache));
            this.ActiveDirectoryGroupPrincipalCache = activeDirectoryGroupPrincipalCache ?? throw new ArgumentNullException(nameof(activeDirectoryGroupPrincipalCache));
            this.PrincipalContextsCache = principalContextsCache ?? throw new ArgumentNullException(nameof(principalContextsCache));
        }

        public IActiveDirectoryDomainsCache ActiveDirectoryDomainCache { get; }

        public IActiveDirectoryGroupsCache ActiveDirectoryGroupsCache { get; }

        public IActiveDirectoryGroupInfoCache ActiveDirectoryGroupPrincipalCache { get; }

        public IPrincipalContextCache PrincipalContextsCache { get; }
    }
}