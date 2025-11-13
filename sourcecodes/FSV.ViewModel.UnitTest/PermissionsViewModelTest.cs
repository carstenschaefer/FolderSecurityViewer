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
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Abstractions;
    using Business.Abstractions;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Exporter;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core;
    using FileSystem.Interop.Types;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Permission;
    using Prism.Events;

    [TestClass]
    public class PermissionsViewModelTest
    {
        [TestMethod]
        public void PermissionsViewModel_AccessControlList_does_not_throw_if_first_item_is_not_PermissionItemAclViewModel_Test()
        {
            // Arrange
            PermissionsViewModel sut = CreatePermissionsViewModel();

            var permissionItemOwnerViewModel = new PermissionItemOwnerViewModel(new Mock<IOwnerService>().Object, "path");
            sut.Items.Add(permissionItemOwnerViewModel);

            // Act
            IEnumerable<IAclModel> actual = sut.AccessControlList;

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void PermissionsViewModel_AccessControlList_does_not_throw_Test()
        {
            // TODO: this is a basic test to prevent regressions; it needs a better implementation once we have reworked the ReportTrustee-configuration

            // Arrange
            PermissionsViewModel sut = CreatePermissionsViewModel();

            IFlyOutService flyOutService = new Mock<IFlyOutService>().Object;
            var aclModelBuilder = new AclModelBuilder();
            var accessControlListViewProviderMock = new Mock<IAclViewProvider>();
            accessControlListViewProviderMock.Setup(provider => provider.GetAclView(It.IsAny<LongPath>())).Returns(new List<IAcl>());

            var logger = new NullLogger<PermissionItemAclViewModel>();

            static ReportTrustee ReportTrustee()
            {
                return null;
            }

            var expected = new PermissionItemAclViewModel(flyOutService, aclModelBuilder, accessControlListViewProviderMock.Object, logger, ReportTrustee, "path");
            sut.Items.Add(expected);

            var permissionItemOwnerViewModel = new PermissionItemOwnerViewModel(new Mock<IOwnerService>().Object, "path");
            sut.Items.Add(permissionItemOwnerViewModel);

            // Act
            IEnumerable<IAclModel> actual = sut.AccessControlList;

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void PermissionsViewModel_export_table_generator_test()
        {
            PermissionsViewModel vm = CreatePermissionsViewModelForExport();
            IConfigurationManager configurationManager = new Mock<IConfigurationManager>().Object;

            var sut = new ExportTableGenerator(configurationManager);
            DataTable result = sut.GetPermissionsSortTable(vm);

            Assert.AreEqual(vm.AllPermissions.TableName, result.TableName);
            Assert.HasCount(vm.AllPermissions.Rows.Count, result.Rows);
        }

        private static PermissionsViewModel CreatePermissionsViewModel()
        {
            IServiceProvider serviceProvider = new Mock<IServiceProvider>().Object;

            IDialogService dialogService = new Mock<IDialogService>().Object;
            IDispatcherService dispatcherService = new Mock<IDispatcherService>().Object;
            IEventAggregator eventAggregator = new Mock<IEventAggregator>().Object;
            IFlyOutService flyOutService = new Mock<IFlyOutService>().Object;
            IPermissionReportManager reportManager = new Mock<IPermissionReportManager>().Object;
            IPermissionTask permissionTask = new Mock<IPermissionTask>().Object;
            IDatabaseConfigurationManager dbConfigurationManager = new Mock<IDatabaseConfigurationManager>().Object;
            IConfigurationManager configurationManager = new Mock<IConfigurationManager>().Object;
            IExportService exportService = new Mock<IExportService>().Object;
            INavigationService navigationService = new Mock<INavigationService>().Object;

            var permissionItemAclViewModelBuilder = new ModelBuilder<string, PermissionItemAclViewModel>(serviceProvider);
            var permissionItemAclDifferenceViewModelBuilder = new ModelBuilder<string, PermissionItemACLDifferenceViewModel>(serviceProvider);
            var permissionItemSavedReportsViewModelBuilder = new ModelBuilder<string, PermissionItemSavedReportsViewModel>(serviceProvider);
            var permissionItemOwnerViewModelBuilder = new ModelBuilder<string, PermissionItemOwnerViewModel>(serviceProvider);
            var groupPermissionsViewModelBuilder = new ModelBuilder<DataTable, string, GroupPermissionsViewModel>(serviceProvider);

            const string path = "path";

            ReportTrustee ReportTrustee()
            {
                return null;
            }

            var sut = new PermissionsViewModel(
                dialogService,
                dispatcherService,
                eventAggregator,
                flyOutService,
                reportManager,
                permissionTask,
                dbConfigurationManager,
                configurationManager,
                ReportTrustee,
                new NullLogger<PermissionsViewModel>(),
                exportService,
                navigationService,
                permissionItemAclViewModelBuilder,
                permissionItemAclDifferenceViewModelBuilder,
                permissionItemSavedReportsViewModelBuilder,
                permissionItemOwnerViewModelBuilder,
                groupPermissionsViewModelBuilder,
                path);

            return sut;
        }

        private static PermissionsViewModel CreatePermissionsViewModelForExport()
        {
            IServiceProvider serviceProvider = new Mock<IServiceProvider>().Object;

            IDialogService dialogService = new Mock<IDialogService>().Object;
            IDispatcherService dispatcherService = new Mock<IDispatcherService>().Object;
            IEventAggregator eventAggregator = new Mock<IEventAggregator>().Object;
            IFlyOutService flyOutService = new Mock<IFlyOutService>().Object;
            IPermissionReportManager reportManager = new Mock<IPermissionReportManager>().Object;
            IPermissionTask permissionTask = GetPermissionTask();
            IDatabaseConfigurationManager dbConfigurationManager = new Mock<IDatabaseConfigurationManager>().Object;
            IConfigurationManager configurationManager = new Mock<IConfigurationManager>().Object;
            IExportService exportService = new Mock<IExportService>().Object;
            INavigationService navigationService = new Mock<INavigationService>().Object;

            var permissionItemAclViewModelBuilder = new ModelBuilder<string, PermissionItemAclViewModel>(serviceProvider);
            var permissionItemAclDifferenceViewModelBuilder = new ModelBuilder<string, PermissionItemACLDifferenceViewModel>(serviceProvider);
            var permissionItemSavedReportsViewModelBuilder = new ModelBuilder<string, PermissionItemSavedReportsViewModel>(serviceProvider);
            var permissionItemOwnerViewModelBuilder = new ModelBuilder<string, PermissionItemOwnerViewModel>(serviceProvider);
            var groupPermissionsViewModelBuilder = new ModelBuilder<DataTable, string, GroupPermissionsViewModel>(serviceProvider);

            const string path = "path";

            ReportTrustee ReportTrustee()
            {
                return null;
            }

            var sut = new PermissionsViewModel(
                dialogService,
                dispatcherService,
                eventAggregator,
                flyOutService,
                reportManager,
                permissionTask,
                dbConfigurationManager,
                configurationManager,
                ReportTrustee,
                new NullLogger<PermissionsViewModel>(),
                exportService,
                navigationService,
                permissionItemAclViewModelBuilder,
                permissionItemAclDifferenceViewModelBuilder,
                permissionItemSavedReportsViewModelBuilder,
                permissionItemOwnerViewModelBuilder,
                groupPermissionsViewModelBuilder,
                path);

            return sut;
        }

        private static IPermissionTask GetPermissionTask()
        {
            var mock = new Mock<IPermissionTask>();

            var table = new DataTable("Permissions");
            table.Columns.Add(new DataColumn("Name"));

            table.Rows.Add("First");
            table.Rows.Add("Second");

            mock.Setup(m => m.RunAsync(It.IsAny<string>(), It.IsAny<Action<int>>()))
                .Returns(() => Task.FromResult(table));

            return mock.Object;
        }
    }
}