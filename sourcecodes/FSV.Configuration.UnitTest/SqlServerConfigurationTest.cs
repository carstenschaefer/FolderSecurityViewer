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

namespace FSV.Configuration.UnitTest
{
    using System.Data.SqlClient;
    using System.Security;
    using Crypto.Abstractions;
    using Database;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class SqlServerConfigurationTest
    {
        private static SecureString MakeSecureString(string s)
        {
            var result = new SecureString();
            foreach (char c in s)
            {
                result.AppendChar(c);
            }

            result.MakeReadOnly();
            return result;
        }

        private static string MakeConnectionString(string dataSource, string catalog, string user, string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = catalog,
                UserID = user,
                Password = password,
                IntegratedSecurity = false
            };

            return builder.ToString();
        }

        [TestMethod]
        public void SqlServerConfiguration_parses_protected_connection_string_and_allows_configuration_Test()
        {
            // Arrange
            string connectionString = MakeConnectionString("datasource", "db", "user", "password");
            var protectedConnectionString = ProtectedConnectionString.Create(connectionString);

            const string expectedPassword = "encrypted";
            string expectedConnectionString = MakeConnectionString("changed-datasource", "changed-db", "changed-user", expectedPassword);

            var secureMock = new Mock<ISecure>();
            const bool secureEncryptionUsesEntropy = false;
            secureMock.Setup(secure => secure.EncryptFromSecureString(It.IsAny<SecureString>(), secureEncryptionUsesEntropy)).Returns(expectedPassword).Verifiable();

            var sut = new SqlServerConfiguration(protectedConnectionString, secureMock.Object)
            {
                DataSource = "changed-datasource",
                Database = "changed-db",
                UserID = "changed-user",
                Password = MakeSecureString("changed-password")
            };

            // Act
            ProtectedConnectionString actual = sut.GetProtectedConnectionString();

            // Assert
            Assert.IsNotNull(actual);
            secureMock.Verify(secure => secure.EncryptFromSecureString(It.IsAny<SecureString>(), secureEncryptionUsesEntropy), Times.Once);

            string unprotected = actual.GetDecryptedConnectionString();
            Assert.AreEqual(expectedConnectionString, unprotected);
        }
    }
}