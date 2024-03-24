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
    using System.IO;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core;
    using FileSystem.Interop.Types;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Xunit;

    public class UserPermissionTaskProcessDirectoryTest
    {
        [Fact]
        public async Task UserPermissionTask_RunAsync_ProcessDirectory_does_not_query_directories_if_no_sub_folders_detected_Test()
        {
            // Arrange
            const string userName = "user";

            var aclModelBuilderMock = new Mock<IAclModelBuilder>();

            var finderMock = new Mock<IUserActiveDirectoryFinder>();
            finderMock.SetupSet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>()).Verifiable();

            var activeDirectoryFinderFactory = new Mock<IActiveDirectoryFinderFactory>();
            activeDirectoryFinderFactory
                .Setup(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()))
                .Returns(finderMock.Object)
                .Verifiable();

            var aclViewProviderMock = new Mock<IAclViewProvider>();
            aclViewProviderMock.Setup(provider => provider.GetAclView(It.IsAny<LongPath>())).Returns(Enumerable.Empty<Acl>()).Verifiable();

            var configurationManagerMock = new Mock<IConfigurationManager>();

            var folderDirectoryEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();

            const int scanLevel = 2;

            static ReportUser CreateReportUser()
            {
                var reportUserXml = $"<User><ScanLevel>{scanLevel}</ScanLevel></User>";
                XElement userElement = XElement.Parse(reportUserXml);
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return null;
            }

            using var sut = new UserPermissionTask(
                aclModelBuilderMock.Object,
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                folderDirectoryEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync(userName, Environment.CurrentDirectory, OnProgress);

            // Assert
            Assert.NotNull(actual);
            activeDirectoryFinderFactory.Verify(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()), Times.Once);
            finderMock.VerifySet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>(), Times.Never());
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_ProcessDirectory_invokes_finder_FindUser_for_each_acl_Test()
        {
            // Arrange
            const string userName = "user";

            var aclModelBuilder = new AclModelBuilder();

            var finderMock = new Mock<IUserActiveDirectoryFinder>();
            finderMock.SetupSet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>()).Verifiable();
            finderMock.Setup(finder => finder.FindUser(userName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Verifiable();

            // Info: this ScanOptions-object is pain in the ass; if not properly set, it will break the task´s execution
            var activeDirectoryScanOptions = new ActiveDirectoryScanOptions(new NullLogger<ActiveDirectoryScanOptions>()) { ExclusionGroups = new List<ConfigItem>() };
            finderMock.SetupGet(finder => finder.ScanOptions).Returns(activeDirectoryScanOptions);

            var activeDirectoryFinderFactory = new Mock<IActiveDirectoryFinderFactory>();
            activeDirectoryFinderFactory
                .Setup(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()))
                .Returns(finderMock.Object)
                .Verifiable();

            var aclList = new List<Acl>
            {
                new(userName, AccessControlType.Allow, FileSystemRights.Read, InheritanceFlags.None, PropagationFlags.None)
            };

            var aclViewProviderMock = new Mock<IAclViewProvider>();
            aclViewProviderMock.Setup(provider => provider.GetAclView(It.IsAny<LongPath>()))
                .Returns(aclList).Verifiable();

            var configurationManagerMock = new Mock<IConfigurationManager>();

            var folderDirectoryEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();
            var folderReports = new List<IFolderReport> { new FolderReportStub(Path.Combine(Environment.CurrentDirectory, "sub-folder-1")) };
            folderDirectoryEnumeratorMock.Setup(enumerator => enumerator.GetStructure(It.IsAny<LongPath>(), It.IsAny<FolderEnumeratorOptions>(), It.IsAny<Action>(), It.IsAny<Func<bool>>()))
                .Returns(folderReports)
                .Verifiable();

            const int scanLevel = 2;

            static ReportUser CreateReportUser()
            {
                var reportUserXml = $"<User><ScanLevel>{scanLevel}</ScanLevel></User>";
                XElement userElement = XElement.Parse(reportUserXml);
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return null;
            }

            using var sut = new UserPermissionTask(
                aclModelBuilder,
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                folderDirectoryEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync(userName, Environment.CurrentDirectory, OnProgress);

            // Assert
            Assert.NotNull(actual);

            activeDirectoryFinderFactory.Verify(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()), Times.Once);
            finderMock.VerifySet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>(), Times.Once());
            finderMock.Verify(finder => finder.FindUser(userName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(aclList.Count));
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_ProcessDirectory_FileManagementService_GetDirectories_throws_access_denied_error_does_not_break_iteration_Test()
        {
            // Arrange
            const string userName = "user";

            var aclModelBuilder = new AclModelBuilder();

            var finderMock = new Mock<IUserActiveDirectoryFinder>();
            finderMock.SetupSet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>()).Verifiable();
            finderMock.Setup(finder => finder.FindUser(userName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Verifiable();

            // Info: this ScanOptions-object is pain in the ass; if not properly set, it will break the task´s execution
            var activeDirectoryScanOptions = new ActiveDirectoryScanOptions(new NullLogger<ActiveDirectoryScanOptions>()) { ExclusionGroups = new List<ConfigItem>() };
            finderMock.SetupGet(finder => finder.ScanOptions).Returns(activeDirectoryScanOptions);

            var activeDirectoryFinderFactory = new Mock<IActiveDirectoryFinderFactory>();
            activeDirectoryFinderFactory
                .Setup(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()))
                .Returns(finderMock.Object)
                .Verifiable();

            var aclList = new List<Acl>
            {
                new(userName, AccessControlType.Allow, FileSystemRights.Read, InheritanceFlags.None, PropagationFlags.None)
            };

            var aclViewProviderMock = new Mock<IAclViewProvider>();
            aclViewProviderMock.Setup(provider => provider.GetAclView(It.IsAny<LongPath>()))
                .Returns(aclList).Verifiable();

            string currentDirectory = Environment.CurrentDirectory;

            var folderDirectoryEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();
            var folderReports = new List<IFolderReport> { new FolderReportStub(Path.Combine(currentDirectory, "protected-folder"), "You´re not allowed to watch this my friend.") };
            folderDirectoryEnumeratorMock.Setup(enumerator => enumerator.GetStructure(It.IsAny<LongPath>(), It.IsAny<FolderEnumeratorOptions>(), It.IsAny<Action>(), It.IsAny<Func<bool>>()))
                .Returns(folderReports)
                .Verifiable();

            const int scanLevel = 2;

            static ReportUser CreateReportUser()
            {
                var reportUserXml = $"<User><ScanLevel>{scanLevel}</ScanLevel></User>";
                XElement userElement = XElement.Parse(reportUserXml);
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return null;
            }

            using var sut = new UserPermissionTask(
                aclModelBuilder,
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                folderDirectoryEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync(userName, currentDirectory, OnProgress);

            // Assert
            activeDirectoryFinderFactory.Verify(factory => factory.CreateUserActiveDirectoryFinder(userName, It.IsAny<ActiveDirectoryScanResult<int>>()), Times.Once);
            finderMock.VerifySet(finder => finder.CurrentDirectory = It.IsAny<DirectoryInfo>(), Times.Once());
            finderMock.Verify(finder => finder.FindUser(userName, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(aclList.Count));
        }
    }

    public class FolderReportStub : IFolderReport
    {
        public FolderReportStub(string fullName, string errorMessage = null)
        {
            this.FullName = fullName;
            this.ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; }

        public string FullName { get; }
        public string Name { get; }
        public long FileCount { get; }
        public double Size { get; }
        public long FileCountInclSub { get; }
        public double SizeInclSub { get; }
        public string Owner { get; }
        public Exception Exception { get; }
        public FolderReportStatus Status { get; }
    }
}