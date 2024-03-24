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
    using System;
    using Abstractions;
    using Cache;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ActiveDirectoryUtilityTest
    {
        [TestMethod]
        public void ActiveDirectoryUtility_GetContext_returns_machine_context_if_domain_is_null_Test()
        {
            // Arrange
            ActiveDirectoryUtility sut = CreateActiveDirectoryUtility();
            string domain = null;

            // Act
            PrincipalContextInfo actual = sut.GetContext(domain);

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void ActiveDirectoryUtility_GetMachineContext_Test()
        {
            // Arrange
            string machineName = Environment.MachineName;

            var principalContextFactoryMock = new Mock<IPrincipalContextFactory>();

            var sut = new ActiveDirectoryUtility(
                principalContextFactoryMock.Object,
                new Mock<IActiveDirectoryState>().Object,
                new NullLogger<ActiveDirectoryUtility>());

            // Act
            PrincipalContextInfo actual = sut.GetMachineContext(machineName);

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void ActiveDirectoryUtility_IsSid_returns_true_Test()
        {
            // Arrange
            const string sidSddlForm = "S-1-5-21-397955417-626881126-188441444-512";

            // Act
            bool actual = ActiveDirectoryUtility.IsSId(sidSddlForm);

            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ActiveDirectoryUtility_IsSid_returns_false_on_invalid_sid_Test()
        {
            // Arrange
            const string sid = "S-1-5-21";

            // Act
            bool actual = ActiveDirectoryUtility.IsSId(sid);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ActiveDirectoryLdapUtility_GetDomainNameFromDistinguishedName_Test()
        {
            // Arrange
            const string distinguishedName = "CN=Floris	Deex,OU=End Users,OU=organizational-unit,DC=domain-component,DC=local";

            // Act
            string actual = ActiveDirectoryLdapUtility.GetDomainNameFromDistinguishedName(distinguishedName);

            // Assert
            Assert.IsNotNull(distinguishedName);
        }

        private static ActiveDirectoryUtility CreateActiveDirectoryUtility()
        {
            var stateMock = new Mock<IActiveDirectoryState>();
            var principalFactoryMock = new Mock<IPrincipalContextFactory>();

            return new ActiveDirectoryUtility(principalFactoryMock.Object, stateMock.Object, new NullLogger<ActiveDirectoryUtility>());
        }
    }
}