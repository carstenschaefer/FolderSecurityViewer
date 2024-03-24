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

namespace FSV.Console.Exporter
{
    using System;
    using System.IO;
    using Configuration.Abstractions;

    public class ExportFilePath
    {
        private readonly string baseFilePath;
        private readonly IConfigurationPaths configurationPaths;

        public ExportFilePath(IConfigurationPaths configurationPaths, string baseFilePath) : this(configurationPaths, baseFilePath, null)
        {
        }

        public ExportFilePath(IConfigurationPaths configurationPaths, string baseFilePath, string filePrefix)
        {
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.FileNamePrefix = filePrefix;
            this.baseFilePath = baseFilePath;
        }

        public string FileNamePrefix { get; }
        public string DirectoryPath { get; }

        public string GetFilePath(string extension)
        {
            string name = string.IsNullOrEmpty(this.baseFilePath) ? this.CreateFilePath(extension) : this.BuildFilePath(extension);
            return name;
        }

        /// <summary>
        ///     Creates a valid file path if baseFilePath is not complete with file name.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private string BuildFilePath(string extension)
        {
            string path = this.baseFilePath;
            try
            {
                // Get attributes associated with path. If the path doesn't exist exception occurs.
                FileAttributes attributes = File.GetAttributes(path);

                // Check if the path is directory.
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    path = this.CreateFilePath(extension);
                }
            }
            catch (FileNotFoundException)
            {
                // Path doesn't exist. Find out if the user has provided path with extension. Make sure to attach extension if it is missing from path.
                var info = new DirectoryInfo(path);

                if (string.IsNullOrEmpty(info.Extension))
                {
                    path += extension;
                }
            }

            return path;
        }

        private string CreateFilePath(string extension)
        {
            var fileCounter = 0;

            while (true)
            {
                var name = string.Empty;

                if (!string.IsNullOrEmpty(this.FileNamePrefix))
                {
                    name = $"{this.FileNamePrefix} - ";
                }

                name = $"{name}{DateTime.Now:yyyy-MMM-dd_HH-mm-ss}";

                if (fileCounter > 0)
                {
                    name = $"{name}_{fileCounter}";
                }

                string filePath = this.configurationPaths.GetExportFilePath(name);
                filePath = $"{filePath}{extension}";

                if (!File.Exists(filePath))
                {
                    return filePath;
                }

                fileCounter++;
            }
        }
    }
}