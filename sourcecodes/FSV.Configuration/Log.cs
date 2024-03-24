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
    using Abstractions;

    internal sealed class Log
    {
        private readonly IConfigurationManager configurationManager;
        private readonly object syncObject = new();

        public Log(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            // this.Instance = this.Refresh();
        }

        public void Disable()
        {
            // LogManager.GetRepository().ResetConfiguration();
        }

        public void Enable()
        {
            // XmlConfigurator.Configure();
        }

        /* private ILog Refresh()
        {
            void TrySetGlobalLogPathProperty()
            {
                string logDirectory = this.configurationManager.LogDirectory;
                string logFile = this.configurationManager.LogFile;
                if (string.IsNullOrWhiteSpace(logDirectory) == false && string.IsNullOrWhiteSpace(logFile) == false) GlobalContext.Properties["LogPath"] = Path.Combine(logDirectory, logFile);
            }

            TrySetGlobalLogPathProperty();

            XmlConfigurator.Configure();

            lock (this.syncObject)
            {
                ILog instance = LogManager.GetLogger("FileAppender");
                bool isLoggingConfigured = this.configurationManager.ConfigRoot?.Logging ?? false;
                if (isLoggingConfigured == false) this.Disable();

                return instance;
            }
        } */
    }
}