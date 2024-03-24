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
    using System.Threading;
    using System.Xml.Linq;
    using Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Sections.ConfigXml;

    [TestClass]
    public class CultureManagerTest
    {
        private const string CultureName = "de-de";

        [TestMethod]
        public void CultureManager_culture_test()
        {
            Mock<IConfigurationManager> configurationManagerMock = this.GetConfigurationManagerMock();

            var sut = new CultureManager(configurationManagerMock.Object);

            sut.InitializeCulture();

            Assert.AreEqual(Thread.CurrentThread.CurrentCulture.Name.ToLower(), CultureName.ToLower());
        }

        private Mock<IConfigurationManager> GetConfigurationManagerMock()
        {
            XElement element = XElement.Parse($@"
                <FolderSecurityViewer>
                    <Culture>{CultureName}</Culture>
                    <Report>
                        <Trustee><ScanLevel/><Settings /><ExclusionGroups/><TrusteeGridColumns /><RightsTranslations /><ExcludedBuiltInGroups /></Trustee>
                        <Folder />
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