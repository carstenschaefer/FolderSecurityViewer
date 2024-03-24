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
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Core;
    using Core.Abstractions;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DirectoryFolderEnumeratorTest
    {
        [TestMethod]
        public void DirectoryFolderEnumerator_GetStructure_access_denied_Test()
        {
            // Arrange
            var fileManagementServiceMock = new Mock<IFileManagementService>();
            var directoryServiceMock = new Mock<IDirectorySizeService>();
            var ownerServiceMock = new Mock<IOwnerService>();

            var kernel32Mock = new Mock<IKernel32>();

            var lastErrorReturnValues = new Queue<uint>(new[]
            {
                WinError.ErrorAccessDenied,
                WinError.ErrorNoMoreFiles
            });

            kernel32Mock.Setup(kernel32 => kernel32.GetLastError())
                .Returns(() => lastErrorReturnValues.Dequeue())
                .Verifiable();

            const string directoryPath = "directory-path";
            const string findFirstFilePathExpression = directoryPath + "\\*";

            var kernel32FindFileMock = new Mock<IKernel32FindFile>();

            var handle = new FindFileHandleStub(new IntPtr(-1));

            static void FindFirstFileDelegate(string name, out Win32FindData data)
            {
                data = default;
            }

            kernel32FindFileMock
                .Setup(file => file.FindFirstFile(findFirstFilePathExpression, out It.Ref<Win32FindData>.IsAny))
                .Callback(new FindFirstFileDelegate(FindFirstFileDelegate))
                .Returns(handle);

            const int findNextFileFailedResult = 0;
            kernel32FindFileMock.Setup(file => file.FindNextFile(handle, out It.Ref<Win32FindData>.IsAny)).Returns(findNextFileFailedResult);

            IFileManagementService fileManagementService = fileManagementServiceMock.Object;
            IDirectorySizeService directorySizeService = directoryServiceMock.Object;
            IOwnerService ownerService = ownerServiceMock.Object;
            IKernel32 kernel32 = kernel32Mock.Object;
            IKernel32FindFile kernel32FindFile = kernel32FindFileMock.Object;
            ILogger<DirectoryFolderEnumerator> enumeratorLogger = new Mock<ILogger<DirectoryFolderEnumerator>>().Object;

            var sut = new DirectoryFolderEnumerator(fileManagementService, directorySizeService, ownerService, kernel32, kernel32FindFile, enumeratorLogger);

            static void IncrementProgressCallback()
            {
            }

            const bool subtree = false;
            const bool includeHiddenFolder = false;
            const bool includeCurrentFolder = false;
            const bool sizeAndFileCount = true;
            const bool sizeAndFileCountForSubTree = false;
            const bool includeOwner = true;

            var folderEnumeratorOptions = new FolderEnumeratorOptions(subtree, includeHiddenFolder, includeCurrentFolder, sizeAndFileCount, sizeAndFileCountForSubTree, includeOwner, null);

            IEnumerable<IFolderReport> enumerable = sut.GetStructure(directoryPath, folderEnumeratorOptions, IncrementProgressCallback, () => false);

            // Act
            List<IFolderReport> actual = enumerable.ToList();

            // Assert
            Assert.IsNotNull(enumerable);

            IFolderReport single = actual.SingleOrDefault();
            Assert.IsNotNull(single);
            Assert.AreEqual(FolderReportStatus.AccessDeniedError, single.Status);
        }

        private delegate void FindFirstFileDelegate(string lpFileName, out Win32FindData lpFindFileData);

        private class FindFileHandleStub : FindFileHandle
        {
            public FindFileHandleStub(IntPtr handle)
            {
                this.handle = handle;
            }
        }
    }
}