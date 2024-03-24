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

namespace FSV.ViewModel.UnitTest
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Xml.Linq;
    using Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Exporter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ExportTableGeneratorTest
    {
        [TestMethod]
        public void ExportTableGenerator_OwnerTable_test()
        {
            Mock<IConfigurationManager> configurationMock = this.GetConfigurationManagerMock();
            IEnumerable<FolderItemViewModel> folders = this.GetFolders();

            var sut = new ExportTableGenerator(configurationMock.Object);

            DataTable result = sut.GetOwnerTable(folders, "Name", SortOrder.Ascending);

            Assert.AreEqual(result.Rows.Count, folders.Count());

            DataRow dataRow = result.Rows[0];
            Assert.AreEqual("Aspen", dataRow[0]);
        }

        [TestMethod]
        public void ExportTableGenerator_FolderTable_test()
        {
            Mock<IConfigurationManager> configurationMock = this.GetConfigurationManagerMock();
            IEnumerable<FolderItemViewModel> folders = this.GetFolders();

            var sut = new ExportTableGenerator(configurationMock.Object);

            DataTable result = sut.GetFolderTable(folders, "Name", SortOrder.Ascending);

            Assert.AreEqual(result.Rows.Count, folders.Count());

            DataRow dataRow = result.Rows[0];
            Assert.AreEqual("Aspen", dataRow[0]);
        }

        private IEnumerable<FolderItemViewModel> GetFolders()
        {
            List<FolderItemViewModel> folders = new()
            {
                new FolderItemViewModel { FullName = "some/path/Folder One", Name = "Folder One", Owner = "User" },
                new FolderItemViewModel { FullName = "some/path/Aspen", Name = "Aspen", Owner = "User" }
            };

            return folders;
        }

        private Mock<IConfigurationManager> GetConfigurationManagerMock()
        {
            XElement element = XElement.Parse(@"
                <FolderSecurityViewer>
                    <Culture>en-us</Culture>
                    <Report>
                        <Trustee><ScanLevel/><Settings /><ExclusionGroups/><TrusteeGridColumns /><RightsTranslations /><ExcludedBuiltInGroups /></Trustee>
                        <Folder>
                            <Owner>True</Owner>
                            <IncludeCurrentFolder>True</IncludeCurrentFolder>
                            <IncludeSubFolders>True</IncludeSubFolders>
                            <IncludeHiddenFolders>True</IncludeHiddenFolders>
                            <IncludeFileCount>True</IncludeFileCount>
                            <IncludeSubFolderFileCount>True</IncludeSubFolderFileCount>
                        </Folder>
                        <User />
                    </Report>
                </FolderSecurityViewer>");

            var configRoot = new ConfigRoot(element);

            var configurationManagerMock = new Mock<IConfigurationManager>();
            configurationManagerMock
                .SetupGet(m => m.ConfigRoot)
                .Returns(configRoot);

            return configurationManagerMock;
        }
    }
}