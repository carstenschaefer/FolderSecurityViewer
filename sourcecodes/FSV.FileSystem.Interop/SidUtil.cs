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
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text;
    using Abstractions;
    using Core;
    using Core.Abstractions;
    using Types;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class SidUtil : ISidUtil
    {
        private readonly IAdvapi32 advapi32;
        private readonly IKernel32 kernel32;

        public SidUtil(IAdvapi32 advapi32, IKernel32 kernel32)
        {
            this.advapi32 = advapi32 ?? throw new ArgumentNullException(nameof(advapi32));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
        }

        public string GetSidString(byte[] sid)
        {
            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            // TODO: verify if this functionality can be achieved without any interop code (should be possible with .NET on-board security).
            if (!this.advapi32.ConvertSidToStringSid(sid, out IntPtr ptrSid))
            {
                throw new Win32Exception();
            }

            try
            {
                return Marshal.PtrToStringAuto(ptrSid);
            }
            finally
            {
                this.kernel32.LocalFree(ptrSid);
            }
        }

        public string GetSidString(IntPtr sidPtr)
        {
            if (!this.advapi32.ConvertSidToStringSid(sidPtr, out string sid))
            {
                return sid;
            }

            return null;
        }

        public IAclAccountModel GetAccountName(IntPtr sid)
        {
            var name = new StringBuilder();
            var cchName = (uint)name.Capacity;
            var referencedDomainName = new StringBuilder();
            var cchReferencedDomainName = (uint)referencedDomainName.Capacity;

            int err = WinError.NoError;
            bool result = this.advapi32.LookupAccountSid(IntPtr.Zero, sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out SidNameUse sidUse) != 0;

            if (!result)
            {
                err = Marshal.GetLastWin32Error();
                if (err == Constants.ErrorInsufficientBuffer)
                {
                    name.EnsureCapacity((int)cchName);
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);

                    result = this.advapi32.LookupAccountSid(IntPtr.Zero, sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out sidUse) != 0;

                    if (!result)
                    {
                        err = Marshal.GetLastWin32Error();
                    }
                }
            }

            var aclAccount = new AclAccountModel();
            if (result && (err == WinError.NoError || err == Constants.ErrorInsufficientBuffer) && !string.IsNullOrEmpty(referencedDomainName.ToString()))
            {
                aclAccount.Name = referencedDomainName + @"\" + name;
                aclAccount.NetBiosDomain = referencedDomainName.ToString();

                if (TryGetGroupType(sidUse, out AccountType accountType, out string accountTypeString))
                {
                    aclAccount.Type = accountType;
                    aclAccount.TypeString = accountTypeString;
                }
            }
            else
            {
                aclAccount.Name = name.ToString();
            }

            return aclAccount;
        }

        private static bool TryGetGroupType(SidNameUse sidUse, out AccountType accountType, out string accountTypeString)
        {
            accountType = AccountType.None;
            accountTypeString = null;

            switch (sidUse)
            {
                case SidNameUse.SidTypeUser:
                    accountType = AccountType.User;
                    accountTypeString = "User";
                    return true;
                case SidNameUse.SidTypeGroup:
                    accountType = AccountType.Group;
                    accountTypeString = "Group";
                    return true;
                case SidNameUse.SidTypeWellKnownGroup:
                    accountType = AccountType.WellknownGroup;
                    accountTypeString = "Wellknown Group";
                    return true;
                case SidNameUse.SidTypeAlias:
                    accountType = AccountType.Group;
                    accountTypeString = "Group";
                    return true;
                default:
                    return false;
            }
        }
    }
}