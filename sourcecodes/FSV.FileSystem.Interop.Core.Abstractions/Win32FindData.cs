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

namespace FSV.FileSystem.Interop.Core.Abstractions
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct Win32FindData
    {
        public readonly uint dwFileAttributes;

        public readonly FileTime ftCreationTime;

        public readonly FileTime ftLastAccessTime;

        public readonly FileTime ftLastWriteTime;

        public readonly uint nFileSizeHigh;

        public readonly uint nFileSizeLow;

        public readonly uint dwReserved0;

        public readonly uint dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.MaxPath)]
        public readonly string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public readonly string cAlternateFileName;

        public readonly uint dwFileType;

        public readonly uint dwCreatorType;

        public readonly uint wFinderFlags;
    }
}