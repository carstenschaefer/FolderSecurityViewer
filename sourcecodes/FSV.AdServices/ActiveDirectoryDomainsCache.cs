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

    public sealed class ActiveDirectoryDomainsCache : IActiveDirectoryDomainsCache
    {
        private readonly Dictionary<string, string> allDomains = new();

        private readonly Dictionary<string, string> allDomainsInverse = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly object syncObject = new();

        public bool HasDomains()
        {
            return this.allDomains.Any();
        }

        public string GetDomainFriendlyName(string domain)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            lock (this.syncObject)
            {
                return this.allDomainsInverse.TryGetValue(domain, out string result) ? result : null;
            }
        }

        public bool TryGetDomain(string friendlyDomainName, out string result)
        {
            if (friendlyDomainName == null)
            {
                throw new ArgumentNullException(nameof(friendlyDomainName));
            }

            lock (this.syncObject)
            {
                string key = friendlyDomainName.ToUpperInvariant();
                return this.allDomains.TryGetValue(key, out result);
            }
        }

        public void AddDomain(CachedDomainInfo domainInfo)
        {
            if (domainInfo == null)
            {
                throw new ArgumentNullException(nameof(domainInfo));
            }

            string upperCaseFriendlyDomainName = domainInfo.FriendlyName.ToUpper();

            lock (this.syncObject)
            {
                this.allDomains.Add(upperCaseFriendlyDomainName, domainInfo.DomainName);
                this.allDomainsInverse.Add(domainInfo.DomainName, upperCaseFriendlyDomainName);
            }
        }
    }
}