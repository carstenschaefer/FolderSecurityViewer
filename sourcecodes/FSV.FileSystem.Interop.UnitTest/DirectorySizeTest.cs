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
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DirectorySizeTest
    {
        [TestMethod]
        public void DirectorySize_Get_Test()
        {
            // Arrange
            string directoryPath = Environment.CurrentDirectory;

            const bool includeSubtrees = true;
            const bool includeHiddenFolders = true;

            var services = new ServiceCollection();
            services.UsePlatformServices();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var sut = serviceProvider.GetRequiredService<IDirectorySizeService>();

            // Act
            FolderSizeInfo actual = sut.AggregateSizeInfo(directoryPath, includeSubtrees, includeHiddenFolders);

            // Assert
            Assert.IsGreaterThan(0, actual.Size);
            Assert.IsGreaterThan(0, actual.FileCount);
        }
    }
}