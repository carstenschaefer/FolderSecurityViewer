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

namespace FSV.Business
{
    using System;
    using System.IO;
    using JetBrains.Annotations;

    public static class PathUtil
    {
        public static int GetPathDepth([NotNull] string parentDirectoryPath, [NotNull] string subDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(parentDirectoryPath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(parentDirectoryPath));
            }

            if (string.IsNullOrWhiteSpace(subDirectoryPath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(subDirectoryPath));
            }

            parentDirectoryPath = parentDirectoryPath.NormalizePath();
            subDirectoryPath = subDirectoryPath.NormalizePath();

            if (subDirectoryPath.StartsWith(parentDirectoryPath, StringComparison.InvariantCulture) == false)
            {
                throw new InvalidOperationException("The specified sub-directory path does not belong to the specified parent directory.");
            }

            int mainLength = parentDirectoryPath.GetPathSegments();
            int subLength = subDirectoryPath.GetPathSegments();

            return subLength - mainLength;
        }

        private static string NormalizePath(this string path)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            path = path.Replace('/', directorySeparatorChar)
                .TrimEnd(directorySeparatorChar)
                .Trim();

            return path + directorySeparatorChar;
        }

        private static int GetPathSegments(this string path)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            char[] separator = { directorySeparatorChar };
            const StringSplitOptions splitOptions = StringSplitOptions.RemoveEmptyEntries;

            string[] pathSegments = path.Split(separator, splitOptions);
            return pathSegments.Length;
        }
    }
}