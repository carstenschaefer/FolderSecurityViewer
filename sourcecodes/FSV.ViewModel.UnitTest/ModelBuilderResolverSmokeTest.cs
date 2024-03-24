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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Abstractions;
    using AdBrowser;
    using AdServices.AbstractionLayer;
    using AdServices.Abstractions;
    using Business;
    using Configuration;
    using Configuration.Abstractions;
    using Crypto;
    using Extensions.Logging.Abstractions;
    using FileSystem.Interop;
    using FolderSecurityViewer;
    using FolderTree;
    using Home;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Owner;
    using Passables;
    using Permission;
    using Setting;
    using Templates;

    [TestClass]
    public class ModelBuilderResolverSmokeTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Resolve_SplashViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);

            // Act
            var actual = serviceProvider.GetRequiredService<SplashViewModel>();

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public async Task Resolve_OwnerReportViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);


            // Act
            var resolvedParameters = new[] { new ResolverOverride(typeof(UserPath), "userPath", new UserPath("user", "path")) };
            var actual = serviceProvider.GetRequiredService<OwnerReportViewModel>(resolvedParameters);

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public async Task Resolve_TemplateContainerViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);


            // Act
            var actual = serviceProvider.GetRequiredService<TemplateContainerViewModel>();

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public async Task Resolve_PermissionItemACLDifferenceViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);

            // Act
            var resolvedParameters = new[] { new ResolverOverride(typeof(string), "folderPath", "path") };
            var actual = serviceProvider.GetRequiredService<PermissionItemACLDifferenceViewModel>(resolvedParameters);

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public async Task Resolve_ADBrowserViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);

            // Act
            var resolvedParameters = new[] { new ResolverOverride(typeof(ADBrowserType), "type", ADBrowserType.Principals) };
            var actual = serviceProvider.GetRequiredService<ADBrowserViewModel>(resolvedParameters);

            // Assert
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public async Task Resolve_SearchExclusionGroupViewModel_smoke_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            var searcherMock = new Mock<ISearcher>();
            services.AddSingleton(searcherMock.Object);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            var actual = serviceProvider.GetRequiredService<SearchExclusionGroupViewModel>();

            // Assert
            Assert.IsNotNull(actual);
        }

        private static void ConfigureAllServices(ServiceCollection services)
        {
            var configurationPathsMock = new Mock<IConfigurationPaths>();
            configurationPathsMock.SetupGet(paths => paths.LogDirectory)
                .Returns(Path.Combine(Environment.CurrentDirectory, "logs"));

            services.AddSingleton(configurationPathsMock.Object);
            services.AddSingleton(new Mock<ILoggingLevelSwitchAdapter>().Object);
            services.AddSingleton(new Mock<IExportService>().Object);
            services.AddLogging(builder => builder.ClearProviders());

            services
                .UsePlatformServices()
                .UseConfigurationServices()
                .UseSecurityServices()
                .UseActiveDirectoryAbstractionLayer()
                .UseBusiness()
                .UseViewModels()
                .UseModelBuilders();

            services.AddModule<AppModule>();
            services.AddModule<FolderTreeModule>();
            services.AddSingleton(new Mock<IDispatcherService>().Object);
        }

        [TestMethod]
        public async Task ResolveAllKnownModelTypes_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            ConfigureAllServices(services);

            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            await configurationManager.CreateDefaultConfigFileAsync(true);

            IEnumerable<Type> viewModelTypes = ViewModelsModule.GetViewModelTypes();

            // Act
            foreach (Type viewModelType in viewModelTypes)
            {
                var unsatisfiedParameters = new List<(Type, string, string)>();

                void ResolveErrorCallback(Type type, string parameterName, Exception e)
                {
                    unsatisfiedParameters.Add((type, parameterName, e.Message));
                }

                try
                {
                    this.TestContext.WriteLine("");
                    this.TestContext.WriteLine($"Resolving {viewModelType}...");
                    object instance = serviceProvider.GetRequiredService(viewModelType, ResolveErrorCallback);
                }
                catch (NullReferenceException)
                {
                    this.TestContext.WriteLine($"Resolving instance of type {viewModelType} failed due to an unsatisfied dependency that was not validated.");
                }
                catch (TargetInvocationException)
                {
                }
                catch (ArgumentException e)
                {
                    this.TestContext.WriteLine(e.Message);
                }

                if (unsatisfiedParameters.Any())
                {
                    foreach ((Type, string, string) parameter in unsatisfiedParameters)
                    {
                        this.TestContext.WriteLine($"Failed to resolve parameter ({parameter.Item2}) of type {parameter.Item1} due to an unhandled error: {parameter.Item3}.");
                    }
                }
            }

            // Assert
        }
    }
}