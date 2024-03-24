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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class NetShare
    {
        private const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;

        /// <summary>Get more detailed information about all shared items</summary>
        /// <param name="serverName">server name</param>
        /// <returns>collection of descriptions</returns>
        /// <remarks>Administration rights are required</remarks>
        public static IEnumerable<ShareInfo2> Enum2(string serverName)
        {
            var ShareInfos = new List<ShareInfo2>();
            uint entriesRead = 0;
            uint totalEntries = 0;
            uint resume_handle = 0;
            int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_2));
            IntPtr bufPtr = IntPtr.Zero;
            var server = new StringBuilder(serverName);
            int ret = NetApi32.NetShareEnum(server, 2, ref bufPtr, MAX_PREFERRED_LENGTH, ref entriesRead, ref totalEntries, ref resume_handle);
            if (ret == (int)NetError.NERR_Success)
            {
                IntPtr currentPtr = bufPtr;
                for (var i = 0; i < entriesRead; i++)
                {
                    var shi2 = (SHARE_INFO_2)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_2));
                    ShareInfos.Add(new ShareInfo2
                    {
                        NetName = shi2.NetName,
                        Description = shi2.Remark,
                        ShareType = shi2.ShareType,
                        MaxUsers = shi2.MaxUsers,
                        CurrentUses = shi2.CurrentUsers,
                        Path = shi2.Path,
                        Password = shi2.Password
                    });
                    currentPtr = currentPtr + nStructSize; //new IntPtr(currentPtr.ToInt32() + nStructSize);
                }

                NetApi32.NetApiBufferFree(bufPtr);
                return ShareInfos.ToArray();
            }

            throw new Exception();
        }
    }
}