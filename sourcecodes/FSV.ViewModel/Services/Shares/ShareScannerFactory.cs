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

namespace FSV.ViewModel.Services.Shares
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Text;
    using Abstractions;
    using AdServices.Abstractions;
    using Configuration.Sections.ShareConfigXml;
    using Crypto.Abstractions;
    using Setting;

    internal class ShareScannerFactory : IShareScannerFactory
    {
        private readonly IAdAuthentication adAuthentication;
        private readonly ISecure secure;
        private readonly ISettingShareService settingShareService;

        public ShareScannerFactory(
            ISecure secure,
            IAdAuthentication adAuthentication,
            ISettingShareService settingShareService)
        {
            this.secure = secure ?? throw new ArgumentNullException(nameof(secure));
            this.adAuthentication = adAuthentication ?? throw new ArgumentNullException(nameof(adAuthentication));
            this.settingShareService = settingShareService ?? throw new ArgumentNullException(nameof(settingShareService));
        }

        public IShareServerScanner CreateServerScanner()
        {
            AdUserDetail user = this.GetCredentials();
            if (user == null)
            {
                return new ShareServerScanner();
            }

            return new ShareServerScanner(user.UserName, user.Domain, user.Password);
        }

        public IShareScanner CreateShareScanner()
        {
            AdUserDetail user = this.GetCredentials();
            if (user == null)
            {
                return new ShareScanner();
            }

            return new ShareScanner(user.UserName, user.Domain, user.Password);
        }

        private AdUserDetail GetCredentials()
        {
            ShareCredentials credentials = this.settingShareService.GetCredentials();
            if (credentials == null || string.IsNullOrWhiteSpace(credentials.UserName))
            {
                return null;
            }

            SecureString securePassword = credentials.GetPassword();
            var password = string.Empty;

            if (securePassword != null)
            {
                byte[] decryptedBytes = this.secure.GetBytes(securePassword).ToArray();
                password = Encoding.UTF8.GetString(decryptedBytes);
            }

            KeyValuePair<string, string> upn = this.adAuthentication.GetUserPrincipalName(credentials.UserName);

            return new AdUserDetail(upn.Key, password, upn.Value);
        }

        private class AdUserDetail
        {
            internal AdUserDetail(string userName, string password, string domain)
            {
                this.UserName = userName;
                this.Password = password;
                this.Domain = domain ?? string.Empty;
            }

            internal string UserName { get; }
            internal string Domain { get; }
            internal string Password { get; }
        }
    }
}