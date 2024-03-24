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
    using System.Xml.Linq;
    using Abstractions;
    using Crypto.Abstractions;
    using Sections.ShareConfigXml;

    public class ShareConfigurationManager : IShareConfigurationManager
    {
        private const string ConfigFileName = "ShareConfig.xml";
        private readonly IConfigurationPaths configPaths;
        private readonly ISecure secure;
        private XDocument _configDoc;

        public ShareConfigurationManager(IConfigurationPaths configurationPaths, ISecure secure)
        {
            this.configPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.secure = secure ?? throw new ArgumentNullException(nameof(secure));
        }

        public ShareConfigRoot ConfigRoot { get; private set; }

        public void InitConfig()
        {
            string file = ConfigPath.GetUserFilePath(ConfigFileName);

            if (File.Exists(file))
            {
                this._configDoc = XDocument.Load(file);
                this.ConfigRoot = this.CreateConfigRoot();
            }
            else
            {
                this.CreateDefaultInstance();
            }
        }

        public void Save()
        {
            string filePath = this.configPaths.GetUserFilePath(ConfigFileName);
            this._configDoc.Save(filePath);
        }

        private void CreateDefaultInstance()
        {
            try
            {
                using (Stream stream = typeof(ConfigurationManager).Assembly.GetManifestResourceStream("FSV.Configuration.Defaults.ShareConfig.xml"))
                {
                    this._configDoc = XDocument.Load(stream);
                    this.ConfigRoot = this.CreateConfigRoot();
                }

                this.Save();
            }
            catch (Exception ex)
            {
                throw new Exception("Share Configuration File is corrupt: " + ex.Message);
            }
        }

        private ShareConfigRoot CreateConfigRoot()
        {
            return new ShareConfigRoot(this._configDoc.Root, this.secure);
        }
    }
}