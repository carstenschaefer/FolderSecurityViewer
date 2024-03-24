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
    using JetBrains.Annotations;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class PrincipalInfo : IEquatable<PrincipalInfo>
    {
        protected PrincipalInfo(PrincipalContextInfo principalContextInfo, string name, string sid, [CanBeNull] string distinguishedName, PrincipalType memberType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sid));
            }

            this.ContextInfo = principalContextInfo ?? throw new ArgumentNullException(nameof(principalContextInfo));
            this.Name = name;
            this.Sid = sid;
            this.DistinguishedName = distinguishedName;
            this.MemberType = memberType;
        }

        public PrincipalContextInfo ContextInfo { get; }
        public ContextType ContextType => this.ContextInfo.ContextType;

        /// <summary>
        ///     Gets the name of the principal.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets or formatted security identifier (SID) of the principal.
        /// </summary>
        public string Sid { get; }

        /// <summary>
        ///     Gets the distinguished name (DN) of the principal.
        /// </summary>
        public string DistinguishedName { get; }

        /// <summary>
        ///     Gets a value indicating the structural object class directory attribute.
        /// </summary>
        public PrincipalType MemberType { get; }

        public bool Equals(PrincipalInfo other)
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

        public static PrincipalInfo CreateFrom(Principal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            string name = principal.Name;
            string sid = principal.Sid.Value;
            string distinguishedName = principal.DistinguishedName;
            PrincipalType memberType = GetMemberType(principal);

            // TODO: check what we need from the underlying object and add those properties to the new instance
            // var DirectoryEntry = MemberType == PrincipalType.User ? (DirectoryEntry) principal.GetUnderlyingObject() : null;

            var contextInfo = PrincipalContextInfo.CreateFrom(principal.Context);
            return new PrincipalInfo(contextInfo, name, sid, distinguishedName, memberType);
        }

        public DirectoryEntryWrapper ResolveDirectoryEntry()
        {
            switch (this.MemberType)
            {
                case PrincipalType.Group:
                {
                    PrincipalContext principalContext = this.ContextInfo.CreateContext();
                    GroupPrincipal principal = GroupPrincipal.FindByIdentity(principalContext, IdentityType.Sid, this.Sid);
                    return new DirectoryEntryWrapper(principal);
                }
                case PrincipalType.User:
                {
                    PrincipalContext principalContext = this.ContextInfo.CreateContext();
                    UserPrincipal principal = UserPrincipal.FindByIdentity(principalContext, IdentityType.Sid, this.Sid);
                    return new DirectoryEntryWrapper(principal);
                }
            }

            return null;
        }

        private static PrincipalType GetMemberType(Principal principal)
        {
            return principal.StructuralObjectClass switch
            {
                "group" => PrincipalType.Group,
                "user" => PrincipalType.User,
                _ => PrincipalType.Other
            };
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is PrincipalInfo other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            return this.Sid != null ? this.Sid.GetHashCode() : 0;
        }
    }
}