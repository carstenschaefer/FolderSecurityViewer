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
    using System.Collections.Generic;
    using System.Text;

    public static class ActiveDirectoryLdapUtility
    {
        public static string GetDomainNameFromDistinguishedName(string dn)
        {
            if (dn == null)
            {
                return string.Empty;
            }

            string ldapPart = RetrieveAllClassItemsFromLdapPath("DC", dn);
            if (string.IsNullOrEmpty(ldapPart))
            {
                return string.Empty;
            }

            ldapPart = ldapPart.ToUpper().Replace("DC=", string.Empty);
            const char dot = '.';
            const char comma = ',';
            ldapPart = ldapPart.Replace(comma, dot);
            return ldapPart.ToLower();
        }

        private static string RetrieveAllClassItemsFromLdapPath(string className, string ldapPath)
        {
            if ((ldapPath == null) | (ldapPath == string.Empty))
            {
                return string.Empty;
            }

            if ((className == null) | (className == string.Empty))
            {
                return ldapPath;
            }

            var classItemList = new StringBuilder();

            if (className.EndsWith("="))
            {
                className = $"{className}=";
            }

            string[] ldapParts = GetDnElementListFromLdapPath(ldapPath);

            const char c = ',';
            foreach (string ldapPartLoopVariable in ldapParts)
            {
                string ldapPart = ldapPartLoopVariable;
                if (!ldapPart.ToUpper().StartsWith(className.ToUpper()))
                {
                    continue;
                }

                if (classItemList.Length > 0)
                {
                    classItemList.Append(c);
                }

                classItemList.Append(ldapPart);
            }

            return classItemList.ToString();
        }

        public static string[] GetDnElementListFromLdapPath(string ldapPath)
        {
            if (string.IsNullOrEmpty(ldapPath))
            {
                return new string[] { };
            }

            // handle a possible ldap path header (like "LDAP://server/")
            int firstEqualIdx = ldapPath.IndexOf('=');

            // look for the first '=' sign
            if (firstEqualIdx != -1)
            {
                // from the found '=' sign search back to the first '/' sign
                int lastSlashIdx = ldapPath.LastIndexOf('/', firstEqualIdx);
                if (lastSlashIdx != -1)
                {
                    // if the found '/' is not the last char of the string, cut everything before it
                    if (lastSlashIdx == ldapPath.Length - 1)
                    {
                        ldapPath = string.Empty;
                    }
                    else
                    {
                        ldapPath = ldapPath.Substring(lastSlashIdx + 1);
                    }
                }
            }

            var dnBlock = new StringBuilder();
            var ldapParts = new List<string>();
            var lastChar = '\\';

            // search the DN for real comma chars (that are NOT escaped) 
            // and split it at these positions
            var i = 0;
            while (i < ldapPath.Length)
            {
                char curChar = ldapPath[i];

                // if the current char is a ',' and the previous one is a '\',
                // this comma is a real DN separator - so we add the so far collected
                // characters to the result list and reset the char collector
                if (curChar == ',' && lastChar != '\\')
                {
                    ldapParts.Add(dnBlock.ToString());
                    dnBlock.Length = 0;
                }
                else
                {
                    // add the character to the collector
                    dnBlock.Append(curChar);
                }

                // handle the special case of an escaped '\' char
                // (to avoid conflicts if the escaped '\' is followed by a comma)
                if (curChar == '\\' && lastChar == '\\')
                {
                    lastChar = '\\';
                }
                else
                {
                    lastChar = curChar;
                }

                // update last character
                i = i + 1;
            }

            // also add the last collected chars to the result list
            ldapParts.Add(dnBlock.ToString());
            return ldapParts.ToArray();
        }
    }
}