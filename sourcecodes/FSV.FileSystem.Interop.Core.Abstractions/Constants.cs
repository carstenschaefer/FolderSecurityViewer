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
    using System;

    public static class Constants
    {
        public const uint FormatMessageAllocateBuffer = 0x00000100;

        public const uint FormatMessageFromSystem = 0x00001000;


        public const uint ErrorInsufficientBuffer = 122;

        public const long InvalidFileAttributes = -1;

        public const int MaxPath = 260;

        public const string LongPathPrefix = @"\\?\";
        public const string LongUncPathPrefix = @"\\?\UNC\";

        public static readonly IntPtr InvalidHandle = new(-1);

        public static readonly uint FileAttributeDirectory = 0x00000010;
    }
}