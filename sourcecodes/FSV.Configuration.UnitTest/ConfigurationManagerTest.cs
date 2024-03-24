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

namespace FSV.Configuration.UnitTest
{
    using Abstractions;
    using FSV.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ConfigurationManagerTest
    {
        [TestMethod]
        public void ConfigurationManager_ctor_sets_log_directory_and_file_Test()
        {
            // Arrange
            var configurationPaths = new ConfigurationPaths();
            configurationPaths.CreateApplicationDirectories();

            var logLevelSwitchAdapterMock = new Mock<ILoggingLevelSwitchAdapter>();

            // Act
            var sut = new ConfigurationManager(configurationPaths, logLevelSwitchAdapterMock.Object);

            // Assert
            Assert.IsNotNull(sut.LogDirectory);
            Assert.IsNotNull(sut.LogFile);
        }

        [TestMethod]
        public void ConfigurationManager_ctor_throws_if_the_given_ConfigurationPath_object_is_not_initialized_Test()
        {
            // Arrange
            var configurationPaths = new ConfigurationPaths();
            var logLevelSwitchAdapterMock = new Mock<ILoggingLevelSwitchAdapter>();

            // Act
            void Act()
            {
                var sut = new ConfigurationManager(configurationPaths, logLevelSwitchAdapterMock.Object);
            }

            // Assert
            Assert.ThrowsException<ConfigurationException>(Act);
        }
    }
}