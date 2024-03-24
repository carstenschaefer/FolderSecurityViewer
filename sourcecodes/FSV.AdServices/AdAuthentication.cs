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

namespace FSV.AdServices
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using System.Security;
    using System.Text;
    using Abstractions;
    using Crypto.Abstractions;

    public class AdAuthentication : IAdAuthentication
    {
        private readonly ISecure _secure;

        public AdAuthentication(ISecure secure)
        {
            this._secure = secure ?? throw new ArgumentNullException(nameof(secure));
        }

        public bool ValidateUser(string userName, SecureString password)
        {
            byte[] decryptedBytes = this._secure.GetBytes(password).ToArray();
            using PrincipalContext principalContext = GetPrincipalContext();
            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, userName);
            return principalContext.ValidateCredentials(userPrincipal.UserPrincipalName, Encoding.UTF8.GetString(decryptedBytes), ContextOptions.Negotiate);
        }

        public KeyValuePair<string, string> GetUserPrincipalName(string userName)
        {
            using PrincipalContext principalContext = GetPrincipalContext();
            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, userName);
            string[] parts = userPrincipal.UserPrincipalName.Split('@');
            return new KeyValuePair<string, string>(parts[0], parts[1]);
        }

        private static PrincipalContext GetPrincipalContext()
        {
            return new PrincipalContext(ContextType.Domain, null);
        }
    }
}