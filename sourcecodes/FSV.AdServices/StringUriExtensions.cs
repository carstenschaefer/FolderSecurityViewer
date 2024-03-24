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

    public static class StringUriExtensions
    {
        public static string GetServerName(this string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(directoryPath));
            }

            const string localhost = "Localhost";
            if (Uri.TryCreate(directoryPath, UriKind.Absolute, out Uri uri) == false)
            {
                return localhost;
            }

            string host = uri.Host;
            return string.IsNullOrEmpty(host) ? localhost : host;
        }
    }
}