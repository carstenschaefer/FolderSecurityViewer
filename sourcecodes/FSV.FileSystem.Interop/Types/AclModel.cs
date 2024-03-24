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

namespace FSV.FileSystem.Interop.Types
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.AccessControl;
    using Abstractions;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    internal sealed class AclModel : IAclModel
    {
        public AclModel(string account, AccessControlType type, InheritanceFlags inheritanceFlags, FileSystemRights rights, AccountType accountType, PropagationFlags propagationFlags)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(account));
            }

            this.Account = account;
            this.Type = type;
            this.InheritanceFlags = inheritanceFlags;
            this.Rights = rights;
            this.AccountType = accountType;
            this.PropagationFlags = propagationFlags;
        }

        public string Account { get; }

        public string TypeString { get; set; }

        public string RightsString { get; set; }

        public bool Inherited { get; set; }

        public string InheritanceFlagsString { get; set; }

        public string AccountTypeString { get; set; }
        public AccessControlType Type { get; }
        public InheritanceFlags InheritanceFlags { get; }
        public FileSystemRights Rights { get; }
        public AccountType AccountType { get; }
        public PropagationFlags PropagationFlags { get; }

        public override string ToString()
        {
            return this.Dump();
        }
    }
}