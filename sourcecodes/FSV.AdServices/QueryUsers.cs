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
    using System.Collections.Concurrent;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;

    public class QueryUsers
    {
        private readonly ConcurrentBag<int> _summary = new();
        private readonly ICurrentDomainCheckUtility domainCheckUtility;

        public QueryUsers(ICurrentDomainCheckUtility domainCheckUtility)
        {
            this.domainCheckUtility = domainCheckUtility ?? throw new ArgumentNullException(nameof(domainCheckUtility));
        }

        public int CountUsersForest()
        {
            var users = 0;
            ParallelLoopResult res = Parallel.ForEach(this.domainCheckUtility.GetAllDomainsOfForest(), curr => this._summary.Add(this.CountUsersDomain(curr)));

            if (res.IsCompleted)
            {
                users = this._summary.Sum();
            }

            return users;
        }

        private int CountUsersDomain(string domainName)
        {
            using var query = new UserPrincipal(new PrincipalContext(ContextType.Domain, domainName));
            using var search = new PrincipalSearcher(query);
            using PrincipalSearchResult<Principal> principalSearchResult = search.FindAll();
            return principalSearchResult.AsParallel().Count();
        }
    }
}