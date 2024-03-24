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

namespace FSV.FileSystem.Interop
{
    using System.IO;
    using Core.Abstractions;

    internal static class Win32FindDataExtensions
    {
        private const string CurrentDirectoryPattern = ".";

        private const string ParentDirectoryPattern = "..";

        public static bool IsHidden(this Win32FindData findData)
        {
            const uint flag = (uint)FileAttributes.Hidden;
            return (findData.dwFileAttributes & flag) == flag;
        }

        public static bool IsSystem(this Win32FindData findData)
        {
            const uint flag = (uint)FileAttributes.System;
            return (findData.dwFileAttributes & flag) == flag;
        }

        public static bool IsTemporary(this Win32FindData findData)
        {
            var flag = (uint)FileAttributes.Temporary;
            return (findData.dwFileAttributes & flag) == flag;
        }

        public static bool IsDirectory(this Win32FindData findData)
        {
            const uint flag = (uint)FileAttributes.Directory;
            return (findData.dwFileAttributes & flag) == flag;
        }

        public static bool IsCurrentDirectory(this Win32FindData findData)
        {
            string currentFileName = findData.cFileName;
            return CurrentDirectoryPattern.Equals(currentFileName);
        }

        public static bool IsParentDirectory(this Win32FindData findData)
        {
            string currentFileName = findData.cFileName;
            return ParentDirectoryPattern.Equals(currentFileName);
        }

        internal static bool WorkOnFolder(this Win32FindData findData, bool includeHiddenFolder, bool includeCurrentFolder)
        {
            return findData.IsDirectory()
                   && (includeHiddenFolder || !findData.IsHidden())
                   && !findData.IsSystem()
                   && !findData.IsTemporary()
                   && (includeCurrentFolder || !findData.IsCurrentDirectory())
                   && !findData.IsParentDirectory();
        }
    }
}