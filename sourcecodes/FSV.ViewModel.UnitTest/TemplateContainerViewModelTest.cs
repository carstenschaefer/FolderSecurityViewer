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
    using System.Xml.Linq;
    using Abstractions;
    using FSV.Templates.Abstractions;
    using Managers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Prism.Events;
    using Templates;

    [TestClass]
    public class TemplateContainerViewModelTest
    {
        private const string TestTemplateName = "Test Template Name";

        [TestMethod]
        public async Task TemplateContainerViewModel_Add_test()
        {
            var services = new ServiceCollection();

            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForAddTemplate();

            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock);

            TemplateContainerViewModel sut = new ModelBuilder<TemplateContainerViewModel>(serviceProvider).Build();
            await sut.InitializeAsync();

            _ = sut.ShowTemplateEditor(TemplateType.PermissionReport, "some/test/path");

            Assert.AreEqual(3, sut.Templates.Count);
            Assert.AreEqual(TestTemplateName, sut.Templates.Last().Name);
        }

        [TestMethod]
        public async Task TemplateContainerViewModel_Remove_test()
        {
            var services = new ServiceCollection();
            Mock<IDialogService> dialogServiceMock = this.GetMockDialogServiceForRemoveTemplate();
            IServiceProvider serviceProvider = this.ConfigureAllServices(services, dialogServiceMock);

            TemplateContainerViewModel sut = new ModelBuilder<TemplateContainerViewModel>(serviceProvider).Build();
            await sut.InitializeAsync();
            TemplateItemViewModel templateItem = sut.Templates.Last();
            templateItem.Selected = true;

            sut.RemoveTemplateCommand.Execute(null);

            Assert.AreEqual(1, sut.Templates.Count);
            Assert.AreEqual("Three One", sut.Templates.First().Name);
        }

        private IServiceProvider ConfigureAllServices(ServiceCollection services, Mock<IDialogService> dialogServiceMock)
        {
            IEventAggregator eventAggregator = new EventAggregator();
            Mock<IDispatcherService> dispatcherMock = this.GetMockDispatcherService();
            Mock<ITemplateFile> templateFileMock = this.GetMockTemplateFile();

            services
                .UseModelBuilders()
                .UseViewModels()
                .AddSingleton(CreateMock<ILogger<TemplateContainerViewModel>>())
                .AddSingleton(CreateMock<INavigationService>())
                .AddSingleton(dialogServiceMock.Object)
                .AddSingleton(dispatcherMock.Object)
                .AddSingleton(templateFileMock.Object)
                .AddSingleton(eventAggregator)
                .AddSingleton<TemplateManager>();

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

        private Mock<ITemplateFile> GetMockTemplateFile()
        {
            const string xml = @"
                <Templates>
                  <Template id='207666ed-6772-4818-ab0f-eb2d8ba85df0' name='Three One' type='2' user='Test\clarkkent'>Shares\Path</Template>
                  <Template id='c29f8555-26d0-4d5d-ac9a-b0bf92a993bd' name='Testing Owner Report' user='TEST\brucebanner' type='2'>Shares\Path</Template>
                </Templates>";

            XDocument document = XDocument.Parse(xml);
            List<Template> templateList = document.Root.Elements().Select(el => new Template(el)).ToList();

            var mock = new Mock<ITemplateFile>();
            mock.SetupGet(m => m.Templates)
                .Returns(() => new TemplateCollection(document.Root, templateList));
            mock.Setup(m => m.CreateTemplate())
                .Returns(() => new Template());

            return mock;
        }

        private Mock<IDialogService> GetMockDialogServiceForAddTemplate()
        {
            var dialogServiceMock = new Mock<IDialogService>();

            dialogServiceMock.Setup(m => m.ShowDialog(It.IsAny<WorkspaceViewModel>()))
                .Callback<WorkspaceViewModel>(m =>
                {
                    if (m is TemplateNewViewModel vm)
                    {
                        vm.Name = TestTemplateName;
                    }

                    m.OKCommand.Execute(null);
                })
                .Returns(() => true);

            return dialogServiceMock;
        }

        private Mock<IDialogService> GetMockDialogServiceForRemoveTemplate()
        {
            var dialogServiceMock = new Mock<IDialogService>();

            dialogServiceMock.Setup(m => m.Ask(It.IsAny<string>()))
                .Returns(() => true);

            return dialogServiceMock;
        }

        private static T CreateMock<T>() where T : class
        {
            return new Mock<T>().Object;
        }
    }
}