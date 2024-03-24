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

namespace FSV.Extensions.WindowConfiguration
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Serialization.Abstractions;

    public class WindowConfigurationManager : IWindowConfigurationManager
    {
        private const string FileName = "Window.json";
        private const SerializerType SerializerType = Serialization.Abstractions.SerializerType.Json;

        private readonly string filePath;
        private readonly IFileSystem fileSystem;
        private readonly ILogger<IWindowConfigurationManager> logger;
        private readonly Func<SerializerType, ISerializationWrapper> serializerFactory;
        private Settings settings;

        public WindowConfigurationManager(
            IConfigurationPaths configurationPaths,
            Func<SerializerType, ISerializationWrapper> serializerFactory,
            ILogger<IWindowConfigurationManager> logger,
            IFileSystem fileSystem)
        {
            if (configurationPaths is null)
            {
                throw new ArgumentNullException(nameof(configurationPaths));
            }

            this.serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            this.filePath = configurationPaths.GetUserFilePath(FileName);
        }

        public Position GetPosition()
        {
            return this.settings?.Position ?? Position.Empty;
        }

        public void SetPosition(Position position)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            this.settings = new Settings(position);
        }

        public void Initialize()
        {
            try
            {
                IFile file = this.fileSystem.File;
                if (file.Exists(this.filePath) == false)
                {
                    throw new FileNotFoundException($"File not found: {this.filePath}");
                }

                ISerializationWrapper serializationWrapper = this.serializerFactory.Invoke(SerializerType);
                using Stream stream = file.Open(this.filePath, FileMode.Open);
                this.settings = serializationWrapper.Deserialize<Settings>(stream);
            }
            catch (Exception e)
            {
                this.settings = Settings.Empty;
                this.logger.LogError(e, "Failed to load window settings from file: {file}.", this.filePath);
                throw new ConfigurationException($"Cannot load window settings from file: {this.filePath}.", e);
            }
        }

        public async Task SaveAsync()
        {
            if (this.settings == null || this.settings.Equals(Settings.Empty))
            {
                return;
            }

            try
            {
                ISerializationWrapper serializationWrapper = this.serializerFactory.Invoke(SerializerType);
                using Stream stream = this.fileSystem.File.Open(this.filePath, FileMode.OpenOrCreate);
                stream.SetLength(0);
                await serializationWrapper.SerializeAsync(stream, this.settings);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to save window setting in the file: {file}.", this.filePath);
                throw new ConfigurationException($"Cannot save window setting in the file: {this.filePath}", ex);
            }
        }
    }
}