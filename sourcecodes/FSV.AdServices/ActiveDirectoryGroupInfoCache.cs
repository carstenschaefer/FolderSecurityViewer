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
    using System.Linq;
    using Abstractions;
    using Cache;

    public class ActiveDirectoryGroupInfoCache : IActiveDirectoryGroupInfoCache
    {
        private readonly Dictionary<string, GroupPrincipalInfo> groups = new();
        private readonly object syncObject = new();

        public bool TryGetGroup(string groupName, out GroupPrincipalInfo groupPrincipalInfo)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            groupPrincipalInfo = null;
            lock (this.syncObject)
            {
                if (!this.groups.TryGetValue(groupName, out GroupPrincipalInfo reference))
                {
                    return false;
                }

                groupPrincipalInfo = reference;
                return true;
            }
        }

        public void AddGroup(string groupName, GroupPrincipalInfo groupPrincipalInfo)
        {
            if (groupPrincipalInfo == null)
            {
                throw new ArgumentNullException(nameof(groupPrincipalInfo));
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            lock (this.syncObject)
            {
                this.groups[groupName] = groupPrincipalInfo;
            }
        }

        public bool HasGroupWithMember(string groupName, string memberName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(memberName));
            }

            return this.TryGetGroup(groupName, out GroupPrincipalInfo adGroup)
                   && adGroup.Members.Any(x => string.Compare(x.Name, memberName, StringComparison.InvariantCulture) == 0);
        }

        public void Clear()
        {
            lock (this.syncObject)
            {
                this.groups.Clear();
            }
        }
    }
}