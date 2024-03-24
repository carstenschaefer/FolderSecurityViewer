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
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using AdBrowser;
    using Events;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Prism.Events;
    using Security.Abstractions;
    using ShareReport;
    using ShareServices.Abstractions;
    using ShareServices.Constants;
    using ShareServices.Models;

    [TestClass]
    public class ServersContainerViewModelTest
    {
        [TestMethod]
        public async Task ServersContainerViewModel_FillServers_test()
        {
            // Arrange
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            // Act
            await sut.FillServersAsync();

            SharedServerViewModel first = sut.Servers.First();

            // Assert
            Assert.AreEqual(2, sut.Servers.Count());
            Assert.AreNotEqual(SharedServerViewModel.Empty, first);
            Assert.AreEqual(ServerState.Failure.ToString(), first.State);
        }

        [TestMethod]
        public void ServersContainerViewModel_AddServerTextCommand_test()
        {
            const string newServerName = "ShareTest";
            // Arrange
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            // Act
            sut.NewServerName = newServerName;
            sut.AddServerTextCommand.Execute(null);

            // Assert
            Assert.AreEqual(1, sut.Servers.Count());
            Assert.AreNotEqual(SharedServerViewModel.Empty, sut.Servers.First());
            Assert.AreEqual(newServerName, sut.Servers.First().DisplayName);
        }

        [TestMethod]
        public void ServersContainerViewModel_AddServerWinCommand_test()
        {
            // Arrange
            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForAddServers();

            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            // Act
            sut.AddServerWinCommand.Execute(null);

            // Assert
            Assert.AreEqual(2, sut.Servers.Count());
            Assert.AreEqual("Unix", sut.Servers.First().DisplayName);
        }

        [TestMethod]
        public void ServersContainerViewModel_ScanServerCommand_test()
        {
            // Arrange
            var services = new ServiceCollection();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            // Act
            sut.ScanServerCommand.Execute(null);

            // Assert
            Assert.AreEqual(2, sut.Servers.Count());
            Assert.AreEqual("MacOS", sut.Servers.First().DisplayName);
            Assert.AreEqual("Playstation", sut.Servers.Last().DisplayName);
        }

        [TestMethod]
        public async Task ServersContainerViewModel_ClearServersCommand_test()
        {
            // Arrange
            var services = new ServiceCollection();
            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForClearServers();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            // Act
            await sut.FillServersAsync();
            sut.ClearServersCommand.Execute(null);

            // Assert
            Assert.AreEqual(1, sut.Servers.Count());
            Assert.AreEqual(SharedServerViewModel.Empty, sut.Servers.First());
        }

        [TestMethod]
        public async Task ServerContainerViewModel_AddServersEvent_test()
        {
            var services = new ServiceCollection();

            IEventAggregator eventAggregator = new EventAggregator();
            var addServersEvent = eventAggregator.GetEvent<AddServersEvent>();

            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForClearServers();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock, eventAggregator);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            await sut.FillServersAsync();
            addServersEvent.Publish(new[] { "WinShares", "NewShares" });

            Assert.AreEqual(3, sut.Servers.Count()); // WinShares already exists in collection. 
            Assert.AreEqual("NewShares", sut.Servers.First().DisplayName);
        }

        [TestMethod]
        public async Task ServerContainerViewModel_RemoveServerEvent_test()
        {
            var services = new ServiceCollection();

            IEventAggregator eventAggregator = new EventAggregator();
            var removeServerEvent = eventAggregator.GetEvent<RemoveServerEvent>();

            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForClearServers();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock, eventAggregator);

            ServersContainerViewModel sut = new ModelBuilder<ServersContainerViewModel>(serviceProvider).Build();

            await sut.FillServersAsync();
            SharedServerViewModel viewModel = sut.Servers.First();

            removeServerEvent.Publish(viewModel);

            Assert.AreEqual(1, sut.Servers.Count());
            Assert.AreNotEqual(SharedServerViewModel.Empty, sut.Servers.First());
        }

        private IServiceProvider ConfigureAllServices(ServiceCollection services)
        {
            return this.ConfigureAllServices(services, new Mock<IDialogService>());
        }

        private IServiceProvider ConfigureAllServices(ServiceCollection services, Mock<IDialogService> dialogServiceMock, IEventAggregator eventAggregator = null)
        {
            IEventAggregator localEventAggregator = eventAggregator ?? new EventAggregator();

            var navigationServiceMock = new Mock<INavigationService>();
            var exportServiceMock = new Mock<IExportService>();
            var loggerMock = new Mock<ILogger<ServersContainerViewModel>>();

            Mock<IShareScannerFactory> shareScannerFactoryMock = this.GetMockShareScannerFactory();
            Mock<IShareServersManager> shareServersManagerMock = this.GetShareServersManagerMock();
            Mock<IDispatcherService> dispatcherMock = this.GetMockDispatcherService();
            Mock<ISecurityContext> securityContextMock = this.GetSecurityContextMock();

            services
                .UseModelBuilders()
                .UseViewModels()
                .AddSingleton(dialogServiceMock.Object)
                .AddSingleton(navigationServiceMock.Object)
                .AddSingleton(dispatcherMock.Object)
                .AddSingleton(shareServersManagerMock.Object)
                .AddSingleton(shareScannerFactoryMock.Object)
                .AddSingleton(securityContextMock.Object)
                .AddSingleton(loggerMock.Object)
                .AddSingleton(exportServiceMock.Object)
                .AddSingleton(localEventAggregator);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private Mock<ISecurityContext> GetSecurityContextMock()
        {
            var securityContextMock = new Mock<ISecurityContext>();
            securityContextMock.Setup(m => m.RunAsync(It.IsAny<Func<object, Task>>()))
                .Callback((Func<object, Task> func) => func(null))
                .Returns(() => Task.CompletedTask);

            return securityContextMock;
        }

        private Mock<IShareServersManager> GetShareServersManagerMock()
        {
            var mockShareServersManager = new Mock<IShareServersManager>();
            mockShareServersManager.Setup(m => m.GetServerListAsync())
                .Returns(() => Task.FromResult(this.CreateServerList()));

            mockShareServersManager.Setup(m => m.CreateServer(It.IsAny<string>()))
                .Returns((string name) => new ServerItem(name, ServerState.Scanned));

            mockShareServersManager.Setup(m => m.ServerExists(It.IsAny<string>()))
                .Returns((string name) => string.Compare("WinShares", name, StringComparison.InvariantCultureIgnoreCase) == 0);

            return mockShareServersManager;
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

        private Mock<IDialogService> GetMockDialogServiceForAddServers()
        {
            var dialogServiceMock = new Mock<IDialogService>();

            dialogServiceMock.Setup(m => m.ShowDialog(It.IsAny<WorkspaceViewModel>()))
                .Callback<WorkspaceViewModel>(m =>
                {
                    if (m is AddServersViewModel vm)
                    {
                        vm.NewServerNames = "Windows, Unix";
                    }

                    m.OKCommand.Execute(null);
                })
                .Returns(() => true);

            return dialogServiceMock;
        }

        private Mock<IDialogService> GetMockDialogServiceForClearServers()
        {
            var dialogServiceMock = new Mock<IDialogService>();

            dialogServiceMock
                .Setup(m => m.ShowDialog(It.IsAny<WorkspaceViewModel>()))
                .Returns(() => true);

            return dialogServiceMock;
        }

        private Mock<IShareScannerFactory> GetMockShareScannerFactory()
        {
            var mockShareScannerFactory = new Mock<IShareScannerFactory>();
            Mock<IShareScanner> mockShareScanner = this.GetMockShareScanner();
            Mock<IShareServerScanner> mockShareServerScanner = this.GetMockShareServerScanner();

            mockShareScannerFactory.Setup(m => m.CreateShareScanner())
                .Returns(() => mockShareScanner.Object);
            mockShareScannerFactory.Setup(m => m.CreateServerScanner())
                .Returns(() => mockShareServerScanner.Object);

            return mockShareScannerFactory;
        }

        private Mock<IShareScanner> GetMockShareScanner()
        {
            Share GetShare(string server, string name)
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

            IList<Share> GetSharesOfServer(string server)
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

            var mockShareScanner = new Mock<IShareScanner>();
            mockShareScanner.Setup(m => m.GetShare(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string server, string name) => GetShare(server, name));

            mockShareScanner.Setup(m => m.GetShareAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string server, string name) => Task.FromResult(GetShare(server, name)));

            mockShareScanner.Setup(m => m.GetSharesOfServerAsync(It.IsAny<string>()))
                .Returns((string server) => Task.FromResult(GetSharesOfServer(server)));

            return mockShareScanner;
        }

        private Mock<IShareServerScanner> GetMockShareServerScanner()
        {
            var mockShareServerScanner = new Mock<IShareServerScanner>();

            mockShareServerScanner.Setup(m => m.GetServersAsync())
                .ReturnsAsync(() =>
                {
                    var result = new List<Server>(2)
                    {
                        new() { Name = "Playstation" },
                        new() { Name = "MacOS" }
                    };

                    return result.AsEnumerable();
                });

            return mockShareServerScanner;
        }

        private IEnumerable<ServerItem> CreateServerList()
        {
            var xServers = new List<ServerItem>();

            var xShareOne = new ShareItem("WinShares", @"WinShares");
            var xShareTwo = new ShareItem("WebShares", @"Web\wwwroot");

            var xServerWindows = new ServerItem("Windows", ServerState.Scanned, new List<ShareItem>(2) { xShareOne, xShareTwo });
            var xServerUbuntu = new ServerItem("Ubuntu", ServerState.Failure);

            xServers.Add(xServerWindows);
            xServers.Add(xServerUbuntu);

            return xServers;
        }
    }
}