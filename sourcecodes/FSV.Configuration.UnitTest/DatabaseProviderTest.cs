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
    using Database;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DatabaseProviderTest
    {
        [TestMethod]
        public void DatabaseProvider_TryParse_find_None_returns_false_Test()
        {
            // Arrange

            // Act
            bool actual = DatabaseProvider.TryParse("None", out DatabaseProvider found);

            // Assert
            Assert.IsFalse(actual);
            Assert.IsNull(found);
        }

        [TestMethod]
        [DataRow(DatabaseProviders.SQLiteProviderName)]
        [DataRow(DatabaseProviders.SqlServerProviderName)]
        public void DatabaseProvider_TryParse_find_Test(string name)
        {
            // Arrange

            // Act
            bool actual = DatabaseProvider.TryParse(name, out DatabaseProvider found);

            // Assert
            Assert.IsTrue(actual);
            Assert.IsNotNull(found);
        }

        [TestMethod]
        public void DatabaseProvider_Equals_returns_false_for_different_objects_Test()
        {
            // Arrange
            DatabaseProvider other = DatabaseProviders.None;

            // Act
            bool actual = DatabaseProviders.SqlServer.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void DatabaseProvider_Equals_returns_true_for_different_object_with_same_value_Test()
        {
            // Arrange
            var other = new DatabaseProvider(DatabaseProviders.SqlServerProviderName);

            // Act
            bool actual = DatabaseProviders.SqlServer.Equals(other);

            // Assert
            Assert.IsTrue(actual);
        }
    }
}