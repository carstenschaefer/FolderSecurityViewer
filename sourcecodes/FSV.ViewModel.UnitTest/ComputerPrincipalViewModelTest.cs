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
    using AdServices.EnumOU;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Prism.Events;

    [TestClass]
    public class ComputerPrincipalViewModelTest
    {
        [TestMethod]
        public async Task ComputerPrincipalViewModel_initialize_test()
        {
            // Arrange
            const string domainName = "test.local";

            var adBrowserServiceMock = new Mock<IAdBrowserService>();
            IServiceProvider serviceProvider = this.ConfigureAllServices(adBrowserServiceMock.Object);

            this.SetupAdBrowserService(adBrowserServiceMock, serviceProvider);

            var adTreeViewModel = new AdTreeViewModel { DisplayName = domainName, Type = TreeViewNodeType.Domain };
            DomainViewModel domainViewModel = this.GetDomainViewModel(domainName, serviceProvider);

            ComputerPrincipalViewModel sut = new ModelBuilder<AdTreeViewModel, IPrincipalViewModel, ComputerPrincipalViewModel>(serviceProvider).Build(adTreeViewModel, domainViewModel);

            // Act
            await sut.InitializeAsync();

            // Assert
            Assert.IsNotNull(sut.Items);
            Assert.IsNotNull(sut.Parent);
            Assert.AreEqual(1, sut.Items.Count());
        }

        private IServiceProvider ConfigureAllServices(IAdBrowserService adBrowserService)
        {
            var services = new ServiceCollection();
            services
                .UseModelBuilders()
                .UseViewModels();

            Mock<IDialogService> dialogServiceMock = new();
            Mock<IDispatcherService> dispatcherServiceMock = new();
            Mock<IEventAggregator> eventAggregatorMock = new();
            Mock<ILogger<DomainViewModel>> loggerMock = new();

            services.AddSingleton(dialogServiceMock.Object);
            services.AddSingleton(dispatcherServiceMock.Object);
            services.AddSingleton(eventAggregatorMock.Object);
            services.AddSingleton(adBrowserService);
            services.AddSingleton(loggerMock.Object);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private void SetupAdBrowserService(Mock<IAdBrowserService> adBrowserServiceMock, IServiceProvider serviceProvider)
        {
            Task<IEnumerable<IPrincipalViewModel>> GetComputerPrincipals(IPrincipalViewModel parent)
            {
                ModelBuilder<AdTreeViewModel, IPrincipalViewModel, ComputerPrincipalViewModel> computerPrincipalModelBuilder = new(serviceProvider);

                ComputerPrincipalViewModel[] result =
                {
                    computerPrincipalModelBuilder.Build(new AdTreeViewModel("Computer One", "Computer One", "CompOne", TreeViewNodeType.Computer), parent)
                };

                return Task.FromResult(result.AsEnumerable<IPrincipalViewModel>());
            }

            adBrowserServiceMock
                .Setup(m => m.GetComputerPrincipalsAsync(It.IsAny<string>(), It.IsAny<IPrincipalViewModel>()))
                .Returns((string _, IPrincipalViewModel parent) => GetComputerPrincipals(parent));
        }

        private DomainViewModel GetDomainViewModel(string domainName, IServiceProvider serviceProvider)
        {
            AdTreeViewModel adTreeViewModel = new() { DisplayName = domainName, Type = TreeViewNodeType.Domain, DistinguishedName = domainName, SamAccountName = domainName };
            ModelBuilder<AdTreeViewModel, ADBrowserType, DomainViewModel> builder = new(serviceProvider);

            return builder.Build(adTreeViewModel, ADBrowserType.Computers);
        }
    }
}