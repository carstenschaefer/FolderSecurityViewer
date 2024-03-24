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
    using System.Linq;
    using System.Security.AccessControl;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Abstractions;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core;
    using FileSystem.Interop.Types;
    using Microsoft.Extensions.Logging.Abstractions;
    using Mocks;
    using Moq;
    using Xunit;

    public class UserPermissionTaskTest
    {
        [Fact]
        public void UserPermissionTask_ctor_does_not_throw_if_ReportUser_factory_returns_null_Test()
        {
            // Arrange
            var aclModelBuilderMock = new Mock<IAclModelBuilder>();
            var activeDirectoryFinderFactory = new Mock<IActiveDirectoryFinderFactory>();
            var aclViewProviderMock = new Mock<IAclViewProvider>();
            var folderDirectoryEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();
            var configurationManagerMock = new Mock<IConfigurationManager>();

            static ReportUser CreateReportUser()
            {
                return null;
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return null;
            }

            // Act
            UserPermissionTask Act()
            {
                return new UserPermissionTask(
                    aclModelBuilderMock.Object,
                    activeDirectoryFinderFactory.Object,
                    aclViewProviderMock.Object,
                    folderDirectoryEnumeratorMock.Object,
                    FolderEnumeratorOptionsFactory,
                    new NullLogger<UserPermissionTask>(),
                    CreateReportUser);
            }

            // Assert
            UserPermissionTask actual = Act();
            Assert.NotNull(actual);
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_throws_BusinessServiceException_if_finder_factory_does_not_return_valid_IUserActiveDirectoryFinder_instance_Test()
        {
            // Arrange
            var aclModelBuilderMock = new Mock<IAclModelBuilder>();
            var activeDirectoryFinderFactory = new Mock<IActiveDirectoryFinderFactory>();
            var aclViewProviderMock = new Mock<IAclViewProvider>();
            var directoryFolderEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();
            var configurationManagerMock = new Mock<IConfigurationManager>();

            static ReportUser CreateReportUser()
            {
                XElement userElement = XElement.Parse("<User />");
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return null;
            }

            var sut = new UserPermissionTask(
                aclModelBuilderMock.Object,
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                directoryFolderEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            async Task Act()
            {
                UserPermissionTaskResult actual = await sut.RunAsync("user", Environment.CurrentDirectory, OnProgress);
            }

            // Assert
            await Assert.ThrowsAsync<BusinessServiceException>(async () => await Act());
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_ScanDepth_1_Test()
        {
            // Arrange
            var configurationManagerMock = new Mock<IConfigurationManager>();

            Mock<IActiveDirectoryFinderFactory> activeDirectoryFinderFactory = this.GetActiveDirectoryFinderFactoryMock();
            Mock<IAclViewProvider> aclViewProviderMock = this.GetAclViewProviderMock();
            Mock<IDirectoryFolderEnumerator> directoryFolderEnumeratorMock = this.GetDirectoryFolderEnumeratorMock();

            static ReportUser CreateReportUser()
            {
                XElement userElement = XElement.Parse("<User><ScanLevel>1</ScanLevel></User>");
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return new FolderEnumeratorOptions(true, true, true, true, true, true, string.Empty);
            }

            var sut = new UserPermissionTask(
                new AclModelBuilder(),
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                directoryFolderEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync("user", "some", OnProgress);

            // Assert
            Assert.Equal(2, actual.Result.Rows.Count);
            Assert.True(actual.ExceptionFolders.Any());
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_ScanDepth_2_Test()
        {
            // Arrange
            var configurationManagerMock = new Mock<IConfigurationManager>();

            Mock<IActiveDirectoryFinderFactory> activeDirectoryFinderFactory = this.GetActiveDirectoryFinderFactoryMock();
            Mock<IAclViewProvider> aclViewProviderMock = this.GetAclViewProviderMock();
            Mock<IDirectoryFolderEnumerator> directoryFolderEnumeratorMock = this.GetDirectoryFolderEnumeratorMock();

            static ReportUser CreateReportUser()
            {
                XElement userElement = XElement.Parse("<User><ScanLevel>2</ScanLevel></User>");
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return new FolderEnumeratorOptions(true, true, true, true, true, true, string.Empty);
            }

            var sut = new UserPermissionTask(
                new AclModelBuilder(),
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                directoryFolderEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync("user", "some", OnProgress);

            // Assert
            Assert.Equal(6, actual.Result.Rows.Count);
            Assert.True(actual.ExceptionFolders.Any());
        }

        [Fact]
        public async Task UserPermissionTask_RunAsync_ScanDepth_0_Test()
        {
            // Arrange
            var configurationManagerMock = new Mock<IConfigurationManager>();

            Mock<IActiveDirectoryFinderFactory> activeDirectoryFinderFactory = this.GetActiveDirectoryFinderFactoryMock();
            Mock<IAclViewProvider> aclViewProviderMock = this.GetAclViewProviderMock();
            Mock<IDirectoryFolderEnumerator> directoryFolderEnumeratorMock = this.GetDirectoryFolderEnumeratorMock();

            static ReportUser CreateReportUser()
            {
                XElement userElement = XElement.Parse("<User><ScanLevel>0</ScanLevel></User>");
                return new ReportUser(userElement);
            }

            static FolderEnumeratorOptions FolderEnumeratorOptionsFactory()
            {
                return new FolderEnumeratorOptions(true, true, true, true, true, true, string.Empty);
            }

            var sut = new UserPermissionTask(
                new AclModelBuilder(),
                activeDirectoryFinderFactory.Object,
                aclViewProviderMock.Object,
                directoryFolderEnumeratorMock.Object,
                FolderEnumeratorOptionsFactory,
                new NullLogger<UserPermissionTask>(),
                CreateReportUser);

            static void OnProgress(int i)
            {
            }

            // Act
            UserPermissionTaskResult actual = await sut.RunAsync("user", "some", OnProgress);

            // Assert
            Assert.Equal(12, actual.Result.Rows.Count);
            Assert.True(actual.ExceptionFolders.Any());
        }

        private Mock<IDirectoryFolderEnumerator> GetDirectoryFolderEnumeratorMock()
        {
            var directoryFolderEnumeratorMock = new Mock<IDirectoryFolderEnumerator>();

            static IFolderReport GetFolderReport(string fullName, string name, Exception ex = null)
            {
                Mock<IFolderReport> mock = new();
                mock.SetupGet(m => m.FullName).Returns(fullName);
                mock.SetupGet(m => m.Name).Returns(name);
                mock.SetupGet(m => m.Exception).Returns(ex);

                return mock.Object;
            }

            directoryFolderEnumeratorMock
                .Setup(m =>
                    m.GetStructure(
                        It.IsNotNull<LongPath>(),
                        It.IsAny<FolderEnumeratorOptions>(),
                        It.IsAny<Action>(),
                        It.IsAny<Func<bool>>()))
                .Returns(() =>
                {
                    return new List<IFolderReport>
                    {
                        GetFolderReport("some\\test", "test"),
                        GetFolderReport("some\\test\\first", "first"),
                        GetFolderReport("some\\test\\first\\one", "one"),
                        GetFolderReport("some\\test\\first\\one\\01", "01"),
                        GetFolderReport("some\\test\\first\\one\\02", "02"),
                        GetFolderReport("some\\test\\first\\two", "two"),
                        GetFolderReport("some\\test\\second", "second"),
                        GetFolderReport("some\\test\\second\\un", "un"),
                        GetFolderReport("some\\test\\second\\dos", "dos"),
                        GetFolderReport("some\\test\\ja", "ja"),
                        GetFolderReport("some\\test\\haan", "haan"),
                        GetFolderReport("some\\test\\error", "error", new Exception()),
                        GetFolderReport("some\\moon", "moon")
                    };
                })
                .Verifiable();

            return directoryFolderEnumeratorMock;
        }

        private Mock<IAclViewProvider> GetAclViewProviderMock()
        {
            Mock<IAclViewProvider> mockAclViewProvider = new();

            static IAcl GetAcl(string accountName, AccountType accountType)
            {
                Mock<IAcl> mock = new();

                mock.SetupGet(m => m.AccountName).Returns(accountName);
                mock.SetupGet(m => m.AccountType).Returns(accountType);
                mock.SetupGet(m => m.Rights).Returns(FileSystemRights.FullControl);
                mock.SetupGet(m => m.Type).Returns(AccessControlType.Allow);

                return mock.Object;
            }

            List<IAcl> acls = new()
            {
                GetAcl("user", AccountType.User),
                GetAcl("Batman", AccountType.User),
                GetAcl("Avengers", AccountType.Group)
            };

            mockAclViewProvider.Setup(m => m.GetAclView(It.IsAny<LongPath>()))
                .Returns(acls);

            return mockAclViewProvider;
        }

        private Mock<IActiveDirectoryFinderFactory> GetActiveDirectoryFinderFactoryMock()
        {
            Mock<IActiveDirectoryFinderFactory> mockActiveDirectoryFinderFactory = new();

            mockActiveDirectoryFinderFactory
                .Setup(m =>
                    m.CreateUserActiveDirectoryFinder(
                        It.IsNotNull<string>(),
                        It.IsAny<ActiveDirectoryScanResult<int>>()))
                .Returns((string userName, ActiveDirectoryScanResult<int> scanResult) => { return new MockActiveDirectoryFinder(scanResult); });

            return mockActiveDirectoryFinderFactory;
        }
    }
}