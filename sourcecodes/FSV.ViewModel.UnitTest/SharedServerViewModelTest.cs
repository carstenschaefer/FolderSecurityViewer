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
    using System.Threading.Tasks;
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Prism.Events;
    using ShareReport;
    using ShareServices.Abstractions;
    using ShareServices.Constants;
    using ShareServices.Models;

    [TestClass]
    public class SharedServerViewModelTest
    {
        [TestMethod]
        public void SharedServerViewModel_StartScan_test()
        {
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            ServerItem xServer = this.GetServer();
            SharedServerViewModel sut = new ModelBuilder<ServerItem, bool, SharedServerViewModel>(serviceProvider).Build(xServer, true);

            sut.ListSharesCommand.Execute(null);

            Assert.HasCount(2, sut.Shares);
        }

        private ServerItem GetServer()
        {
            return new ServerItem("Windows", ServerState.Scanned, new List<ShareItem>());
        }

        private IServiceProvider ConfigureAllServices(ServiceCollection services)
        {
            IEventAggregator eventAggregator = new EventAggregator();

            var dialogServiceMock = new Mock<IDialogService>();
            var navigationServiceMock = new Mock<INavigationService>();
            var exportServiceMock = new Mock<IExportService>();
            Mock<IShareScannerFactory> shareScannerFactoryMock = this.GetMockShareScannerFactory();
            Mock<IShareServersManager> shareServersManagerMock = this.GetShareServersManagerMock();
            Mock<IDispatcherService> dispatcherMock = this.GetMockDispatcherService();

            services
                .UseModelBuilders()
                .UseViewModels()
                .AddSingleton(dialogServiceMock.Object)
                .AddSingleton(navigationServiceMock.Object)
                .AddSingleton(dispatcherMock.Object)
                .AddSingleton(shareServersManagerMock.Object)
                .AddSingleton(shareScannerFactoryMock.Object)
                .AddSingleton(exportServiceMock.Object)
                .AddSingleton(eventAggregator);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private Mock<IShareScannerFactory> GetMockShareScannerFactory()
        {
            var mockShareScannerFactory = new Mock<IShareScannerFactory>();
            Mock<IShareScanner> mockShareScanner = this.GetMockShareScanner();

            mockShareScannerFactory.Setup(m => m.CreateShareScanner())
                .Returns(() => mockShareScanner.Object);

            return mockShareScannerFactory;
        }

        private Mock<IShareScanner> GetMockShareScanner()
        {
            static Share GetShare(string name)
            {
                return new Share
                {
                    Name = name,
                    Path = @"Some\Test\Path",
                    ClientConnections = 0,
                    Description = "Some test description",
                    MaxUsers = 10,
                    Trustees = new List<ShareTrustee>(0)
                };
            }

            var mockShareScanner = new Mock<IShareScanner>();
            mockShareScanner.Setup(m => m.GetShare(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string name) => GetShare(name));

            mockShareScanner.Setup(m => m.GetShareAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string name) => Task.FromResult(GetShare(name)));

            mockShareScanner.Setup(m => m.GetSharesOfServerAsync(It.IsAny<string>()))
                .Returns((string _) => Task.FromResult(GetSharesOfServer()));

            return mockShareScanner;

            static IList<Share> GetSharesOfServer()
            {
                return new List<Share>
                {
                    new()
                    {
                        Name = "Test One",
                        Path = @"Some\Test\Path",
                        ClientConnections = 0,
                        Description = "Some test description",
                        MaxUsers = 10,
                        Trustees = new List<ShareTrustee>(0)
                    },
                    new()
                    {
                        Name = "Test Two",
                        Path = @"Some\Other\Test\Path",
                        ClientConnections = 2,
                        Description = "Some new test description",
                        MaxUsers = 5,
                        Trustees = new List<ShareTrustee>(0)
                    }
                };
            }
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

        private Mock<IShareServersManager> GetShareServersManagerMock()
        {
            var mockShareServersManager = new Mock<IShareServersManager>();

            mockShareServersManager
                .Setup(m => m.CreateShare(It.IsAny<ServerItem>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((ServerItem _, string name, string path) => new ShareItem(name, path));

            return mockShareServersManager;
        }
    }
}