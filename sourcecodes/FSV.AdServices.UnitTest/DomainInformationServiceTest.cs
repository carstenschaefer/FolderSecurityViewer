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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DomainInformationServiceTest
    {
        [TestMethod]
        public void DomainInformationService_GetRootDomainNetBiosName_returns_null_if_computer_does_not_belong_to_domain_Test()
        {
            // Arrange
            var searcherMock = new Mock<ISearcher>();
            var domainCheckUtilityMock = new Mock<ICurrentDomainCheckUtility>();
            domainCheckUtilityMock.Setup(utility => utility.IsComputerJoinedAndConnectedToDomain()).Returns(false).Verifiable();
            var sut = new DomainInformationService(domainCheckUtilityMock.Object, () => searcherMock.Object);

            // Act
            string actual = sut.GetRootDomainNetBiosName();

            // Assert
            Assert.IsNull(actual);
            domainCheckUtilityMock.Verify(utility => utility.IsComputerJoinedAndConnectedToDomain(), Times.Once);
        }

        [TestMethod]
        public void DomainInformationService_GetRootDomainNetBiosName_returns_name_if_computer_does_not_belong_to_domain_Test()
        {
            // Arrange
            const string domain = "domain";

            var domainCheckUtilityMock = new Mock<ICurrentDomainCheckUtility>();
            domainCheckUtilityMock.Setup(utility => utility.IsComputerJoinedAndConnectedToDomain()).Returns(true).Verifiable();
            domainCheckUtilityMock.Setup(utility => utility.GetComputerDomainName()).Returns(domain).Verifiable();

            var searcherMock = new Mock<ISearcher>();
            const string expectedNetBiosDomainName = "netBios-" + domain;
            searcherMock.Setup(searcher => searcher.GetNetBiosDomainName(domain)).Returns(expectedNetBiosDomainName).Verifiable();

            var sut = new DomainInformationService(domainCheckUtilityMock.Object, () => searcherMock.Object);

            // Act
            string actual = sut.GetRootDomainNetBiosName();

            // Assert
            Assert.IsNotNull(actual);
            Assert.AreEqual(expectedNetBiosDomainName, actual);

            domainCheckUtilityMock.Verify(utility => utility.IsComputerJoinedAndConnectedToDomain(), Times.Once);
            domainCheckUtilityMock.Verify(utility => utility.GetComputerDomainName(), Times.Once);
            searcherMock.Verify(searcher => searcher.GetNetBiosDomainName(domain), Times.Once);
        }
    }
}