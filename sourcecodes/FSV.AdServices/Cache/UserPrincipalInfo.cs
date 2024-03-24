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
    using System.Diagnostics.CodeAnalysis;
    using System.DirectoryServices.AccountManagement;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class UserPrincipalInfo : PrincipalInfo, IEquatable<UserPrincipalInfo>
    {
        public UserPrincipalInfo(PrincipalContextInfo contextInfo, string name, string sid, string distinguishedName) : base(contextInfo, name, sid, distinguishedName, PrincipalType.User)
        {
        }

        public bool Equals(UserPrincipalInfo other)
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

        public static UserPrincipalInfo CreateFrom(UserPrincipal user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var contextInfo = PrincipalContextInfo.CreateFrom(user.Context);

            string name = user.Name;
            string sid = user.Sid.Value;
            string distinguishedName = user.DistinguishedName;

            return new UserPrincipalInfo(contextInfo, name, sid, distinguishedName);
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

            return this.Equals((UserPrincipalInfo)obj);
        }

        public override int GetHashCode()
        {
            return this.Sid != null ? this.Sid.GetHashCode() : 0;
        }
    }
}