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

namespace FSV.Crypto.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SecureTest
    {
        private const string Password = "Abc#123";

        [TestMethod]
        public void Secure_EncryptFromSecureString_Empty_test()
        {
            ISecure secure = new Secure();
            string encrypted = secure.EncryptFromSecureString(new SecureString());

            Assert.IsNotNull(encrypted);
            Assert.AreEqual(0, encrypted.Length);
        }

        [TestMethod]
        public void Secure_EncryptFromSecureString_Entropy_test()
        {
            ISecure secure = new Secure();
            SecureString secureString = this.GetSecureString(Password);
            string encrypted = secure.EncryptFromSecureString(secureString);

            Assert.IsNotNull(encrypted);
            Assert.IsFalse(Password.Equals(encrypted));
        }

        [TestMethod]
        public void Secure_EncryptFromSecureString_NoEntropy_test()
        {
            ISecure secure = new Secure();
            SecureString secureString = this.GetSecureString(Password);

            string encryptedWithEntropy = secure.EncryptFromSecureString(secureString);
            string encryptedWithoutEntropy = secure.EncryptFromSecureString(secureString, false);

            Assert.IsNotNull(encryptedWithEntropy);
            Assert.IsNotNull(encryptedWithoutEntropy);
            Assert.IsFalse(encryptedWithEntropy.Equals(encryptedWithoutEntropy));
        }

        [TestMethod]
        public void Secure_DecryptToSecureString_Empty_test()
        {
            const string password = null;
            ISecure secure = new Secure();

            SecureString decrypted = secure.DecryptToSecureString(password);

            Assert.IsNull(decrypted);
        }

        [TestMethod]
        public void Secure_DecryptToSecureString_Entropy_test()
        {
            ISecure secure = new Secure();

            string encrypted = secure.EncryptFromSecureString(this.GetSecureString(Password));
            SecureString decrypted = secure.DecryptToSecureString(encrypted);

            Assert.AreEqual(Password.Length, decrypted.Length);
        }

        [TestMethod]
        public void Secure_DecryptToSecureString_NoEntropy_test()
        {
            ISecure secure = new Secure();

            string encrypted = secure.EncryptFromSecureString(this.GetSecureString(Password), false);
            SecureString decrypted = secure.DecryptToSecureString(encrypted, false);

            Assert.AreEqual(Password.Length, decrypted.Length);
        }

        [TestMethod]
        public void Secure_DecryptToSecureString_different_entropies_encrypt_false_decrypt_true_test()
        {
            ISecure secure = new Secure();

            string encrypted = secure.EncryptFromSecureString(this.GetSecureString(Password), false);

            Assert.Throws<CryptographicException>(() => secure.DecryptToSecureString(encrypted));
        }

        [TestMethod]
        public void Secure_DecryptToSecureString_different_entropies_encrypt_true_decrypt_false_test()
        {
            ISecure secure = new Secure();

            string encrypted = secure.EncryptFromSecureString(this.GetSecureString(Password));

            Assert.Throws<CryptographicException>(() => secure.DecryptToSecureString(encrypted, false));
        }

        [TestMethod]
        public void Secure_GetBytes_test()
        {
            ISecure secure = new Secure();
            using SecureString secureString = this.GetSecureString(Password);

            IEnumerable<byte> bytes = secure.GetBytes(secureString);

            Assert.AreEqual(secureString.Length, bytes.Count());
        }

        private SecureString GetSecureString(string password)
        {
            var secureString = new SecureString();
            foreach (char item in password)
            {
                secureString.AppendChar(item);
            }

            secureString.MakeReadOnly();
            return secureString;
        }
    }
}