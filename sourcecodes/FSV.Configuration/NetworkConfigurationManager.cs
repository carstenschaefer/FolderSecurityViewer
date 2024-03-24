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
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;

    public class NetworkConfigurationManager
    {
        private const string CONFIG_FILE_NAME = "Network.xml";
        private const string PASSWORD_FILE_NAME = "NetworkPassword.dat";
        internal static byte[] PASSWORD_ENTROPY = { 3, 34, 76, 45, 9, 85, 20, 108, 7, 54 };

        private static string _configFilePath;
        private static string _configPasswordFilePath;

        private static XDocument _configDoc;
        private static XDocument _configPasswordDoc;
        private static XElement _configRoot;
        private static XElement _configProxy;
        private static XElement _configCredentials;

        public static ProxyType ProxyType
        {
            get => (ProxyType)Enum.Parse(typeof(ProxyType), _configProxy.Attribute("type").Value);
            set => _configProxy.Attribute("type").Value = value.ToString();
        }

        public static string ProxyServer
        {
            get => _configProxy.Element("Server").Value;
            set => _configProxy.Element("Server").Value = value;
        }

        public static int ProxyPort
        {
            get
            {
                var value = 0;
                int.TryParse(_configProxy.Element("Port").Value, out value);
                return value;
            }
            set => _configProxy.Element("Port").Value = value < 0 ? "0" : value.ToString();
        }

        public static bool UseCredentials
        {
            get => bool.Parse(_configCredentials.Attribute("use").Value);
            set => _configCredentials.Attribute("use").Value = value.ToString();
        }

        public static string Username
        {
            get => _configCredentials.Element("Username").Value;
            set => _configCredentials.Element("Username").Value = value;
        }

        public static string Password
        {
            get => _configPasswordDoc.Root.Element("ProxyPassword").Value;
            set => _configPasswordDoc.Root.Element("ProxyPassword").Value = value;
        }

        public static void InitConfig()
        {
            try
            {
                string applicationDataFolderPath = ConfigPath.GetOrCreateApplicationDataFolderIfNotExists();
                _configFilePath = Path.Combine(applicationDataFolderPath, CONFIG_FILE_NAME);
                _configPasswordFilePath = Path.Combine(applicationDataFolderPath, PASSWORD_FILE_NAME);

                if (File.Exists(_configFilePath))
                {
                    _configDoc = XDocument.Load(_configFilePath);
                    _configRoot = _configDoc.Root;

                    _configProxy = _configRoot.Element("Proxy");
                    _configCredentials = _configRoot.Element("ProxyCredentials");
                }
                else
                {
                    CreateDefaultInstance();
                }

                if (File.Exists(_configPasswordFilePath))
                {
                    LoadPasswordDefaultFile();
                }
                else
                {
                    CreatePasswordDefaultInstance();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Network Configuration File is corrupt: " + ex.Message);
            }
        }

        private static void LoadPasswordDefaultFile()
        {
            using (var stream = new FileStream(_configPasswordFilePath, FileMode.Open))
            {
                var encryptedBytes = new byte[stream.Length];
                stream.Read(encryptedBytes, 0, encryptedBytes.Length);
                stream.Close();

                byte[] bytes = ProtectedData.Unprotect(encryptedBytes, PASSWORD_ENTROPY, DataProtectionScope.CurrentUser);

                _configPasswordDoc = XDocument.Parse(Encoding.UTF8.GetString(bytes));
            }
        }

        private static void CreateDefaultInstance()
        {
            _configRoot = new XElement("Network");

            _configProxy = new XElement("Proxy");
            _configProxy.Add(new XAttribute("type", ProxyType.None.ToString()));

            _configProxy.Add(new XElement("Server"));
            _configProxy.Add(new XElement("Port")
            {
                Value = "0"
            });

            _configCredentials = new XElement("ProxyCredentials");

            _configCredentials.Add(new XAttribute("use", bool.FalseString));
            _configCredentials.Add(new XElement("Username"));

            _configRoot.Add(_configProxy);
            _configRoot.Add(_configCredentials);

            _configDoc = new XDocument();
            _configDoc.Add(_configRoot);

            _configDoc.Save(_configFilePath);
        }

        private static void CreatePasswordDefaultInstance()
        {
            _configPasswordDoc = new XDocument();
            _configPasswordDoc.Add(new XElement("Passwords"));
            _configPasswordDoc.Root.Add(new XElement("ProxyPassword"));

            byte[] bytes = Encoding.UTF8.GetBytes(_configPasswordDoc.ToString());
            byte[] encryptedBytes = ProtectedData.Protect(bytes, PASSWORD_ENTROPY, DataProtectionScope.CurrentUser);
            using (var writer = new FileStream(_configPasswordFilePath, FileMode.CreateNew))
            {
                writer.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }

        public static void Save()
        {
            _configDoc.Save(_configFilePath);

            byte[] bytes = Encoding.UTF8.GetBytes(_configPasswordDoc.ToString());
            byte[] encryptedBytes = ProtectedData.Protect(bytes, PASSWORD_ENTROPY, DataProtectionScope.CurrentUser);
            using (var writer = new FileStream(_configPasswordFilePath, FileMode.Truncate))
            {
                writer.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }
    }
}