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

namespace FSV.ShareServices
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;

    internal class XDocumentManager : IXDocumentManager
    {
        private const string ServersFileName = "Servers.xml";

        private readonly IConfigurationPaths configurationPaths;
        private readonly ILogger<XDocumentManager> logger;

        private readonly object syncObject = new();

        public XDocumentManager(
            IConfigurationPaths configurationPaths,
            ILogger<XDocumentManager> logger)
        {
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public XDocument GetServersDocument()
        {
            string serversFilePath = this.configurationPaths.GetUserFilePath(ServersFileName);

            if (File.Exists(serversFilePath))
            {
                try
                {
                    return XDocument.Load(serversFilePath);
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, "Failed to read raw servers document from file ({XmlFile}) due to an error.", ServersFileName);
                }
            }

            this.logger.LogInformation("The server document is invalid or does not exist. A new file will be created ({XmlFile}).", ServersFileName);

            XDocument emptyDocument = CreateEmptyDocument();
            this.Save(emptyDocument);

            return emptyDocument;
        }

        public void Save(XDocument serversDoc)
        {
            if (serversDoc == null)
            {
                throw new ArgumentNullException(nameof(serversDoc));
            }

            string serversFilePath = this.configurationPaths.GetUserFilePath(ServersFileName);

            try
            {
                lock (this.syncObject)
                {
                    serversDoc.Save(serversFilePath);
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to save raw servers document to file ({XmlFile}).", serversFilePath);
                throw new ShareServersManagerException("Failed to save raw servers document to file.");
            }
        }

        /// <summary>
        ///     Creates a new <see cref="XDocument" /> object holding the minimum required nodes.
        /// </summary>
        private static XDocument CreateEmptyDocument()
        {
            return new XDocument(new XElement("Servers"));
        }
    }
}