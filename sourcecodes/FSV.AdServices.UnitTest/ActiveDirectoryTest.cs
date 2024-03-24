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
    using Abstractions;
    using ActiveDirectoryServices.TestAbstractionLayer;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ActiveDirectoryTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ActiveDirectory_TryInitiateDomainEnumeration_Test()
        {
            // Arrange
            IActiveDirectoryAbstractionService activeDirectoryAbstractionService = ActiveDirectoryYamlAbstractionService.Create("Data/forest.yaml");

            var domainsCacheMock = new Mock<IActiveDirectoryDomainsCache>();
            domainsCacheMock.Setup(cache => cache.AddDomain(It.IsAny<CachedDomainInfo>())).Verifiable();

            var activeDirectoryUtilityMock = new Mock<IActiveDirectoryUtility>();
            activeDirectoryUtilityMock
                .Setup(utility => utility.GetNetBiosNameofDomain(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((dnsForestDomainName, dnsDomainName) => $"{dnsForestDomainName}.{dnsDomainName}.com")
                .Verifiable();

            var sut = new ActiveDirectory(
                new Mock<IConfigurationManager>().Object,
                new ActiveDirectoryState(
                    domainsCacheMock.Object,
                    new Mock<IActiveDirectoryGroupsCache>().Object,
                    new Mock<IActiveDirectoryGroupInfoCache>().Object,
                    new Mock<IPrincipalContextCache>().Object),
                activeDirectoryUtilityMock.Object,
                activeDirectoryAbstractionService,
                new PrincipalContextFactory(new NullLogger<PrincipalContextFactory>()),
                new NullLoggerFactory(),
                new NullLogger<ActiveDirectory>());

            // Act
            bool actual = sut.TryInitiateDomainEnumeration();

            // Assert
            Assert.IsTrue(actual);

            activeDirectoryUtilityMock.Verify(utility => utility.GetNetBiosNameofDomain(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
            domainsCacheMock.Verify(cache => cache.AddDomain(It.IsAny<CachedDomainInfo>()), Times.AtLeastOnce);
        }
    }
}