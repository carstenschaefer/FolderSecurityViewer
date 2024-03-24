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
    using System.Security.AccessControl;
    using Abstractions;

    public sealed class AclModelBuilder : IAclModelBuilder
    {
        public IAclModel Build(IAcl acl)
        {
            if (acl == null)
            {
                throw new ArgumentNullException(nameof(acl));
            }

            var info = new AclModel(acl.AccountName, acl.Type, acl.InheritanceFlags, acl.Rights, acl.AccountType, acl.PropagationFlags)
            {
                TypeString = acl.Type.ToString(),
                InheritanceFlagsString = acl.InheritanceFlags.ToString(),
                Inherited = acl.Inherited,
                RightsString = acl.Rights.ToString(),
                AccountTypeString = acl.AccountType.ToString()
            };

            if (info.RightsString == "268435456")
            {
                info.RightsString = "Special: Creater Owner";
            }

            if (((int)acl.InheritanceFlags == 3) & (acl.PropagationFlags == PropagationFlags.InheritOnly))
            {
                info.InheritanceFlagsString = "Subfolders and Files only";
            }
            else if (((int)acl.InheritanceFlags == 3) & (acl.PropagationFlags == PropagationFlags.None))
            {
                info.InheritanceFlagsString = "This Folder, Subfolders and Files";
            }
            else if (((int)acl.InheritanceFlags == 3) & (acl.PropagationFlags == PropagationFlags.NoPropagateInherit))
            {
                info.InheritanceFlagsString = "This Folder, Subfolders and Files";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.ContainerInherit)
                     & (acl.PropagationFlags == PropagationFlags.None))
            {
                info.InheritanceFlagsString = "This Folder and Subfolders";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.ContainerInherit)
                     & (acl.PropagationFlags == PropagationFlags.InheritOnly))
            {
                info.InheritanceFlagsString = "Subfolders only";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.ObjectInherit)
                     & (acl.PropagationFlags == PropagationFlags.None))
            {
                info.InheritanceFlagsString = "This Folder and Files";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.ObjectInherit)
                     & (acl.PropagationFlags == PropagationFlags.NoPropagateInherit))
            {
                info.InheritanceFlagsString = "This Folder and Files";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.None)
                     & (acl.PropagationFlags == PropagationFlags.None))
            {
                info.InheritanceFlagsString = "This Folder only";
            }
            else if ((acl.InheritanceFlags == InheritanceFlags.ObjectInherit)
                     & (acl.PropagationFlags == PropagationFlags.InheritOnly))
            {
                info.InheritanceFlagsString = "Files only";
            }

            return info;
        }
    }
}