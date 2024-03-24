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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DirectoryFolderEnumeratorIntegrationTest
    {
        [TestMethod]
        public void DirectoryFolderEnumerator_GetStructure_Test()
        {
            // Arrange
            string directory = Environment.CurrentDirectory;

            const bool subtree = false;
            const bool includeHiddenFolder = false;
            const bool includeCurrentFolder = true;
            const bool sizeAndFileCount = true;
            const bool sizeAndFileCountForSubTree = false;
            const bool includeOwner = true;
            const string ownerFilter = null;

            bool Cancel()
            {
                return false;
            }

            void Increment()
            {
            }

            var folderEnumeratorOptions = new FolderEnumeratorOptions(subtree,
                includeHiddenFolder, includeCurrentFolder, sizeAndFileCount, sizeAndFileCountForSubTree, includeOwner, ownerFilter);

            var services = new ServiceCollection();
            services.UsePlatformServices();
            services.AddLogging();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            var sut = serviceProvider.GetRequiredService<IDirectoryFolderEnumerator>();

            // Act
            IEnumerable<IFolderReport> actual = sut.GetStructure(directory, folderEnumeratorOptions, Increment, Cancel).ToList();

            // Assert
            Assert.IsNotNull(actual);
        }

        private static string StripInvalidPathChars(string s)
        {
            return s.Replace("/", "\\");
        }

        [TestMethod]
        public void DirectoryFolderEnumerator_GetStructure_include_current_folder_and_subfolder_tree_Test()
        {
            var directories = new[]
            {
                "a",
                "a/b/c"
            };

            var directoryPath = $"{Environment.CurrentDirectory}{nameof(this.DirectoryFolderEnumerator_GetStructure_include_current_folder_and_subfolder_tree_Test)}_{Guid.NewGuid():N}";
            foreach (string directory in directories)
            {
                string path = Path.Combine(directoryPath, StripInvalidPathChars(directory));
                Directory.CreateDirectory(path);
            }

            var expectedFilePaths = new[]
            {
                "",
                "a",
                "a/b",
                "a/b/c"
            };

            for (var i = 0; i < expectedFilePaths.Length; i++)
            {
                string path = Path.Combine(directoryPath, StripInvalidPathChars(expectedFilePaths[i]));
                expectedFilePaths[i] = path.TrimEnd('\\');
            }

            const bool subtree = true;
            const bool includeHiddenFolder = false;
            const bool includeCurrentFolder = true;
            const bool sizeAndFileCount = true;
            const bool sizeAndFileCountForSubTree = false;
            const bool includeOwner = true;
            const string ownerFilter = null;

            bool Cancel()
            {
                return false;
            }

            void Increment()
            {
            }

            var folderEnumeratorOptions = new FolderEnumeratorOptions(subtree,
                includeHiddenFolder, includeCurrentFolder, sizeAndFileCount, sizeAndFileCountForSubTree, includeOwner, ownerFilter);

            var services = new ServiceCollection();
            services.UsePlatformServices();
            services.AddLogging();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var sut = serviceProvider.GetRequiredService<IDirectoryFolderEnumerator>();


            // Act
            IEnumerable<IFolderReport> actual = sut.GetStructure(directoryPath, folderEnumeratorOptions, Increment, Cancel).ToList();

            // Assert
            Assert.IsNotNull(actual);

            List<string> actualFilePaths = actual.Select(report => report.FullName).ToList();
            Assert.IsTrue(expectedFilePaths.SequenceEqual(actualFilePaths));
        }
    }
}