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
    using AdServices.Abstractions;
    using AdServices.EnumOU;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Prism.Events;

    [TestClass]
    public class PrincipalViewModelTest
    {
        [TestMethod]
        public async Task PrincipalViewModel_initialize_test()
        {
            // Arrange
            const string domainName = "test.local";

            var adBrowserServiceMock = new Mock<IAdBrowserService>();
            IServiceProvider serviceProvider = this.ConfigureAllServices(adBrowserServiceMock.Object);

            this.SetupAdBrowserService(adBrowserServiceMock, serviceProvider);

            var adTreeViewModel = new AdTreeViewModel { DisplayName = domainName, Type = TreeViewNodeType.Domain };
            DomainViewModel domainViewModel = this.GetDomainViewModel(domainName, serviceProvider);

            PrincipalViewModel sut = new ModelBuilder<AdTreeViewModel, IPrincipalViewModel, PrincipalViewModel>(serviceProvider).Build(adTreeViewModel, domainViewModel);

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

            var dialogServiceMock = new Mock<IDialogService>();
            var eventAggregatorMock = new Mock<IEventAggregator>();
            var navigationServiceMock = new Mock<INavigationService>();
            var flyoutServiceMock = new Mock<IFlyOutService>();
            var domainInformationServiceMock = new Mock<IDomainInformationService>();
            var loggerMock = new Mock<ILogger<DomainViewModel>>();

            services.AddSingleton(dialogServiceMock.Object);
            services.AddSingleton(flyoutServiceMock.Object);
            services.AddSingleton(eventAggregatorMock.Object);
            services.AddSingleton(navigationServiceMock.Object);
            services.AddSingleton(adBrowserService);
            services.AddSingleton(domainInformationServiceMock.Object);
            services.AddSingleton(loggerMock.Object);

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private void SetupAdBrowserService(Mock<IAdBrowserService> adBrowserServiceMock, IServiceProvider serviceProvider)
        {
            Task<IEnumerable<IPrincipalViewModel>> GetPrincipals(IPrincipalViewModel parent)
            {
                var principalModelBuilder = new ModelBuilder<AdTreeViewModel, IPrincipalViewModel, PrincipalViewModel>(serviceProvider);

                PrincipalViewModel[] result =
                {
                    principalModelBuilder.Build(new AdTreeViewModel("OU One", "OU One", "OUOne", TreeViewNodeType.OU), parent)
                };

                return Task.FromResult(result.AsEnumerable<IPrincipalViewModel>());
            }

            adBrowserServiceMock
                .Setup(m => m.GetPrincipalsAsync(It.IsAny<string>(), It.IsAny<IPrincipalViewModel>()))
                .Returns((string _, IPrincipalViewModel parent) => GetPrincipals(parent));
        }

        private DomainViewModel GetDomainViewModel(string domainName, IServiceProvider serviceProvider)
        {
            AdTreeViewModel adTreeViewModel = new() { DisplayName = domainName, Type = TreeViewNodeType.Domain, DistinguishedName = domainName, SamAccountName = domainName };
            ModelBuilder<AdTreeViewModel, ADBrowserType, DomainViewModel> builder = new(serviceProvider);

            return builder.Build(adTreeViewModel, ADBrowserType.Computers);
        }
    }
}