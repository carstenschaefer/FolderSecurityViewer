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

    public class ActiveDirectoryGroupsCache : IActiveDirectoryGroupsCache
    {
        private readonly Dictionary<string, List<string>> groups = new();
        private readonly object syncObject = new();

        public bool TryGetGroup(string groupName, out IEnumerable<string> accountNames)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            accountNames = null;
            lock (this.syncObject)
            {
                if (this.groups.TryGetValue(groupName, out List<string> list))
                {
                    accountNames = list.AsReadOnly();
                    return true;
                }
            }

            return false;
        }

        public void AddAccountNames(string groupName, IEnumerable<string> accountNames)
        {
            if (accountNames == null)
            {
                throw new ArgumentNullException(nameof(accountNames));
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            lock (this.syncObject)
            {
                if (this.groups.TryGetValue(groupName, out List<string> list))
                {
                    List<string> union = list.Union(accountNames).Distinct().ToList();
                    this.groups[groupName] = union;
                    return;
                }

                this.groups[groupName] = new List<string>(accountNames);
            }
        }

        public bool HasGroupWithAccountName(string groupName, string accountName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            }

            return this.TryGetGroup(groupName, out IEnumerable<string> accounts)
                   && accounts.Any(x => string.Compare(x, accountName, StringComparison.InvariantCulture) == 0);
        }

        public bool HasGroupWithAccountName(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            }

            return this.TryGetGroupsWithAccountName(accountName, out IEnumerable<string> _);
        }

        public bool TryGetGroupsWithAccountName(string accountName, out IEnumerable<string> groupNames)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            }

            bool IsSameAccountName(string x)
            {
                return string.Compare(x, accountName, StringComparison.InvariantCulture) == 0;
            }

            groupNames = this.groups
                .Where(pair => pair.Value.Any(IsSameAccountName))
                .Select(pair => pair.Key)
                .ToList();

            return groupNames.Any();
        }

        public bool HasGroup(string groupName)
        {
            return this.TryGetGroup(groupName, out IEnumerable<string> _);
        }
    }
}