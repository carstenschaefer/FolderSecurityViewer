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
    using System.Text;
    using Abstractions;
    using JetBrains.Annotations;

    public static class NetShareFunctions
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int NO_ERROR = 0;

        internal static SHARE_INFO_2 GetInfo([NotNull] string serverName, [NotNull] string shareNetName, out SharePermissionEntry[] permissions)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverName));
            }

            if (string.IsNullOrWhiteSpace(shareNetName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(shareNetName));
            }

            permissions = null;
            SHARE_INFO_2 sharedInfo;
            IntPtr bufptr;
            int err = NetApi32.NetShareGetInfo(serverName, shareNetName, 502, out bufptr);

            if (err == (int)NetError.NERR_Success)
            {
                var shareInfo = (SHARE_INFO_502)Marshal.PtrToStructure(bufptr, typeof(SHARE_INFO_502));
                sharedInfo = new SHARE_INFO_2
                {
                    NetName = shareInfo.shi502_netname,
                    Remark = shareInfo.shi502_remark,
                    ShareType = shareInfo.shi502_type,
                    MaxUsers = shareInfo.shi502_max_uses,
                    CurrentUsers = shareInfo.shi502_current_uses,
                    Path = shareInfo.shi502_path,
                    Password = shareInfo.shi502_passwd
                };

                if (shareInfo.shi502_security_descriptor != IntPtr.Zero)
                {
                    IntPtr pAcl = IntPtr.Zero;
                    bool bDaclPresent;
                    bool bDaclDefaulted;
                    if (!Advapi32.GetSecurityDescriptorDacl(shareInfo.shi502_security_descriptor, out bDaclPresent, ref pAcl, out bDaclDefaulted))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new ShareLibException(errorCode, "Can not get security descriptor.");
                    }

                    if (bDaclPresent)
                    {
                        var AclSize = new ACL_SIZE_INFORMATION();
                        if (!Advapi32.GetAclInformation(pAcl, ref AclSize, (uint)Marshal.SizeOf(typeof(ACL_SIZE_INFORMATION)), ACL_INFORMATION_CLASS.AclSizeInformation))
                        {
                            throw new ShareLibException(Marshal.GetLastWin32Error(), "Can not get ACL information.");
                        }

                        permissions = new SharePermissionEntry[AclSize.AceCount];

                        for (uint i = 0; i < AclSize.AceCount; i++)
                        {
                            IntPtr pAce;
                            err = Advapi32.GetAce(pAcl, i, out pAce);
                            var ace = (ACCESS_ALLOWED_ACE)Marshal.PtrToStructure(pAce, typeof(ACCESS_ALLOWED_ACE));
                            var iter = (IntPtr)((long)pAce + (long)Marshal.OffsetOf(typeof(ACCESS_ALLOWED_ACE), "SidStart"));
                            byte[] bSID = null;
                            int size = Advapi32.GetLengthSid(iter);
                            bSID = new byte[size];
                            Marshal.Copy(iter, bSID, 0, size);
                            IntPtr ptrSid;

                            if (!Advapi32.ConvertSidToStringSid(bSID, out ptrSid))
                            {
                                throw new ShareLibException(Marshal.GetLastWin32Error(), "Can not get SID.");
                            }

                            string strSID = Marshal.PtrToStringAuto(ptrSid);
                            var name = new StringBuilder();
                            var cchName = (uint)name.Capacity;
                            var referencedDomainName = new StringBuilder();
                            var cchReferencedDomainName = (uint)referencedDomainName.Capacity;

                            if (!Advapi32.LookupAccountSid(null, bSID, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out SID_NAME_USE _))
                            {
                                err = Marshal.GetLastWin32Error();
                                if (err == ERROR_INSUFFICIENT_BUFFER)
                                {
                                    name.EnsureCapacity((int)cchName);
                                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                                    err = NO_ERROR;

                                    if (!Advapi32.LookupAccountSid(null, bSID, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out SID_NAME_USE _))
                                    {
                                        throw new ShareLibException(Marshal.GetLastWin32Error(), "Can not get account information.");
                                    }
                                }
                            }

                            permissions[i] = new SharePermissionEntry(
                                (referencedDomainName.Length != 0 ? referencedDomainName + "\\" : "") +
                                name,
                                strSID,
                                ace.Mask,
                                ace.Header.AceType != 1);
                        }
                    }
                }
            }
            else
            {
                throw new ShareLibException(err, "Can not get share information. Error code: " + err);
            }

            return sharedInfo;
        }
    }
}