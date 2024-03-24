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

namespace FolderSecurityViewer.Controls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FSV.FileSystem.Interop.Core.Abstractions;
    using JetBrains.Annotations;

    public static class PathToEnumerableConverter
    {
        private static readonly string UncPrefix = new(Path.DirectorySeparatorChar, 2);

        public static IEnumerable<PathSelectorItem> Convert([NotNull] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            return IsUncPath(path) ? GetUncItems(path) : GetPathItems(path);
        }

        private static IEnumerable<PathSelectorItem> GetUncItems(string path)
        {
            var pathWithoutPrefix = string.Empty;
            var hasLongPath = false;

            if (path.StartsWith(Constants.LongUncPathPrefix))
            {
                // The path starts with long path prefix.
                hasLongPath = true;
                pathWithoutPrefix = path.Remove(0, Constants.LongUncPathPrefix.Length);
            }
            else if (path.Length > UncPrefix.Length)
            {
                // Path starts with unc path chars, and it is not empty.
                pathWithoutPrefix = path.Remove(0, UncPrefix.Length);
            }

            // Empty path found after stripping Long path prefix, or unc path chars.  
            if (string.IsNullOrWhiteSpace(pathWithoutPrefix))
            {
                return Enumerable.Empty<PathSelectorItem>();
            }

            string[] parts = pathWithoutPrefix.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            // First two entries are server name and share name.
            // But if length of the parts array is one, it has only server name. So return empty enumerable.
            if (parts.Length == 1)
            {
                return Enumerable.Empty<PathSelectorItem>();
            }

            string serverName = hasLongPath ? $"{Constants.LongUncPathPrefix}{parts[0]}" : $"{UncPrefix}{parts[0]}";
            List<PathSelectorItem> items = new(parts.Length)
            {
                new PathSelectorItem(serverName, $"{UncPrefix}{parts[0]}", true)
            };

            string sharePath = serverName;
            foreach (string part in parts.Skip(1))
            {
                sharePath = Path.Combine(sharePath, part);
                PathSelectorItem item = new(sharePath, part);

                items.Add(item);
            }

            return items;
        }

        private static IEnumerable<PathSelectorItem> GetPathItems(string path)
        {
            string pathWithoutPrefix;
            var hasLongPath = false;

            if (path.StartsWith(Constants.LongPathPrefix))
            {
                // The path starts with long path prefix.
                hasLongPath = true;
                pathWithoutPrefix = path.Remove(0, Constants.LongPathPrefix.Length);
            }
            else
            {
                pathWithoutPrefix = path;
            }

            // Empty path found after stripping Long path prefix.  
            if (string.IsNullOrWhiteSpace(pathWithoutPrefix))
            {
                return Enumerable.Empty<PathSelectorItem>();
            }

            string[] parts = pathWithoutPrefix.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            string partPath = hasLongPath ? $"{Constants.LongPathPrefix}{parts[0]}\\" : $"{parts[0]}\\";

            List<PathSelectorItem> items = new(parts.Length)
            {
                new PathSelectorItem(partPath, $"{parts[0]}\\")
            };

            foreach (string part in parts.Skip(1))
            {
                partPath = Path.Combine(partPath, part);
                PathSelectorItem item = new(partPath, part);

                items.Add(item);
            }

            return items;
        }

        private static bool IsUncPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            bool result = value.StartsWith(Constants.LongUncPathPrefix) ||
                          (!value.StartsWith(Constants.LongPathPrefix) && value.StartsWith(UncPrefix));

            return result;
        }
    }
}