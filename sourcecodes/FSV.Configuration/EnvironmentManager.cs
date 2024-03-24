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

    public class EnvironmentManager
    {
        private const string ENVIRONMENT_FILE_NAME = "Environment.dat";

        public static bool ExistEnvironmentFile => File.Exists(GetEnvironmentCompletePath);

        private static string GetEnvironmentCompletePath
        {
            get
            {
                string configFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ConfigPath.AppFolderName);
                string environmentFilePath = Path.Combine(configFolderPath, ENVIRONMENT_FILE_NAME);
                return environmentFilePath;
            }
        }

        public static void CreateEnvironmentFile(int users)
        {
            var environmentDoc = new XDocument();
            var xUsers = new XElement("Users");
            xUsers.SetAttributeValue("type", users);
            environmentDoc.Add(xUsers);

            byte[] bytes = Encoding.UTF8.GetBytes(environmentDoc.ToString());
            byte[] encryptedBytes = ProtectedData.Protect(bytes, NetworkConfigurationManager.PASSWORD_ENTROPY, DataProtectionScope.CurrentUser);
            using (var writer = new FileStream(GetEnvironmentCompletePath, FileMode.CreateNew))
            {
                writer.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }

        public static int GetUsersFromEnvironmentFile()
        {
            using (var stream = new FileStream(GetEnvironmentCompletePath, FileMode.Open))
            {
                var encryptedBytes = new byte[stream.Length];
                stream.Read(encryptedBytes, 0, encryptedBytes.Length);
                stream.Close();

                byte[] bytes = ProtectedData.Unprotect(encryptedBytes, NetworkConfigurationManager.PASSWORD_ENTROPY, DataProtectionScope.CurrentUser);

                XDocument environmentDoc = XDocument.Parse(Encoding.UTF8.GetString(bytes));
                XElement users = environmentDoc.Element("Users");
                var userCount = 0;

                XAttribute typeAttr = users.Attribute("type");
                if (typeAttr != null)
                {
                    int.TryParse(users.Attribute("type").Value, out userCount);
                }

                return userCount;
            }
        }
    }
}