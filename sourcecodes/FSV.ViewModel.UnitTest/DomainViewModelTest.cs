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
    using Moq;
    using Xunit;

    public class DomainViewModelTest
    {
        [Fact]
        public async void DomainViewModel_Initialize_for_computers_test()
        {
            const string domainName = "test.local";
            // Arrange  
            var adBrowserServiceMock = new Mock<IAdBrowserService>();
            IServiceProvider serviceProvider = this.ConfigureAllServices(adBrowserServiceMock.Object);

            this.SetupAdBrowserService(adBrowserServiceMock, serviceProvider);

            // Act
            DomainViewModel sut = this.GetDomainViewModel(domainName, ADBrowserType.Computers, serviceProvider);
            await sut.InitializeAsync();

            // Assert
            Assert.NotNull(sut.Items);
            Assert.Null(sut.Parent);

            Assert.All(sut.Items,
                item =>
                {
                    Assert.IsType<ComputerPrincipalViewModel>(item);
                    Assert.NotNull(item.Parent);
                });
        }

        [Fact]
        public async void DomainViewModel_Initialize_for_principals_test()
        {
            const string domainName = "test.local";
            // Arrange  
            var adBrowserServiceMock = new Mock<IAdBrowserService>();
            IServiceProvider serviceProvider = this.ConfigureAllServices(adBrowserServiceMock.Object);

            this.SetupAdBrowserService(adBrowserServiceMock, serviceProvider);

            // Act
            DomainViewModel sut = this.GetDomainViewModel(domainName, ADBrowserType.Principals, serviceProvider);
            await sut.InitializeAsync();

            // Assert
            Assert.NotNull(sut.Items);
            Assert.Null(sut.Parent);

            Assert.All(sut.Items,
                item =>
                {
                    Assert.IsType<PrincipalViewModel>(item);
                    Assert.NotNull(item.Parent);
                });
        }

        private IServiceProvider ConfigureAllServices(IAdBrowserService adBrowserService)
        {
            var services = new ServiceCollection();
            services
                .UseModelBuilders()
                .UseViewModels();

            Mock<IDialogService> dialogServiceMock = new();
            Mock<ILogger<DomainViewModel>> loggerMock = new();

            services.AddSingleton(dialogServiceMock.Object)
                .AddSingleton(loggerMock.Object)
                .AddSingleton(adBrowserService);


            IServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        private DomainViewModel GetDomainViewModel(string domainName, ADBrowserType adBrowserType, IServiceProvider serviceProvider)
        {
            AdTreeViewModel adTreeViewModel = new() { DisplayName = domainName, Type = TreeViewNodeType.Domain, DistinguishedName = domainName, SamAccountName = domainName };
            ModelBuilder<AdTreeViewModel, ADBrowserType, DomainViewModel> builder = new(serviceProvider);

            return builder.Build(adTreeViewModel, adBrowserType);
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

            adBrowserServiceMock
                .Setup(m => m.GetPrincipalsAsync(It.IsAny<string>(), It.IsAny<IPrincipalViewModel>()))
                .Returns((string _, IPrincipalViewModel parent) => GetPrincipals(parent));
        }
    }
}