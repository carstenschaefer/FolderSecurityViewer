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
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;

    public static class AclModelExtensions
    {
        /// <summary>
        ///     Compares and marks different items when they are not same as parent ACLs.
        /// </summary>
        /// <param name="parentAclModels">A collection of acl list of parent directory.</param>
        /// <param name="childAclModels">
        ///     A collection of acl list of child directory. DifferentFromParent property is set to true
        ///     when ACL of child directory is different from parent.
        /// </param>
        /// <returns>A boolean value to indicate that differences were found.</returns>
        public static bool IsAclEqual(this IEnumerable<IAclModel> parentAclModels, IEnumerable<IAclModel> childAclModels)
        {
            if (parentAclModels == null)
            {
                throw new ArgumentNullException(nameof(parentAclModels));
            }

            if (childAclModels == null)
            {
                throw new ArgumentNullException(nameof(childAclModels));
            }

            static bool AreSameAccountsWithSameRightsAndInheritanceFlags(IAclModel model, IAclModel other)
            {
                return model.Account == other.Account
                       && model.Rights == other.Rights
                       && model.InheritanceFlags == other.InheritanceFlags
                       && model.PropagationFlags == other.PropagationFlags;
            }

            IEnumerable<IAclModel> parentList = parentAclModels.ToList();
            IEnumerable<IAclModel> childrenList = childAclModels.ToList();

            foreach (IAclModel item in childrenList)
            {
                if (parentList.Any(model => AreSameAccountsWithSameRightsAndInheritanceFlags(model, item)))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}