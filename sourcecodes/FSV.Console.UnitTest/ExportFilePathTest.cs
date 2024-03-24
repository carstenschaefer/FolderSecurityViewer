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

namespace FSV.Console.UnitTest
{
    using System;
    using System.IO;
    using Configuration.Abstractions;
    using Exporter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ExportFilePathTest
    {
        [TestMethod]
        public void ExportFilePath_GetFilePath_test()
        {
            string currentDirectory = Environment.CurrentDirectory;
            const string extension = ".html";

            var configPathMock = new Mock<IConfigurationPaths>();
            configPathMock.Setup(m => m.GetExportFilePath(It.IsAny<string>()))
                .Returns((string name) => Path.Combine(currentDirectory, name));

            var exportFilePath = new ExportFilePath(configPathMock.Object, currentDirectory);
            string filePath = exportFilePath.GetFilePath(extension);

            Assert.AreEqual(
                Path.Combine(currentDirectory, $"{DateTime.Now:yyyy-MMM-dd_HH-mm-ss}{extension}"),
                filePath);
        }

        [TestMethod]
        public void ExportFilePath_GetFilePath_with_prefix_test()
        {
            string currentDirectory = Environment.CurrentDirectory;
            const string extension = ".html";
            const string prefix = "sh_";

            var configPathMock = new Mock<IConfigurationPaths>();
            configPathMock.Setup(m => m.GetExportFilePath(It.IsAny<string>()))
                .Returns((string name) => Path.Combine(currentDirectory, name));

            var exportFilePath = new ExportFilePath(configPathMock.Object, currentDirectory, prefix);
            string filePath = exportFilePath.GetFilePath(extension);

            Assert.AreEqual(
                Path.Combine(currentDirectory, $"{prefix} - {DateTime.Now:yyyy-MMM-dd_HH-mm-ss}{extension}"),
                filePath);
        }
    }
}