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

namespace FSV.FileSystem.Interop
{
    using System;
    using System.Security.AccessControl;
    using Abstractions;

    public class Acl : IEquatable<Acl>, IAcl
    {
        public Acl(string accountName, AccessControlType type, FileSystemRights rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            }

            this.AccountName = accountName;
            this.Type = type;
            this.Rights = rights;
            this.InheritanceFlags = inheritanceFlags;
            this.PropagationFlags = propagationFlags;
        }

        public string AccountName { get; }

        public AccessControlType Type { get; }

        public FileSystemRights Rights { get; }

        public bool Inherited { get; set; }

        public InheritanceFlags InheritanceFlags { get; }

        public PropagationFlags PropagationFlags { get; }
        public AccountType AccountType { get; internal set; }


        public bool Equals(Acl other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.AccountName == other.AccountName && this.Type == other.Type && this.Rights == other.Rights && this.InheritanceFlags == other.InheritanceFlags && this.PropagationFlags == other.PropagationFlags;
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

            return this.Equals((Acl)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.AccountName != null ? this.AccountName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)this.Type;
                hashCode = (hashCode * 397) ^ (int)this.Rights;
                hashCode = (hashCode * 397) ^ (int)this.InheritanceFlags;
                hashCode = (hashCode * 397) ^ (int)this.PropagationFlags;
                return hashCode;
            }
        }
    }
}