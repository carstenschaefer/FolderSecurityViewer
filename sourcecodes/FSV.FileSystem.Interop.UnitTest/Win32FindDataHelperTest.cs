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

namespace FSV.FileSystem.Interop.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Win32FindDataHelperTest
    {
        [TestMethod]
        public void Win32FindDataHelper_HandleLongPath_short_path_remains_unprefixed_Test()
        {
            // Arrange
            const string shortPath = "Z:\\temp";

            // Act
            string actual = Win32FindDataHelper.HandleLongPath(shortPath);

            // Assert
            Assert.AreEqual(shortPath, actual);
        }

        [TestMethod]
        public void Win32FindDataHelper_HandleLongPath_returns_prefixed_string_for_long_path_Test()
        {
            // Arrange
            const int length = 260;
            var longPathExpression = $"Z:\\temp\\{new string('z', length)}";
            string expectedPathExpression = @"\\?\" + longPathExpression;

            // Act
            string actual = Win32FindDataHelper.HandleLongPath(longPathExpression);

            // Assert
            Assert.AreEqual(expectedPathExpression, actual);
        }

        [TestMethod]
        public void Win32FindDataHelper_HandleLongPath_checks_for_existing_prefix_Test()
        {
            // Arrange
            const int length = 260;
            var longPathExpression = $"Z:\\temp\\{new string('z', length)}";
            longPathExpression = @"\\?\" + longPathExpression;

            // Act
            string actual = Win32FindDataHelper.HandleLongPath(longPathExpression);

            // Assert
            Assert.AreEqual(longPathExpression, actual);
        }
    }
}