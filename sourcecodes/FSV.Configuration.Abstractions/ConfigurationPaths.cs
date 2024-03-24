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

namespace FSV.Configuration
{
    using System;
    using System.IO;
    using Abstractions;

    /// <summary>
    ///     Represents several paths related to application.
    /// </summary>
    public sealed class ConfigurationPaths : IConfigurationPaths
    {
        private const string AppFolderName = "FolderSecurityViewer";

        private const string ExportFolderName = "Export";

        private const string LogFolderName = "Logs";

        public string UserAppDirectory { get; private set; }

        public string MachineAppDirectory { get; private set; }

        public string DocumentsAppDirectory { get; private set; }

        public string ExportDirectory { get; private set; }

        public string LogDirectory { get; private set; }

        public string GetMachineFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            return Path.Combine(this.MachineAppDirectory, fileName);
        }

        public string GetUserFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            return Path.Combine(this.UserAppDirectory, fileName);
        }

        public string GetExportFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            return Path.Combine(this.ExportDirectory, fileName);
        }

        public void CreateApplicationDirectories()
        {
            if (TryCreateDocumentsAppDirectory(out string documentsAppDirectory))
            {
                this.DocumentsAppDirectory = documentsAppDirectory;

                this.ExportDirectory = Path.Combine(documentsAppDirectory, ExportFolderName);
                if (!Directory.Exists(this.ExportDirectory))
                {
                    TryCreateDirectory(this.ExportDirectory);
                }

                this.LogDirectory = Path.Combine(documentsAppDirectory, LogFolderName);
                if (!Directory.Exists(this.LogDirectory))
                {
                    TryCreateDirectory(this.LogDirectory);
                }
            }

            this.MachineAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppFolderName);
            if (!Directory.Exists(this.MachineAppDirectory))
            {
                TryCreateDirectory(this.MachineAppDirectory);
            }

            this.UserAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
            if (!Directory.Exists(this.UserAppDirectory))
            {
                TryCreateDirectory(this.UserAppDirectory);
            }
        }

        private static bool TryCreateDocumentsAppDirectory(out string documentsAppDirectory)
        {
            documentsAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppFolderName);
            if (Directory.Exists(documentsAppDirectory) == false && TryCreateDirectory(documentsAppDirectory))
            {
                return true;
            }

            if (Directory.Exists(documentsAppDirectory))
            {
                return true;
            }

            string localApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
            if (Directory.Exists(localApplicationDataPath) || TryCreateDirectory(localApplicationDataPath))
            {
                documentsAppDirectory = localApplicationDataPath;
                return true;
            }

            return false;
        }

        private static bool TryCreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (FileNotFoundException e)
            {
                var errorMessage = $"Failed to create directory path {path} due to an unhandled error. {e.Message}";
                Console.Error.WriteLine(errorMessage);
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to create directory path {path} due to an unhandled error. {e.Message}";
                Console.Error.WriteLine(errorMessage);
            }

            return false;
        }
    }
}