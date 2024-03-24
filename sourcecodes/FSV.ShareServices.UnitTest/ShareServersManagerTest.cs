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

namespace FSV.ShareServices.UnitTest
{
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Abstractions;
    using Constants;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models;
    using Moq;

    [TestClass]
    public class ShareServersManagerTest
    {
        [TestMethod]
        public async Task ServerExists_False_test()
        {
            Mock<IXDocumentManager> documentManagerMock = this.GetEmptyDocumentManagerMock();

            var manager = new ShareServersManager(documentManagerMock.Object, new NullLogger<ShareServersManager>());

            await manager.GetServerListAsync();
            bool result = manager.ServerExists("MyServer");

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public async Task ServerExists_True_test()
        {
            Mock<IXDocumentManager> documentManagerMock = this.GetDocumentManagerMock();

            var manager = new ShareServersManager(documentManagerMock.Object, new NullLogger<ShareServersManager>());

            await manager.GetServerListAsync();
            bool result = manager.ServerExists("WinServers");

            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public async Task CreateServer_test()
        {
            const string serverName = "MainServer";
            Mock<IXDocumentManager> documentManagerMock = this.GetDocumentManagerMock();

            var manager = new ShareServersManager(documentManagerMock.Object, new NullLogger<ShareServersManager>());

            await manager.GetServerListAsync();
            ServerItem serverItem = manager.CreateServer(serverName);

            Assert.AreEqual(ServerState.NotScanned, serverItem.State);
            Assert.AreEqual(serverName, serverItem.Name);
            Assert.AreEqual(0, serverItem.Shares.Count);
        }

        [TestMethod]
        public async Task CreateShare_test()
        {
            const string serverName = "MainServer";
            const string shareName = "FirstShare";
            const string sharePath = "First/Share/Path";

            Mock<IXDocumentManager> documentManagerMock = this.GetDocumentManagerMock();

            var manager = new ShareServersManager(documentManagerMock.Object, new NullLogger<ShareServersManager>());

            await manager.GetServerListAsync();
            ServerItem serverItem = manager.CreateServer(serverName);
            ShareItem shareItem = manager.CreateShare(serverItem, shareName, sharePath);

            Assert.AreEqual(1, serverItem.Shares.Count);
            Assert.AreEqual(shareName, shareItem.Name);
            Assert.AreEqual(sharePath, shareItem.Path);
        }

        private Mock<IXDocumentManager> GetEmptyDocumentManagerMock()
        {
            var documentManagerMock = new Mock<IXDocumentManager>();
            documentManagerMock.Setup(m => m.GetServersDocument())
                .Returns(() => new XDocument());

            return documentManagerMock;
        }

        private Mock<IXDocumentManager> GetDocumentManagerMock()
        {
            var xml = @"
            <Servers>
              <Server Name='WinServers' State='Scanned'>
                <Shares>
                  <Share Name='MDFiles' Path='Shares\DataFiles' />
                  <Share Name='MediaFiles' Path='Shares\AudioFiles' />
                </Shares>
              </Server>
            </Servers>
            ";
            var documentManagerMock = new Mock<IXDocumentManager>();
            documentManagerMock.Setup(m => m.GetServersDocument())
                .Returns(() => XDocument.Parse(xml));

            return documentManagerMock;
        }
    }
}