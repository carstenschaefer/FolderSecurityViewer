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
    using System.IO;
    using System.Linq;
    using Abstractions;
    using Core;
    using Core.Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileManagementTest
    {
        [TestMethod]
        public void FileManagement_GetDirectories_Test()
        {
            // Arrange
            string directoryPath = Environment.CurrentDirectory;
            FileManagement sut = CreateFileManagement();

            // Act
            IEnumerable<IFolder> actual = sut.GetDirectories(directoryPath).ToList();

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void FileManagement_HasSubFolders_Test()
        {
            // Arrange
            string directoryPath = Environment.CurrentDirectory;
            string testDirectoryPath = Path.Combine(directoryPath, nameof(this.FileManagement_HasSubFolders_Test) + "_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectoryPath);

            FileManagement sut = CreateFileManagement();

            // Act
            bool currentDirectoryPathResult = sut.HasSubFolders(directoryPath);
            bool testDirectoryPathResult = sut.HasSubFolders(testDirectoryPath);
            Directory.Delete(testDirectoryPath);

            // Assert
            Assert.IsTrue(currentDirectoryPathResult);
            Assert.IsFalse(testDirectoryPathResult);
        }

        [TestMethod]
        public void FileManagement_FindFilesAndDirs_Test()
        {
            // Arrange
            string directoryPath = Environment.CurrentDirectory;
            FileManagement sut = CreateFileManagement();

            // Act
            IEnumerable<LongPath> actual = sut.FindFilesAndDirs(directoryPath);
            List<LongPath> results = actual?.ToList();

            // Assert
            Assert.IsNotNull(actual);
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void FileManagement_GetAclView_Test()
        {
            // Arrange
            string directoryPath = Environment.CurrentDirectory;

            FileManagement sut = CreateFileManagement();

            // Act
            IEnumerable<IAcl> actual = sut.GetAclView(directoryPath);
            List<IAcl> results = actual?.ToList();

            // Assert
            Assert.IsNotNull(actual);
            Assert.IsNotNull(results);
        }

        private static FileManagement CreateFileManagement()
        {
            var services = new ServiceCollection();
            services.UsePlatformServices();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            var advapi32 = serviceProvider.GetRequiredService<IAdvapi32>();
            var kernel32 = serviceProvider.GetRequiredService<IKernel32>();
            var kernel32FindFile = serviceProvider.GetRequiredService<IKernel32FindFile>();
            var sidUtil = serviceProvider.GetRequiredService<ISidUtil>();

            return new FileManagement(advapi32, kernel32, kernel32FindFile, sidUtil);
        }
    }
}