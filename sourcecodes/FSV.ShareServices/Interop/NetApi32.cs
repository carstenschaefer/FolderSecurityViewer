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
    using System.Security;
    using System.Text;

    public static class NetApi32
    {
        [DllImport("Netapi32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern int NetShareGetInfo(
            [MarshalAs(UnmanagedType.LPWStr)] string serverName,
            [MarshalAs(UnmanagedType.LPWStr)] string netName,
            uint level,
            out IntPtr bufPtr);

        [DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern uint NetServerEnum(
            string ServerName,
            uint level,
            ref IntPtr bufPtr,
            uint prefMaxLen,
            out uint entriesRead,
            out uint totalEntries,
            uint servertype,
            [MarshalAs(UnmanagedType.LPWStr)] string domain, // null for login domain
            out uint resume_handle
        );

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern int NetShareEnum(
            StringBuilder ServerName,
            uint level,
            ref IntPtr bufPtr,
            uint prefmaxlen,
            ref uint entriesread,
            ref uint totalentries,
            ref uint resume_handle
        );
    }
}