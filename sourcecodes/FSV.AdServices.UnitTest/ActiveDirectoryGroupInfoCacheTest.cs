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

namespace FSV.AdServices.UnitTest
{
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using Cache;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ActiveDirectoryGroupInfoCacheTest
    {
        [TestMethod]
        public void ActiveDirectoryGroupInfoCache_Clear_does_not_throw_on_empty_cache_Tes()
        {
            // Arrange
            var sut = new ActiveDirectoryGroupInfoCache();

            var principalContextInfo = new PrincipalContextInfo(ContextType.Domain, "name");

            const string groupName = "group-name";
            var groupPrincipalInfo = new GroupPrincipalInfo(principalContextInfo, groupName, "sid", "distinguished-name", true, Enumerable.Empty<PrincipalInfo>());
            sut.AddGroup(groupName, groupPrincipalInfo);

            bool groupExists = sut.TryGetGroup(groupName, out _);
            
            // Act
            sut.Clear();
            bool groupMissingAfterClear = sut.TryGetGroup(groupName, out _) == false;

            // Assert
            Assert.IsTrue(groupExists);
            Assert.IsTrue(groupMissingAfterClear);
        }

        [TestMethod]
        public void ActiveDirectoryGroupInfoCache_AddContext_TryGetContext_returns_expected_object_Tes()
        {
            // Arrange
            var sut = new ActiveDirectoryGroupInfoCache();
            var principalContextInfo = new PrincipalContextInfo(ContextType.Domain, "name");

            const string groupName = "group-name";
            var groupPrincipalInfo = new GroupPrincipalInfo(principalContextInfo, groupName, "sid", "distinguished-name", true, Enumerable.Empty<PrincipalInfo>());

            // Act
            sut.AddGroup(groupName, groupPrincipalInfo);
            bool actual = sut.TryGetGroup(groupName, out GroupPrincipalInfo cachedGroupPrincipalInfo);

            // Assert
            Assert.IsTrue(actual);
            Assert.IsNotNull(cachedGroupPrincipalInfo);
            Assert.AreEqual(groupPrincipalInfo, cachedGroupPrincipalInfo);
        }

        [TestMethod]
        public void ActiveDirectoryGroupInfoCache_HasGroupWithMember_returns_false_if_member_not_exists_Tes()
        {
            // Arrange
            var sut = new ActiveDirectoryGroupInfoCache();
            var principalContextInfo = new PrincipalContextInfo(ContextType.Domain, "name");
            const string groupName = "group-name";
            var groupPrincipalInfo = new GroupPrincipalInfo(principalContextInfo, groupName, "sid", "distinguished-name", true, Enumerable.Empty<PrincipalInfo>());
            sut.AddGroup(groupName, groupPrincipalInfo);

            // Act
            bool actual = sut.HasGroupWithMember(groupName, "member-name");

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ActiveDirectoryGroupInfoCache_HasGroupWithMember_returns_true_if_member_exists_Tes()
        {
            // Arrange
            var sut = new ActiveDirectoryGroupInfoCache();

            var principalContextInfo = new PrincipalContextInfo(ContextType.Domain, "name");

            const string groupName = "group-name";
            const string memberName = "member-name";

            IEnumerable<PrincipalInfo> members = new List<PrincipalInfo> { new UserPrincipalInfo(principalContextInfo, memberName, "member-sid", "member-distinguished-name") };
            var groupPrincipalInfo = new GroupPrincipalInfo(principalContextInfo, groupName, "sid", "distinguished-name", true, members);
            sut.AddGroup(groupName, groupPrincipalInfo);

            // Act
            bool actual = sut.HasGroupWithMember(groupName, memberName);

            // Assert
            Assert.IsTrue(actual);
        }
    }
}