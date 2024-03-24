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

namespace FSV.Configuration.Sections.ShareConfigXml
{
    using System;
    using System.Security;
    using System.Xml.Linq;
    using Crypto.Abstractions;

    public class ShareCredentials : BaseSection
    {
        private readonly ISecure _secure;

        private XElement _xPassword;

        public ShareCredentials(XElement element, ISecure secure) : base(element, "Credentials")
        {
            this._secure = secure ?? throw new ArgumentNullException(nameof(secure));
            this.Init();
        }

        public string UserName
        {
            get => this.XElement.Element("UserName").Value;
            set => this.XElement.Element("UserName").Value = value ?? string.Empty;
        }

        public SecureString GetPassword()
        {
            string value = this.XElement.Element("Password").Value;
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return this._secure.DecryptToSecureString(value);
        }

        public void SetPassword(SecureString secureString)
        {
            string value = secureString?.Length > 0 ? this._secure.EncryptFromSecureString(secureString) : string.Empty;
            this.XElement.Element("Password").Value = value;
        }

        private void Init()
        {
            if (this.XElement.Element("UserName") == null)
            {
                this.XElement.Add(new XElement("UserName") { Value = string.Empty });
            }

            this._xPassword = this.XElement.Element("Password");
            if (this._xPassword == null)
            {
                this._xPassword = new XElement("Password") { Value = string.Empty };
                this.XElement.Add(this._xPassword);
            }
        }
    }
}