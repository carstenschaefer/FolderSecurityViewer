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
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using AdMembers;
    using AdServices;
    using Configuration.Abstractions;
    using Exporter;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using Moq;

    [TestClass]
    public class GroupMembersViewModelTest
    {
        [TestMethod]
        public async Task GroupMembersViewModel_list_test()
        {
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            GroupMembersViewModel sut = new ModelBuilder<string, GroupMembersViewModel>(serviceProvider).Build("GroupOne");

            await sut.InitializeAsync();

            Assert.HasCount(2, sut.GroupMembers);
            Assert.AreEqual("User One", sut.GroupMembers.First().AccountName);
            Assert.IsFalse(sut.GroupMembers.First().IsGroup);

            Assert.AreEqual("Group Two", sut.GroupMembers.Last().AccountName);
            Assert.IsTrue(sut.GroupMembers.Last().IsGroup);
        }

        [TestMethod]
        public async Task GroupMembersViewModel_export_table_generator_test()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ExportTableGenerator>();

            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            GroupMembersViewModel vm = new ModelBuilder<string, GroupMembersViewModel>(serviceProvider).Build("GroupOne");
            await vm.InitializeAsync();

            var sut = serviceProvider.GetRequiredService<ExportTableGenerator>();
            DataTable exportTable = sut.GetGroupMembersDataTable(vm);

            Assert.IsNotNull(exportTable);
            Assert.HasCount(vm.GroupMembers.Count, exportTable.Rows);
        }

        private IServiceProvider ConfigureAllServices(ServiceCollection services)
        {
            Mock<IDispatcherService> dispatcherMock = this.GetMockDispatcherService();
            Mock<IAdBrowserService> adBrowserServiceMock = this.GetMockAdBrowserService();
            services
                .UseModelBuilders()
                .UseViewModels()
                .AddSingleton(CreateMock<IConfigurationManager>())
                .AddSingleton(CreateMock<IFlyOutService>())
                .AddSingleton(CreateMock<ILogger<GroupMembersViewModel>>())
                .AddSingleton(CreateMock<IExportService>())
                .AddSingleton(dispatcherMock.Object)
                .AddSingleton(adBrowserServiceMock.Object);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private Mock<IDispatcherService> GetMockDispatcherService()
        {
            var dispatcherMock = new Mock<IDispatcherService>();
            dispatcherMock
                .Setup(m => m.BeginInvoke(It.IsAny<Action>()))
                .Callback((Action callback) => callback())
                .Returns(() => Task.CompletedTask);

            dispatcherMock
                .Setup(m => m.InvokeAsync(It.IsAny<Action>()))
                .Callback((Action callback) => callback())
                .Returns(() => Task.CompletedTask);

            return dispatcherMock;
        }

        private Mock<IAdBrowserService> GetMockAdBrowserService()
        {
            var adBrowserServiceMock = new Mock<IAdBrowserService>();

            var list = new List<AdGroupMember>
            {
                new() { DisplayName = "User One", DomainName = "test.local", IsGroup = false, SamAccountName = "User One", Ou = "OU1" },
                new() { DisplayName = "Group Two", DomainName = "test.local", IsGroup = true, SamAccountName = "Group Two", Ou = "OU2" }
            };

            adBrowserServiceMock
                .Setup(m => m.GetMembersOfGroupAsync(It.IsAny<string>(), It.IsAny<QueryType>()))
                .Returns(Task.FromResult(list.Select(m => new GroupMemberItemViewModel(m))));

            return adBrowserServiceMock;
        }

        private static T CreateMock<T>() where T : class
        {
            return new Mock<T>().Object;
        }
    }
}