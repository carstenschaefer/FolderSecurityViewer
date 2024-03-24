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
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    public class ModelBuilderTest
    {
        [TestMethod]
        public void ModelBuilder_delegate_invoke_injects_required_services_and_data_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            services.UseModelBuilders();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var builder = serviceProvider.GetRequiredService<ModelBuilder<DataModel, ViewModelWithServicesAndData>>();

            var expectedDataModel = new DataModel();

            // Act
            ViewModelWithServicesAndData actual = builder.Build(expectedDataModel);

            // Assert
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.ServiceProvider);
            Assert.AreEqual(expectedDataModel, actual.Model);
        }

        private class ViewModelWithServicesAndData : ViewModelBase
        {
            public ViewModelWithServicesAndData(IServiceProvider serviceProvider, DataModel model)
            {
                this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                this.Model = model ?? throw new ArgumentNullException(nameof(model));
            }

            public IServiceProvider ServiceProvider { get; }

            public DataModel Model { get; }
        }

        private class DataModel
        {
        }
    }
}