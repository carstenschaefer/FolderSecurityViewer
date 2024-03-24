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

namespace FSV.Extensions.WindowConfiguration.Test
{
    using System.IO;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Serialization.Abstractions;
    using Xunit;
    
    public class WindowConfigurationManagerTest
    {
        [Fact]
        public void WindowConfigurationModule_Load_Test()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.UseWindowConfigurationServices();

            // Act
        }

        [Fact]
        public void WindowConfigurationManager_Initialize_GetPosition_returns_expected_position_if_file_is_found_Test()
        {
            // Arrange
            const WindowState windowState = WindowState.Maximized;
            var expectedPosition = new Position(500, 400, 0, 0, windowState);
            var settings = new Settings(expectedPosition);
            
            const string path = "Window.json";

            IMockFileDataAccessor mockFileDataAccessor = new MockFileSystem();
            using var stream = new MockFileStream(mockFileDataAccessor, path, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            var serializerMock = new Mock<ISerializationWrapper>();
            serializerMock.Setup(wrapper => wrapper.Deserialize<Settings>(stream)).Returns(settings).Verifiable();

            ISerializationWrapper SerializerFactory(SerializerType type)
            {
                return serializerMock.Object;
            }
            
            var fileMock = new Mock<IFile>();
            fileMock.Setup(file => file.Exists(path)).Returns(true).Verifiable();
            fileMock.Setup(file => file.Open(path, FileMode.Open)).Returns(stream).Verifiable();

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupGet(system => system.File).Returns(fileMock.Object);

            Mock<IConfigurationPaths> configurationPathsMock = GetConfigurationPathsMock();

            var sut = new WindowConfigurationManager(
                configurationPathsMock.Object,
                SerializerFactory,
                new NullLogger<IWindowConfigurationManager>(),
                fileSystemMock.Object);

            // Act
            sut.Initialize();
            Position actual = sut.GetPosition();

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expectedPosition, actual);

            fileMock.Verify(file => file.Exists(path), Times.Once);
            fileMock.Verify(file => file.Open(path, FileMode.Open), Times.Once);
            serializerMock.Verify(wrapper => wrapper.Deserialize<Settings>(stream), Times.Once);
        }

        [Fact]
        public void WindowConfigurationManager_Initialize_throws_if_file_is_found_Test()
        {
            // Arrange
            var serializerMock = new Mock<ISerializationWrapper>();

            ISerializationWrapper SerializerFactory(SerializerType type)
            {
                return serializerMock.Object;
            }

            const string path = "Window.json";

            var fileMock = new Mock<IFile>();
            fileMock.Setup(file => file.Exists(path)).Returns(false).Verifiable();
            fileMock.Setup(file => file.Open(It.IsAny<string>(), It.IsAny<FileMode>())).Verifiable();

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupGet(system => system.File).Returns(fileMock.Object);

            Mock<IConfigurationPaths> configurationPathsMock = GetConfigurationPathsMock();

            var sut = new WindowConfigurationManager(
                configurationPathsMock.Object,
                SerializerFactory,
                new NullLogger<IWindowConfigurationManager>(),
                fileSystemMock.Object);

            // Act
            void Act()
            {
                sut.Initialize();
            }

            Position position = sut.GetPosition();
            Assert.Equal(Position.Empty, position);

            // Assert
            Assert.Throws<ConfigurationException>(Act);
            fileMock.Verify(file => file.Exists(path), Times.Once);
            fileMock.Verify(file => file.Open(It.IsAny<string>(), It.IsAny<FileMode>()), Times.Never());
        }

        [Fact]
        public void WindowConfigurationManager_SaveAsync_Test()
        {
            // Arrange
            const WindowState windowState = WindowState.Maximized;
            var expectedPosition = new Position(500, 400, 0, 0, windowState);
            var settings = new Settings(expectedPosition);

            IMockFileDataAccessor mockFileDataAccessor = new MockFileSystem();
            using var stream = new MockFileStream(mockFileDataAccessor, "Window.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            var serializerMock = new Mock<ISerializationWrapper>();
            serializerMock.Setup(wrapper => wrapper.SerializeAsync(stream, settings)).Verifiable();

            ISerializationWrapper SerializerFactory(SerializerType type)
            {
                return serializerMock.Object;
            }
            
            var fileMock = new Mock<IFile>();
            fileMock.Setup(file => file.Open(It.IsAny<string>(), FileMode.OpenOrCreate)).Returns(stream).Verifiable();

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupGet(system => system.File).Returns(fileMock.Object);

            Mock<IConfigurationPaths> configurationPathsMock = GetConfigurationPathsMock();

            var sut = new WindowConfigurationManager(
                configurationPathsMock.Object,
                SerializerFactory,
                new NullLogger<IWindowConfigurationManager>(),
                fileSystemMock.Object);

            // Act
            sut.SetPosition(expectedPosition);
            sut.SaveAsync();

            // Assert
            fileMock.Verify(file => file.Open(It.IsAny<string>(), FileMode.OpenOrCreate), Times.Once);
            serializerMock.Verify(wrapper => wrapper.SerializeAsync(stream, settings), Times.Once);
        }

        private static Mock<IConfigurationPaths> GetConfigurationPathsMock()
        {
            Mock<IConfigurationPaths> mock = new();
            mock.Setup(m => m.GetUserFilePath(It.IsNotNull<string>()))
                .Returns((string path) => path);

            return mock;
        }
    }
}