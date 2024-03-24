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
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Core.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Win32FindDataExtensionsTest
    {
        [TestMethod]
        public void Win32FindDataExtensions_IsCurrentDirectory_Test()
        {
            // Arrange
            var findData = new Win32FindDataStub
            {
                cFileName = "."
            };

            // Act
            Win32FindDataExtensions_TestHelper(findData, sut =>
            {
                bool actual = sut.IsCurrentDirectory();

                // Assert
                Assert.IsTrue(actual);
            });
        }

        [TestMethod]
        public void Win32FindDataExtensions_IsParentDirectory_Test()
        {
            // Arrange
            var findData = new Win32FindDataStub
            {
                cFileName = ".."
            };

            // Act
            Win32FindDataExtensions_TestHelper(findData, sut =>
            {
                bool actual = sut.IsParentDirectory();

                // Assert
                Assert.IsTrue(actual);
            });
        }

        private static void Win32FindDataExtensions_TestHelper(Win32FindDataStub stub, Action<Win32FindData> actAssert)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Win32FindDataStub>());
            try
            {
                Marshal.StructureToPtr(stub, ptr, true);
                var sut = Marshal.PtrToStructure<Win32FindData>(ptr);

                // Act
                actAssert(sut);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        ///     A writable representation of <see cref="Win32FindData" />.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public struct Win32FindDataStub
        {
            public uint dwFileAttributes;

            public FileTime ftCreationTime;

            public FileTime ftLastAccessTime;

            public FileTime ftLastWriteTime;

            public uint nFileSizeHigh;

            public uint nFileSizeLow;

            public uint dwReserved0;

            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.MaxPath)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;

            public uint dwFileType;

            public uint dwCreatorType;

            public uint wFinderFlags;
        }
    }
}