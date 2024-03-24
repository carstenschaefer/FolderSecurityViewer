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

namespace FSV.ShareServices.Interop
{
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;

    /// <summary>Share information, level 2</summary>
    /// <remarks>
    ///     Requires admin rights to work.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [NoReorder]
    internal struct SHARE_INFO_2
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string NetName;

        public SHARE_TYPE ShareType;

        [MarshalAs(UnmanagedType.LPWStr)] public string Remark;

        public uint Permissions;

        public uint MaxUsers;

        public uint CurrentUsers;

        [MarshalAs(UnmanagedType.LPWStr)] public string Path;

        [MarshalAs(UnmanagedType.LPWStr)] public string Password;
    }
}