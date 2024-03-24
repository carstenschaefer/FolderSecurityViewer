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

namespace FSV.AdServices.EnumOU
{
    using System.Collections.Generic;
    using System.DirectoryServices;

    public static class EnumOu
    {
        public enum PrincialType
        {
            User,
            Group,
            Ou,
            Computer
        }

        public enum SearchLevel
        {
            OneLevel,
            Subtree
        }

        public static IEnumerable<AdTreeViewModel> QueryOU(string baseOU, PrincialType type, SearchLevel level)
        {
            var ous = new List<AdTreeViewModel>();

            string filter = null;
            var treeViewType = TreeViewNodeType.OU;

            switch (type)
            {
                case PrincialType.User:
                    filter = "(&(objectCategory=person)(objectClass=user))";
                    treeViewType = TreeViewNodeType.User;
                    break;
                case PrincialType.Group:
                    filter = "(&(objectClass=group))";
                    treeViewType = TreeViewNodeType.Group;
                    break;
                case PrincialType.Ou:
                    filter = "(|(objectClass=organizationalUnit)(objectClass=container)(objectClass=builtinDomain))";
                    treeViewType = TreeViewNodeType.OU;
                    break;
                case PrincialType.Computer:
                    filter = "(objectClass=computer)";
                    treeViewType = TreeViewNodeType.Computer;
                    break;
            }

            using var root = new DirectoryEntry("LDAP://" + baseOU);
            using var searcher = new DirectorySearcher(root)
            {
                Filter = filter,
                SearchScope = level == SearchLevel.OneLevel ? SearchScope.OneLevel : SearchScope.Subtree,
                PageSize = 10000,
                SizeLimit = 0
            };

            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("sAmAccountName");
            searcher.PropertiesToLoad.Add("distinguishedName");

            using SearchResultCollection result = searcher.FindAll();
            foreach (SearchResult searchResult in result)
            {
                using DirectoryEntry directoryEntry = searchResult.GetDirectoryEntry();
                var displayName = directoryEntry.Properties["name"].Value.ToString();
                var distName = directoryEntry.Properties["distinguishedName"].Value.ToString();

                PropertyValueCollection sAmAccountNameProperty = directoryEntry.Properties["sAmAccountName"];
                string samAccountName = sAmAccountNameProperty.Value?.ToString() ?? string.Empty;

                ous.Add(new AdTreeViewModel(displayName, distName, samAccountName, treeViewType));
            }

            return ous;
        }
    }
}