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
    public sealed class PrincipalContextInfo
    {
        public PrincipalContextInfo(ContextType contextType)
        {
            this.ContextType = contextType;
        }

        public PrincipalContextInfo(ContextType contextType, [CanBeNull] string name) : this(contextType)
        {
            this.Name = name;
        }

        public ContextType ContextType { get; }

        public string Name { get; }

        public static PrincipalContextInfo CreateFrom(PrincipalContext principalContext)
        {
            if (principalContext == null)
            {
                throw new ArgumentNullException(nameof(principalContext));
            }

            ContextType principalContextContextType = principalContext.ContextType;
            string principalContextName = principalContext.Name;

            return new PrincipalContextInfo(principalContextContextType, principalContextName);
        }

        internal PrincipalContext CreateContext()
        {
            return new PrincipalContext(this.ContextType, this.Name);
        }
    }
}