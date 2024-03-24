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

    /// <summary>
    ///     Represents several paths related to application.
    /// </summary>
    /// <remarks>Marked to be removed in future. Use FSV.Configuration.ConfigurationPath instead.</remarks>
    public static class ConfigPath
    {
        public static readonly string AppFolderName = "FolderSecurityViewer";
        private static readonly object syncObject = new();

        public static string GetOrCreateApplicationDataFolderIfNotExists()
        {
            string localApplicationDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userAppDirectory = Path.Combine(localApplicationDataFolderPath, AppFolderName);

            lock (syncObject)
            {
                if (!Directory.Exists(userAppDirectory))
                {
                    Directory.CreateDirectory(userAppDirectory);
                }
            }

            return userAppDirectory;
        }

        private static string GetMachineAppDirectory()
        {
            string commonApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string path = Path.Combine(commonApplicationDataPath, AppFolderName);

            lock (syncObject)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            return path;
        }

        public static string GetMachineFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            string machineAppDirectoryPath = GetMachineAppDirectory();
            return Path.Combine(machineAppDirectoryPath, fileName);
        }

        public static string GetUserFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            string path = GetOrCreateApplicationDataFolderIfNotExists();
            return Path.Combine(path, fileName);
        }

        public static string GetDocumentsPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            string myDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(myDocumentsFolderPath, AppFolderName, fileName);
        }
    }
}