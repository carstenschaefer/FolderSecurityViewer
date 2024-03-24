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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Configuration.Abstractions;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core;
    using FileSystem.Interop.Types;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Worker;
    using Xunit;

    public class AclComparerWorkerTest
    {
        [Fact]
        public void AclComparerWorker_RunWorkerAsync_no_sub_directories_Test()
        {
            // Arrange
            var configurationManagerMock = new Mock<IConfigurationManager>();
            IConfigurationManager configurationManager = configurationManagerMock.Object;

            var fileManagementServiceMock = new Mock<IFileManagementService>();
            fileManagementServiceMock
                .Setup(service => service.IsAccessDenied(It.IsAny<LongPath>()))
                .Returns(false)
                .Verifiable();

            fileManagementServiceMock
                .Setup(provider => provider.GetDirectories(It.IsAny<LongPath>()))
                .Returns(Enumerable.Empty<IFolder>())
                .Verifiable();

            var accessControlListViewProvider = new Mock<IAclViewProvider>();

            var aclModelBuilder = new AclModelBuilder();

            using var sut = new AclComparerWorker(aclModelBuilder, configurationManager,
                accessControlListViewProvider.Object,
                fileManagementServiceMock.Object, new NullLogger<AclComparerWorker>());

            var workerCompleted = false;
            using var resetEvent = new ManualResetEventSlim();

            void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                workerCompleted = true;
                sut.RunWorkerCompleted -= WorkerCompleted;
                resetEvent?.Set();
            }

            sut.RunWorkerCompleted += WorkerCompleted;

            // Act
            sut.RunWorkerAsync(Environment.CurrentDirectory);

            // Assert
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            resetEvent.Wait(timeout);

            Assert.True(workerCompleted);

            fileManagementServiceMock.Verify(service => service.IsAccessDenied(It.IsAny<LongPath>()), Times.Never);
        }

        [Fact]
        public void AclComparerWorker_RunWorkerAsync_sub_directories_Test()
        {
            // Arrange
            var configurationManagerMock = new Mock<IConfigurationManager>();
            IConfigurationManager configurationManager = configurationManagerMock.Object;

            var fileManagementServiceMock = new Mock<IFileManagementService>();
            fileManagementServiceMock
                .Setup(service => service.IsAccessDenied(It.IsAny<LongPath>()))
                .Returns(false)
                .Verifiable();

            LongPath directory = Environment.CurrentDirectory;
            string subFolder1 = Path.Combine(directory, "sub-folder1");
            string subFolder2 = Path.Combine(directory, "sub-folder2");
            var subDirectories = new List<string>
            {
                subFolder1,
                subFolder2
            };

            fileManagementServiceMock
                .Setup(provider => provider.GetDirectories(It.IsAny<LongPath>()))
                .Returns(Enumerable.Empty<IFolder>())
                .Verifiable();

            fileManagementServiceMock
                .Setup(provider => provider.GetDirectories(directory))
                .Returns(subDirectories.Select(s => new FolderStub(s, Path.GetDirectoryName(s), false)))
                .Verifiable();

            var accessControlListViewProvider = new Mock<IAclViewProvider>();

            var aclModelBuilder = new AclModelBuilder();

            using var sut = new AclComparerWorker(aclModelBuilder, configurationManager,
                accessControlListViewProvider.Object,
                fileManagementServiceMock.Object,
                new NullLogger<AclComparerWorker>());

            var workerCompleted = false;
            using var resetEvent = new ManualResetEventSlim();

            void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                workerCompleted = true;
                sut.RunWorkerCompleted -= WorkerCompleted;
                resetEvent?.Set();
            }

            sut.RunWorkerCompleted += WorkerCompleted;

            // Act
            sut.RunWorkerAsync(directory);

            // Assert
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            resetEvent.Wait(timeout);

            Assert.True(workerCompleted);

            fileManagementServiceMock.Verify(service => service.IsAccessDenied(It.IsAny<LongPath>()), Times.Exactly(2));
        }
    }

    public class FolderStub : IFolder
    {
        public FolderStub(string fullName, string name, bool hasSubFolders)
        {
            this.FullName = fullName;
            this.Name = name;
            this.HasSubFolders = hasSubFolders;
        }

        public string FullName { get; }
        public string Name { get; }
        public bool HasSubFolders { get; set; }
        public bool AccessDenied { get; set; } = false;
    }
}