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

namespace FSV.Configuration.Database
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;
    using System.Text;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class ProtectedConnectionString
    {
        private static readonly byte[] ProtectionEntropy = { 36, 3, 78, 15, 19, 65, 29, 178, 17, 154 };
        public static readonly ProtectedConnectionString IntegratedSecurity = new();

        /// <summary>
        ///     Gets or sets encoding to use in encryption and decryption to get bytes.
        /// </summary>
        private static readonly Encoding Encoding = Encoding.ASCII;

        private readonly string encryptedConnectionString;

        private ProtectedConnectionString()
        {
        }

        public ProtectedConnectionString(string encryptedConnectionString) : this()
        {
            if (string.IsNullOrWhiteSpace(encryptedConnectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(encryptedConnectionString));
            }

            this.encryptedConnectionString = encryptedConnectionString;
        }

        public static ProtectedConnectionString Create(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            }

            string encryptedConnectionString = EncryptConnectionString(connectionString);
            return new ProtectedConnectionString(encryptedConnectionString);
        }

        public string GetDecryptedConnectionString()
        {
            return DecryptConnectionString(this.encryptedConnectionString);
        }

        private static string DecryptConnectionString(string encryptedConnectionString)
        {
            if (string.IsNullOrEmpty(encryptedConnectionString))
            {
                return string.Empty;
            }

            try
            {
                byte[] encryptedData = Convert.FromBase64String(encryptedConnectionString);
                byte[] decryptedBytes =
                    ProtectedData.Unprotect(encryptedData, ProtectionEntropy, DataProtectionScope.CurrentUser);
                return Encoding.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static string EncryptConnectionString(string plain)
        {
            if (string.IsNullOrEmpty(plain))
            {
                return string.Empty;
            }

            byte[] data = Encoding.GetBytes(plain);
            byte[] encryptedBytes = ProtectedData.Protect(data, ProtectionEntropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        public override string ToString()
        {
            return this.encryptedConnectionString;
        }
    }
}