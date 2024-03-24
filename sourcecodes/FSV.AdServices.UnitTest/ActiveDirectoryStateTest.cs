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
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ActiveDirectoryStateTest
    {
        [TestMethod]
        public void ActiveDirectoryDomainsCache_AddDomain_get_domain_infos_smoke_Test()
        {
            // Arrange
            var sut = new ActiveDirectoryDomainsCache();

            const string friendlyName = "friendlyName";
            string expectedFriendlyDomainName = friendlyName.ToUpperInvariant();
            const string domainName = "domainName";

            var cachedDomainInfo = new CachedDomainInfo(friendlyName, domainName);

            // Act
            sut.AddDomain(cachedDomainInfo);

            // Assert
            bool actual = sut.HasDomains();
            Assert.IsTrue(actual);

            string actualFriendlyName = sut.GetDomainFriendlyName(domainName);
            Assert.AreEqual(expectedFriendlyDomainName, actualFriendlyName);

            bool found = sut.TryGetDomain(friendlyName, out string actualDomainName);
            Assert.IsTrue(found);
            Assert.AreEqual(domainName, actualDomainName);
        }
    }
}