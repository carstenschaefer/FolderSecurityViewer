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

namespace FSV.Crypto
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using Abstractions;

    public class Secure : ISecure
    {
        private static readonly byte[] PasswordEntropy = { 3, 34, 76, 45, 9, 85, 20, 108, 7, 54 };

        public string EncryptFromSecureString(SecureString secureString, bool useEntropy = true)
        {
            if (secureString?.Length == 0)
            {
                return string.Empty;
            }

            IntPtr pwdPtr = IntPtr.Zero;
            var bytes = new byte[secureString.Length];
            try
            {
                pwdPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);

                for (var i = 0; i < secureString.Length; i++)
                {
                    bytes[i] = Marshal.ReadByte(pwdPtr, i * 2);
                }

                byte[] encryptedBytes = ProtectedData.Protect(
                    bytes,
                    useEntropy ? PasswordEntropy : null,
                    DataProtectionScope.CurrentUser);

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception)
            {
                return string.Empty;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(pwdPtr);

                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = 0;
                }
            }
        }

        public SecureString DecryptToSecureString(string encryptedPassword, bool useEntropy = true)
        {
            if (string.IsNullOrWhiteSpace(encryptedPassword))
            {
                return null;
            }

            var secured = new SecureString();

            byte[] decryptedBytes = null;
            try
            {
                decryptedBytes = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedPassword),
                    useEntropy ? PasswordEntropy : null,
                    DataProtectionScope.CurrentUser);

                if (decryptedBytes != null)
                {
                    for (var i = 0; i < decryptedBytes.Length; i++)
                    {
                        secured.AppendChar((char)decryptedBytes[i]);
                    }
                }
            }
            finally
            {
                if (decryptedBytes != null)
                {
                    for (var i = 0; i < decryptedBytes.Length; i++)
                    {
                        decryptedBytes[i] = 0;
                    }
                }
            }

            secured?.MakeReadOnly();
            return secured;
        }

        public IEnumerable<byte> GetBytes(SecureString secureString)
        {
            if (secureString?.Length == 0)
            {
                throw new ArgumentNullException(nameof(secureString));
            }

            IntPtr pwdPtr = IntPtr.Zero;
            try
            {
                pwdPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);

                for (var i = 0; i < secureString.Length; i++)
                {
                    yield return Marshal.ReadByte(pwdPtr, i * 2);
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(pwdPtr);
            }
        }
    }
}