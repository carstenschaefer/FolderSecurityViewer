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

namespace FSV.Business.UnitTest
{
    using System;
    using System.Threading.Tasks;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop.Abstractions;
    using Models;
    using Moq;
    using Xunit;

    public class AclCompareTaskTest
    {
        [Fact]
        public async Task AclComparerTask_RunAsync_smoke_Test()
        {
            // Arrange
            var aclModelBuilderMock = new Mock<IAclModelBuilder>();
            var aclViewProviderMock = new Mock<IAclViewProvider>();
            var fileManagementServiceMock = new Mock<IFileManagementService>();

            ReportTrustee GetConfigReportTrustee()
            {
                return null;
            }

            var sut = new AclCompareTask(
                aclModelBuilderMock.Object,
                aclViewProviderMock.Object,
                fileManagementServiceMock.Object,
                GetConfigReportTrustee);

            string path = Environment.CurrentDirectory;

            var completed = false;

            void OnComplete()
            {
                completed = true;
            }

            void OnProgress(AclComparisonResult comparerResult)
            {
            }

            // Act

            await sut.RunAsync(path, OnProgress, OnComplete);

            // Assert
            Assert.True(completed);
        }
    }
}