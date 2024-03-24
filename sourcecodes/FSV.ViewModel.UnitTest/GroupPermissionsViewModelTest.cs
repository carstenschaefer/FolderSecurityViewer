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
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Permission;

    [TestClass]
    public class GroupPermissionsViewModelTest
    {
        [TestMethod]
        public async Task GroupPermissionsViewModel_paged_permissions_test()
        {
            // Arrange
            const string path = @"some\test\path";
            IServiceProvider serviceProvider = GetServiceProvider();

            DataTable permissions = GetMockDataTable();

            GroupPermissionsViewModel sut = new ModelBuilder<DataTable, string, GroupPermissionsViewModel>(serviceProvider)
                .Build(permissions, path);

            // Act
            await sut.InitializeAsync();

            // Assert
            Assert.AreSame(permissions, sut.AllPermissions);
            Assert.IsNotNull(sut.PagedPermissions);
            Assert.AreEqual(20, sut.PagedPermissions.Rows.Count);
        }

        private static DataTable GetMockDataTable()
        {
            using MemoryStream xmlStream = GetXmlResource();
            if (xmlStream is null)
            {
                return new DataTable();
            }

            var set = new DataSet();
            set.ReadXml(xmlStream);

            xmlStream.Close();

            return set.Tables[0];
        }

        private static IServiceProvider GetServiceProvider()
        {
            ServiceCollection serviceCollection = new();

            IConfigurationManager configurationManager = new Mock<IConfigurationManager>().Object;
            IExportService exportService = new Mock<IExportService>().Object;

            serviceCollection
                .UseModelBuilders()
                .AddSingleton(configurationManager)
                .AddSingleton(exportService)
                .AddSingleton(GetMockDispatcherService().Object);

            return serviceCollection.BuildServiceProvider();
        }

        private static MemoryStream GetXmlResource()
        {
            string namespaceName = typeof(GroupPermissionsViewModelTest).Namespace + ".Xml.";
            var assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(namespaceName + "GroupPermissions.xml");

            if (stream is null)
            {
                return null;
            }

            var bytes = new byte[stream.Length];

            stream.Read(bytes, 0, bytes.Length);
            stream.Close();

            var memoryStream = new MemoryStream();
            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        private static Mock<IDispatcherService> GetMockDispatcherService()
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
    }
}