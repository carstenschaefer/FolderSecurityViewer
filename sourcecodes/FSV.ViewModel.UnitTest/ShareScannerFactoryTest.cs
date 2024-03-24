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

namespace FSV.ViewModel.UnitTest
{
    using System.Collections.Generic;
    using System.Security;
    using System.Xml.Linq;
    using Abstractions;
    using AdServices.Abstractions;
    using Configuration.Sections.ShareConfigXml;
    using Crypto;
    using Crypto.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Services.Setting;
    using Services.Shares;

    [TestClass]
    public class ShareScannerFactoryTest
    {
        private const string UserName = "TestUserOne";
        private const string Domain = "TEST";

        [TestMethod]
        public void ShareScannerFactory_CreateServerScanner_test()
        {
            ISecure secure = new Secure();

            Mock<IAdAuthentication> adAuthenticationMock = this.GetAdAuthenticationMock();
            Mock<ISettingShareService> settingShareServiceMock = this.GetSettingShareServiceMock(secure);

            var sut = new ShareScannerFactory(secure, adAuthenticationMock.Object, settingShareServiceMock.Object);
            IShareServerScanner scanner = sut.CreateServerScanner();

            Assert.IsNotNull(scanner);
        }

        [TestMethod]
        public void ShareScannerFactory_CreateServerScanner_NoCredentials_test()
        {
            ISecure secure = new Secure();

            Mock<IAdAuthentication> adAuthenticationMock = this.GetAdAuthenticationMock();
            Mock<ISettingShareService> settingShareServiceMock = this.GetSettingShareServiceNoCredentialsMock();

            var sut = new ShareScannerFactory(secure, adAuthenticationMock.Object, settingShareServiceMock.Object);
            IShareServerScanner scanner = sut.CreateServerScanner();

            Assert.IsNotNull(scanner);
        }

        [TestMethod]
        public void ShareScannerFactory_CreateShareScanner_test()
        {
            ISecure secure = new Secure();

            Mock<IAdAuthentication> adAuthenticationMock = this.GetAdAuthenticationMock();
            Mock<ISettingShareService> settingShareServiceMock = this.GetSettingShareServiceMock(secure);

            var sut = new ShareScannerFactory(secure, adAuthenticationMock.Object, settingShareServiceMock.Object);
            IShareScanner scanner = sut.CreateShareScanner();

            Assert.IsNotNull(scanner);
        }

        [TestMethod]
        public void ShareScannerFactory_CreateShareScanner_NoCredentials_test()
        {
            ISecure secure = new Secure();

            Mock<IAdAuthentication> adAuthenticationMock = this.GetAdAuthenticationMock();
            Mock<ISettingShareService> settingShareServiceMock = this.GetSettingShareServiceNoCredentialsMock();

            var sut = new ShareScannerFactory(secure, adAuthenticationMock.Object, settingShareServiceMock.Object);
            IShareScanner scanner = sut.CreateShareScanner();

            Assert.IsNotNull(scanner);
        }

        private Mock<ISettingShareService> GetSettingShareServiceMock(ISecure secure)
        {
            const string password = "Abc#123";

            using SecureString secureString = this.GetSecureString(password);
            string encryptedPassword = secure.EncryptFromSecureString(secureString);

            var xCredentials = new XElement("Credentials");
            var xUserName = new XElement("UserName", UserName);
            var xPassword = new XElement("Password", encryptedPassword);

            xCredentials.Add(xUserName);
            xCredentials.Add(xPassword);

            var shareCredentials = new ShareCredentials(xCredentials, secure);

            var settingShareServiceMock = new Mock<ISettingShareService>();

            settingShareServiceMock.Setup(m => m.GetCredentials()).Returns(() => shareCredentials);

            return settingShareServiceMock;
        }

        private Mock<ISettingShareService> GetSettingShareServiceNoCredentialsMock()
        {
            var settingShareServiceMock = new Mock<ISettingShareService>();

            settingShareServiceMock.Setup(m => m.GetCredentials()).Returns(() => null);

            return settingShareServiceMock;
        }

        private Mock<IAdAuthentication> GetAdAuthenticationMock()
        {
            var adAuthenticationMock = new Mock<IAdAuthentication>();
            adAuthenticationMock.Setup(m => m.GetUserPrincipalName(UserName))
                .Returns(() => new KeyValuePair<string, string>(UserName, Domain));
            return adAuthenticationMock;
        }

        private SecureString GetSecureString(string password)
        {
            var secureString = new SecureString();
            foreach (char item in password)
            {
                secureString.AppendChar(item);
            }

            secureString.MakeReadOnly();
            return secureString;
        }
    }
}