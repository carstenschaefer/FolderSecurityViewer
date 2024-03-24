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

namespace FSV.FileSystem.Interop.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.AccessControl;
    using Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Types;

    [TestClass]
    public class AclModelExtensionsTest
    {
        [TestMethod]
        public void AclModelExtensions_IsAclEqual_lists_have_are_of_the_same_length_returns_true_Test()
        {
            // Arrange
            var model = new AclModel("account", AccessControlType.Allow, InheritanceFlags.None, FileSystemRights.ListDirectory, AccountType.User, PropagationFlags.None);
            IEnumerable<IAclModel> parentAclModels = new[] { model };
            IEnumerable<IAclModel> childAclModels = new[] { model };

            // Act
            bool actual = parentAclModels.IsAclEqual(childAclModels);

            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void AclModelExtensions_IsAclEqual_lists_differ_in_length_returns_true_Test()
        {
            // This test should become green, but it does not. Investigate.

            // Arrange
            var model = new AclModel("account", AccessControlType.Allow, InheritanceFlags.None, FileSystemRights.ListDirectory, AccountType.User, PropagationFlags.None);
            IEnumerable<AclModel> parentAclModels = new[] { model };
            IEnumerable<AclModel> childAclModels = new[]
            {
                model,
                model
            };

            // Act
            bool actual = parentAclModels.IsAclEqual(childAclModels);

            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void AclModelExtensions_IsAclEqual_performance_Test()
        {
            // Arrange
            AclModel[] parentAclModels = Enumerable.Range(1, 1000).Select(i => new AclModel($"account-{i}", AccessControlType.Allow, InheritanceFlags.None, FileSystemRights.Read, AccountType.User, PropagationFlags.None)).ToArray();
            AclModel[] childAclModels = Enumerable.Range(501, 1500).Select(i => new AclModel($"account-{i}", AccessControlType.Allow, InheritanceFlags.None, FileSystemRights.Read, AccountType.User, PropagationFlags.None)).ToArray();

            // Act
            bool actual = parentAclModels.IsAclEqual(childAclModels);

            // Assert
            Assert.IsFalse(actual);
        }
    }
}