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

namespace FSV.AdServices.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;

    /// <summary>
    ///     A class that stores information of a <see cref="GroupPrincipal" /> object.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class GroupPrincipalInfo : PrincipalInfo, IEquatable<GroupPrincipalInfo>
    {
        public GroupPrincipalInfo(PrincipalContextInfo principalContextInfo, string name, string sid, string distinguishedName, bool isSecurityGroup, IEnumerable<PrincipalInfo> members) : base(principalContextInfo, name, sid, distinguishedName, PrincipalType.Group)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sid));
            }

            this.IsSecurityGroup = isSecurityGroup;
            this.Members = members ?? throw new ArgumentNullException(nameof(members));
        }

        public bool IsSecurityGroup { get; }
        public IEnumerable<PrincipalInfo> Members { get; }

        public bool Equals(GroupPrincipalInfo other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Sid == other.Sid;
        }

        public static GroupPrincipalInfo CreateFrom(GroupPrincipal groupPrincipal)
        {
            if (groupPrincipal == null)
            {
                throw new ArgumentNullException(nameof(groupPrincipal));
            }

            string name = groupPrincipal.Name;
            bool isSecurityGroup = groupPrincipal.IsSecurityGroup ?? false;
            string sid = groupPrincipal.Sid.Value;
            string distinguishedName = groupPrincipal.DistinguishedName;

            var context = PrincipalContextInfo.CreateFrom(groupPrincipal.Context);
            IEnumerable<PrincipalInfo> members = GetMembersFromGroupPrincipal(groupPrincipal);
            return new GroupPrincipalInfo(context, name, sid, distinguishedName, isSecurityGroup, members);
        }

        private static IEnumerable<PrincipalInfo> GetMembersFromGroupPrincipal(GroupPrincipal currentPrincipal)
        {
            return currentPrincipal.Members.Select(CreateFrom).ToList();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((GroupPrincipalInfo)obj);
        }

        public override int GetHashCode()
        {
            return this.Sid != null ? this.Sid.GetHashCode() : 0;
        }
    }
}