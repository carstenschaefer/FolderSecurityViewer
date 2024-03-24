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
    using System;
    using Core;
    using Core.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FindFileEnumeratorTest
    {
        [TestMethod]
        public void FindFileEnumerator_ctor_Current_returns_default_Test()
        {
            // Arrange
            string path = Environment.CurrentDirectory;

            var kernel32 = new Kernel32Wrapper();
            var kernel32FindFile = new Kernel32FindFileWrapper();

            // Act
            using var sut = new FindFileEnumerator(kernel32, kernel32FindFile, path);

            // Assert
            Assert.AreEqual(sut.Current, default);
        }

        [TestMethod]
        public void FindFileEnumerator_MoveNext_Test()
        {
            // Arrange
            string path = Environment.CurrentDirectory;
            const string expected = "Debug";

            var kernel32 = new Kernel32Wrapper();
            var kernel32FindFile = new Kernel32FindFileWrapper();

            using var sut = new FindFileEnumerator(kernel32, kernel32FindFile, path);

            // Act
            bool actual = sut.MoveNext();

            // Assert
            Assert.IsTrue(actual);

            Win32FindData current = sut.Current.GetWin32FindData(false);
            Assert.AreNotEqual(default, current);
            Assert.AreEqual(expected, current.cFileName);
        }

        [TestMethod]
        public void FindFileEnumerator_Reset_starts_over_Test()
        {
            // Arrange
            string path = Environment.CurrentDirectory;

            var kernel32 = new Kernel32Wrapper();
            var kernel32FindFile = new Kernel32FindFileWrapper();

            using var sut = new FindFileEnumerator(kernel32, kernel32FindFile, path);

            sut.MoveNext();

            // Act
            bool actual = sut.MoveNext();
            sut.Reset();

            // Assert
            Assert.AreEqual(sut.Current, default);
        }
    }
}