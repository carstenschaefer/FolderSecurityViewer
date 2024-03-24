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
    using System;
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;

    [NoReorder]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHARE_INFO_502
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string shi502_netname;

        public SHARE_TYPE shi502_type;

        [MarshalAs(UnmanagedType.LPWStr)] public string shi502_remark;

        public uint shi502_permissions;

        public uint shi502_max_uses;

        public uint shi502_current_uses;

        [MarshalAs(UnmanagedType.LPWStr)] public string shi502_path;

        [MarshalAs(UnmanagedType.LPWStr)] public string shi502_passwd;

        public uint shi502_reserved;

        public IntPtr shi502_security_descriptor;
    }
}