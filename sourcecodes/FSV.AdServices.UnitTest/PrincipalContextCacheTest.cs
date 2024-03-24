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
    using System.DirectoryServices.AccountManagement;
    using Cache;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PrincipalContextCacheTest
    {
        [TestMethod]
        public void PrincipalContextCache_Clear_does_not_throw_on_empty_cache_Test()
        {
            // Arrange
            var sut = new PrincipalContextCache();

            // Act
            sut.Clear();

            // Assert
        }

        [TestMethod]
        public void PrincipalContextCache_AddContext_TryGetContext_returns_expected_object_Test()
        {
            // Arrange
            var sut = new PrincipalContextCache();

            const string contextKey = "key";
            var principalContextInfo = new PrincipalContextInfo(ContextType.Domain, "name");

            // Act
            sut.AddContext(contextKey, principalContextInfo);
            bool actual = sut.TryGetContext(contextKey, out PrincipalContextInfo cachedPrincipalContextInfo);

            // Assert
            Assert.IsTrue(actual);
            Assert.IsNotNull(cachedPrincipalContextInfo);
            Assert.AreEqual(principalContextInfo, cachedPrincipalContextInfo);
        }
    }
}