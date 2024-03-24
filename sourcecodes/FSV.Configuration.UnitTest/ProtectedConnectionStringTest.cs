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
    using System;
    using System.Data.SqlClient;
    using Database;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProtectedConnectionStringTest
    {
        [TestMethod]
        public void ProtectedConnectionString_Create_encrypts_given_connection_string_Test()
        {
            // Arrange
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "datasource",
                InitialCatalog = "db",
                UserID = "user",
                Password = "password"
            };

            var connectionString = connectionStringBuilder.ToString();

            // Act
            var actual = ProtectedConnectionString.Create(connectionString);

            // Assert
            Assert.IsNotNull(actual);

            var base64EncodedProtectedConnectionString = actual.ToString();
            byte[] decoded = Convert.FromBase64String(base64EncodedProtectedConnectionString);
            Assert.IsNotNull(decoded);
        }

        [TestMethod]
        public void ProtectedConnectionString_ctor_decodes_base64_string_Test()
        {
            // Arrange
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "datasource",
                InitialCatalog = "db",
                UserID = "user",
                Password = "password"
            };

            var connectionString = connectionStringBuilder.ToString();
            var protectedConnectionString = ProtectedConnectionString.Create(connectionString);
            var encryptedConnectionString = protectedConnectionString.ToString();

            // Act
            var actual = new ProtectedConnectionString(encryptedConnectionString);

            // Assert
            Assert.IsNotNull(actual);

            string unprotected = actual.GetDecryptedConnectionString();
            Assert.AreEqual(connectionString, unprotected);
        }
    }
}