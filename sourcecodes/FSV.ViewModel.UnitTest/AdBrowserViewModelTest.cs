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
    public class AdBrowserViewModelTest
    {
        [TestMethod]
        public async Task AdBrowserViewModel_SearchPrincipals_no_result_test()
        {
            const string principalName = "user_1202";

            // Arrange
            Mock<IAdBrowserService> mockAdBrowserService = this.GetAdBrowserServiceForNoResults();

            IServiceProvider serviceProvider = this.ConfigureAllServices(mockAdBrowserService);

            ADBrowserViewModel sut = new ModelBuilder<ADBrowserType, ADBrowserViewModel>(serviceProvider).Build(ADBrowserType.Principals);
            sut.PrincipalName = principalName;

            // Act
            await sut.SearchPrincipalCommand.ExecuteAsync(null);

            // Assert
            Assert.AreEqual(principalName, sut.PrincipalName);
            Assert.IsEmpty(sut.UserPrincipals);
            Assert.IsFalse(sut.ShowUserList);
            Assert.IsFalse(sut.CanReport);
        }

        [TestMethod]
        public async Task AdBrowserViewModel_SearchPrincipals_single_result_test()
        {
            const string principalName = "user_1202";

            // Arrange
            Mock<IAdBrowserService> mockAdBrowserService = this.GetAdBrowserServiceForSingleResult();

            IServiceProvider serviceProvider = this.ConfigureAllServices(mockAdBrowserService);

            ADBrowserViewModel sut = new ModelBuilder<ADBrowserType, ADBrowserViewModel>(serviceProvider).Build(ADBrowserType.Principals);
            sut.PrincipalName = principalName;

            // Act
            await sut.SearchPrincipalCommand.ExecuteAsync(null);

            // Assert
            Assert.AreEqual(principalName, sut.PrincipalName);
            Assert.IsFalse(sut.ShowUserList);
            Assert.IsTrue(sut.CanReport);

            foreach (AdTreeViewModel item in sut.UserPrincipals)
            {
                if (item is null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                Assert.AreEqual(principalName, item.SamAccountName);
                Assert.AreEqual(TreeViewNodeType.User, item.Type);
            }
        }

        [TestMethod]
        public async Task AdBrowserViewModel_SearchPrincipals_collection_result_test()
        {
            const string principalName = "*12*";

            // Arrange
            Mock<IAdBrowserService> mockAdBrowserService = this.GetAdBrowserServiceForMultipleResults();

            IServiceProvider serviceProvider = this.ConfigureAllServices(mockAdBrowserService);

            ADBrowserViewModel sut = new ModelBuilder<ADBrowserType, ADBrowserViewModel>(serviceProvider).Build(ADBrowserType.Principals);
            sut.PrincipalName = principalName;

            // Act
            await sut.SearchPrincipalCommand.ExecuteAsync(null);

            // Assert
            Assert.IsTrue(sut.ShowUserList);
            foreach (AdTreeViewModel item in sut.UserPrincipals)
            {
                if (item is null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                switch (item.Type)
                {
                    case TreeViewNodeType.User:
                        Assert.AreEqual("user_display", item.DistinguishedName);
                        break;
                    case TreeViewNodeType.Group:
                        Assert.AreEqual("group_display", item.DistinguishedName);
                        break;
                    default:
                        Assert.Fail("unexpected item type");
                        break;
                }
            }
        }

        [TestMethod]
        public async Task AdBrowserViewModel_selection_test()
        {
            const string principalName = "user1203";

            // Arrange
            Mock<IAdBrowserService> mockAdBrowserService = new();

            IServiceProvider serviceProvider = this.ConfigureAllServices(mockAdBrowserService);

            this.SetupAdBrowserServiceForSelection(mockAdBrowserService, serviceProvider);

            ADBrowserViewModel sut = new ModelBuilder<ADBrowserType, ADBrowserViewModel>(serviceProvider).Build(ADBrowserType.Principals);

            await sut.InitializeAsync();

            sut.PrincipalName = principalName;

            // Act
            await sut.SearchPrincipalCommand.ExecuteAsync(null);

            IPrincipalViewModel selectedItem = GetSelectedPrincipal(sut.Principals);

            // Assert
            foreach (IPrincipalViewModel item in sut.Principals)
            {
                Assert.AreEqual("the_domain.test", item.DisplayName);
            }

            Assert.IsNotNull(selectedItem);
            Assert.AreEqual(principalName, selectedItem.SamAccountName);
        }

        private IServiceProvider ConfigureAllServices(Mock<IAdBrowserService> mockAdBrowserService)
        {
            var services = new ServiceCollection();
            services
                .UseModelBuilders()
                .UseViewModels();

            Mock<IDialogService> dialogServiceMock = new();
            Mock<IFlyOutService> flyOutServiceMock = new();
            Mock<IDispatcherService> dispatcherServiceMock = new();
            Mock<IEventAggregator> eventAggregatorMock = new();
            Mock<ICurrentDomainCheckUtility> currentDomainCheckUtility = this.GetCurrentDomainCheckUtility();
            Mock<ILogger<ADBrowserViewModel>> adBrowserLogger = new();
            Mock<ILogger<DomainViewModel>> domainLogger = new();
            Mock<IDomainInformationService> domainInformationServiceMock = new();

            services.AddSingleton(dialogServiceMock.Object)
                .AddSingleton(dispatcherServiceMock.Object)
                .AddSingleton(eventAggregatorMock.Object)
                .AddSingleton(mockAdBrowserService.Object)
                .AddSingleton(currentDomainCheckUtility.Object)
                .AddSingleton(adBrowserLogger.Object)
                .AddSingleton(domainLogger.Object)
                .AddSingleton(flyOutServiceMock.Object)
                .AddSingleton(domainInformationServiceMock.Object);

            return services.BuildServiceProvider();
        }

        private Mock<ICurrentDomainCheckUtility> GetCurrentDomainCheckUtility()
        {
            const string domain = "the_domain.test";
            Mock<ICurrentDomainCheckUtility> currentDomainCheckUtility = new();

            currentDomainCheckUtility
                .Setup(m => m.IsComputerJoinedAndConnectedToDomain())
                .Returns(true);

            currentDomainCheckUtility
                .Setup(m => m.GetComputerDomainName())
                .Returns(domain);

            currentDomainCheckUtility
                .Setup(m => m.GetAllDomainsOfForest())
                .Returns(new[] { domain });

            return currentDomainCheckUtility;
        }

        private Mock<IAdBrowserService> GetAdBrowserServiceForNoResults()
        {
            return this.GetAdBrowserService(Array.Empty<AdTreeViewModel>());
        }

        private Mock<IAdBrowserService> GetAdBrowserServiceForSingleResult()
        {
            var result = new[]
            {
                new AdTreeViewModel("User Display", "user_display", "user_1202", TreeViewNodeType.User)
            };

            return this.GetAdBrowserService(result);
        }

        private Mock<IAdBrowserService> GetAdBrowserServiceForMultipleResults()
        {
            var result = new[]
            {
                new AdTreeViewModel("User Display", "user_display", "user_1202", TreeViewNodeType.User),
                new AdTreeViewModel("Group Display", "group_display", "group_1203", TreeViewNodeType.Group)
            };

            return this.GetAdBrowserService(result);
        }

        private Mock<IAdBrowserService> GetAdBrowserService(IList<AdTreeViewModel> result)
        {
            Mock<IAdBrowserService> adBrowserServiceMock = new();

            Task<IEnumerable<AdTreeViewModel>> GetUserPrincipals()
            {
                return Task.FromResult(result.AsEnumerable());
            }

            adBrowserServiceMock
                .Setup(m => m.FindUsersAndGroupsAsync(It.IsAny<string>()))
                .Returns(GetUserPrincipals);

            return adBrowserServiceMock;
        }

        private void SetupAdBrowserServiceForSelection(Mock<IAdBrowserService> adBrowserServiceMock, IServiceProvider serviceProvider)
        {
            var adTreeModels = new List<AdTreeViewModel>
            {
                new("First User", "CN=user1203,OU=testou,DC=the_domain,DC=test", "user1203", TreeViewNodeType.User),
                new("Test Group", "CN=test_group,OU=testou,DC=the_domain,DC=test", "test_group", TreeViewNodeType.Group),
                new("Second User", "CN=user1205,OU=testou,DC=the_domain,DC=test", "user1205", TreeViewNodeType.User)
            };

            var principalModelBuilder = new ModelBuilder<AdTreeViewModel, IPrincipalViewModel, PrincipalViewModel>(serviceProvider);

            IEnumerable<IPrincipalViewModel> GetDomainChildren(IPrincipalViewModel parent)
            {
                var domainChildren = new IPrincipalViewModel[]
                {
                    principalModelBuilder.Build(new AdTreeViewModel("TestOU", "OU=testou,DC=the_domain,DC=test", "test_ou", TreeViewNodeType.OU), parent),
                    principalModelBuilder.Build(new AdTreeViewModel("SecondOU", "OU=secondou,DC=the_domain,DC=test", "second_ou", TreeViewNodeType.OU), parent)
                };

                return domainChildren;
            }

            IEnumerable<IPrincipalViewModel> GetOuChildren(IPrincipalViewModel parent)
            {
                return adTreeModels.Select(m => principalModelBuilder.Build(m, parent));
            }

            adBrowserServiceMock
                .Setup(m => m.GetPrincipalsAsync(It.IsAny<string>(), It.IsAny<IPrincipalViewModel>()))
                .Returns((string _, IPrincipalViewModel parent) => Task.FromResult(parent.Type == TreeViewNodeType.Domain.ToString() ? GetDomainChildren(parent) : GetOuChildren(parent)));

            adBrowserServiceMock
                .Setup(m => m.FindUsersAndGroupsAsync(It.IsAny<string>()))
                .Returns((string text) => { return Task.FromResult(adTreeModels.Where(m => m.SamAccountName.Contains(text)).AsEnumerable()); });
        }

        private static IPrincipalViewModel GetSelectedPrincipal(IEnumerable<IPrincipalViewModel> principals)
        {
            foreach (IPrincipalViewModel item in principals)
            {
                return item.Selected ? item : GetSelectedPrincipal(item.Items);
            }

            return null;
        }
    }
}