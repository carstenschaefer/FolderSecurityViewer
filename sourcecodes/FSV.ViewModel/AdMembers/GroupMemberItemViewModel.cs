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

namespace FSV.ViewModel.AdMembers
{
    using System;
    using System.Linq;
    using Models;
    using Resources;

    public class GroupMemberItemViewModel
    {
        public GroupMemberItemViewModel(AdGroupMember member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            this.AccountName = member.SamAccountName;
            this.AccountType = member.IsGroup ? GroupMemberResource.GroupText : GroupMemberResource.UserText;
            string[] strings = member.Ou.Split('/');
            strings.Reverse();
            this.OU = string.Join("/", strings);
            this.Domain = member.DomainName;
            this.DisplayName = member.DisplayName;
            this.IsGroup = member.IsGroup;
        }

        public string AccountName { get; }
        public string AccountType { get; }
        public string Domain { get; }
        public string DisplayName { get; }
        public bool IsGroup { get; }
        public string OU { get; }
    }
}