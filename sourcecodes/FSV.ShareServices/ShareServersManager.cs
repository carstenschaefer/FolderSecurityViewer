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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Abstractions;
    using Constants;
    using Microsoft.Extensions.Logging;
    using Models;

    public sealed class ShareServersManager : IShareServersManager
    {
        private readonly IXDocumentManager documentManager;
        private readonly ILogger<ShareServersManager> logger;
        private readonly object syncObject = new();
        private XElement serverList;

        private XDocument serversDoc;

        public ShareServersManager(
            IXDocumentManager documentManager,
            ILogger<ShareServersManager> logger)
        {
            this.documentManager = documentManager ?? throw new ArgumentNullException(nameof(documentManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ServerItem>> GetServerListAsync()
        {
            return await Task.Run(this.LoadServersFromXml);
        }

        public bool ServerExists(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverName));
            }

            var result = false;

            lock (this.syncObject)
            {
                foreach (XElement element in this.serverList.Elements())
                {
                    if (!element.HasAttributes && !string.IsNullOrEmpty(element.Value))
                    {
                        element.SetAttributeValue("Name", element.Value);
                        element.SetAttributeValue("State", ServerState.NotScanned);
                        element.Value = string.Empty;
                    }

                    if (element.Attribute("Name") != null)
                    {
                        result = string.Compare(element.Attribute("Name").Value, serverName, StringComparison.InvariantCultureIgnoreCase) == 0;
                        if (result)
                        {
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public void RemoveServer(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException($"'{nameof(serverName)}' cannot be null or whitespace.", nameof(serverName));
            }

            XElement item = this.GetServerElement(serverName);

            if (item == null)
            {
                return;
            }

            lock (this.syncObject)
            {
                item.Remove();
            }

            this.Save();
        }

        public ServerItem CreateServer(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException($"'{nameof(serverName)}' cannot be null or whitespace.", nameof(serverName));
            }

            var res = new XElement("Server");
            res.SetAttributeValue("Name", serverName);
            res.SetAttributeValue("State", ServerState.NotScanned);
            res.Add(new XElement("Shares"));

            lock (this.syncObject)
            {
                this.serverList.Add(res);
            }

            return new ServerItem(serverName, ServerState.NotScanned);
        }

        public ShareItem CreateShare(ServerItem server, string name, string path)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            XElement xElement = this.GetServerElement(server.Name);
            if (xElement == null)
            {
                throw new ShareServersManagerException($"Server element with name {server.Name} not found in collection.");
            }

            var xShare = new XElement("Share");

            xShare.SetAttributeValue("Name", name);
            xShare.SetAttributeValue("Path", path);

            lock (this.syncObject)
            {
                xElement.Element("Shares").Add(xShare);
            }

            var shareitem = new ShareItem(name, path);
            server.Shares.Add(shareitem);

            return shareitem;
        }

        public void RemoveShares(ServerItem server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            lock (this.syncObject)
            {
                XElement xElement = this.GetServerElement(server.Name);
                xElement?.Element("Shares").RemoveAll();
            }
        }

        public void RemoveServers()
        {
            if (!this.serverList.HasElements)
            {
                return;
            }

            lock (this.syncObject)
            {
                this.serverList.RemoveAll();
            }
        }

        public void SetStateScanned(ServerItem server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            lock (this.syncObject)
            {
                XElement xElement = this.GetServerElement(server.Name);
                if (xElement == null)
                {
                    throw new ShareServersManagerException($"Server element with name {server.Name} not found in collection.");
                }

                server.State = ServerState.Scanned;
                xElement.SetAttributeValue("State", ServerState.Scanned);
            }
        }

        public void SetStateScanFailed(ServerItem server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            lock (this.syncObject)
            {
                XElement xElement = this.GetServerElement(server.Name);
                if (xElement == null)
                {
                    throw new ShareServersManagerException($"Server element with name {server.Name} not found in collection.");
                }

                server.State = ServerState.Failure;
                xElement.SetAttributeValue("State", ServerState.Failure);
            }
        }

        public void Save()
        {
            lock (this.syncObject)
            {
                this.documentManager.Save(this.serversDoc);
            }
        }

        private IEnumerable<ServerItem> LoadServersFromXml()
        {
            this.LoadXmlContent();

            IEnumerable<ServerItem> list;
            lock (this.syncObject)
            {
                IEnumerable<XElement> servers = this.serverList.Elements();
                list = servers.Select(this.ConvertElementToServerItem);
            }

            return list;
        }

        private void LoadXmlContent()
        {
            try
            {
                lock (this.syncObject)
                {
                    this.serversDoc = this.documentManager.GetServersDocument();
                    this.serverList = this.serversDoc.Element("Servers");

                    if (this.serverList != null)
                    {
                        return;
                    }

                    this.serverList = new XElement("Servers");
                    this.serversDoc.Add(this.serverList);
                }
            }
            catch (ShareServersManagerException e)
            {
                this.logger.LogError(e, "Failed load server list from document.");
                throw new ShareServersManagerException("The server data file is invalid, or does not exist. See inner exception for further details.", e);
            }
            catch (SecurityException ex)
            {
                throw new ShareServersManagerException("Servers data file is inaccessible.", ex);
            }
            catch (XmlException ex)
            {
                throw new ShareServersManagerException("Servers data file is corrupt.", ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new ShareServersManagerException("Server data file does not exist.", ex);
            }
        }

        private ServerItem ConvertElementToServerItem(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            XAttribute nameAttribute = element.Attribute("Name");
            if (nameAttribute == null)
            {
                throw new ShareServersManagerException("Attribute Name is not found in Server element.");
            }

            XAttribute stateAttribute = element.Attribute("State");
            if (stateAttribute == null)
            {
                throw new ShareServersManagerException("Attribute State is not found in Server element.");
            }

            if (!Enum.TryParse(stateAttribute.Value, out ServerState state))
            {
                throw new ShareServersManagerException("Invalid server state value.");
            }

            IList<ShareItem> shares = element.Element("Shares").Elements().Select(this.ConvertElementToShareItem).ToList();

            return new ServerItem(nameAttribute.Value, state, shares);
        }

        private ShareItem ConvertElementToShareItem(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            XAttribute nameAttribute = element.Attribute("Name");
            if (nameAttribute == null)
            {
                throw new ShareServersManagerException("Attribute Name is not found in Server element.");
            }

            XAttribute pathAttribute = element.Attribute("Path");
            if (pathAttribute == null)
            {
                throw new ShareServersManagerException("Attribute Path is not found in Server element.");
            }

            IEnumerable<ShareItem> shares = element.Elements().Select(this.ConvertElementToShareItem);

            return new ShareItem(nameAttribute.Value, pathAttribute.Value);
        }

        private XElement GetServerElement(string name)
        {
            lock (this.syncObject)
            {
                IEnumerable<XElement> servers = this.serverList.Elements();
                return servers.FirstOrDefault(m => string.Compare(m.Attribute("Name").Value, name, true) == 0);
            }
        }
    }
}