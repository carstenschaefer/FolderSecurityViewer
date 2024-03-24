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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using Abstractions;
    using Core;
    using Core.Abstractions;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class Owner : IOwnerService
    {
        private readonly IAdvapi32 advapi32;
        private readonly IKernel32 kernel32;

        public Owner(IAdvapi32 advapi32, IKernel32 kernel32)
        {
            this.advapi32 = advapi32 ?? throw new ArgumentNullException(nameof(advapi32));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
        }

        public string GetNative(LongPath path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            IntPtr sidOwnerPtr = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());
            IntPtr pSecurityDescriptor = IntPtr.Zero;

            var buffer = new StringBuilder();

            try
            {
                string shortPath = this.GetShortPathName(path);

                uint errorReturn = this.advapi32.GetNamedSecurityInfo(shortPath, SeObjectType.SeFileObject,
                    SecurityInformation.OwnerSecurityInformation, sidOwnerPtr,
                    out IntPtr _, out IntPtr _, out IntPtr _,
                    out pSecurityDescriptor);

                if (errorReturn != 0)
                {
                    uint lastError = this.kernel32.GetLastError();
                    string errorMessage = this.kernel32.GetErrorMessage(lastError);
                    throw new OwnerLookupException($"Failed to lookup owner SID for path {path} due to an error ({lastError}). " + errorMessage);
                }

                IntPtr pSid = Marshal.ReadIntPtr(sidOwnerPtr);

                uint accountLength = 0;
                uint domainLength = 0;
                this.advapi32.LookupAccountSid(IntPtr.Zero, pSid, null, ref accountLength, null, ref domainLength, out SidNameUse _);
                var account = new StringBuilder((int)accountLength);
                var domain = new StringBuilder((int)domainLength);

                if (this.advapi32.LookupAccountSid(IntPtr.Zero, pSid, account, ref accountLength, domain, ref domainLength, out SidNameUse _) == 0)
                {
                    uint lastError = this.kernel32.GetLastError();
                    string errorMessage = this.kernel32.GetErrorMessage(lastError);
                    throw new OwnerLookupException($"Failed to lookup owner SID for path {path} due to an error ({lastError}). " + errorMessage);
                }

                buffer.Append(domain);
                buffer.Append(@"\");
                buffer.Append(account);
            }
            finally
            {
                Marshal.FreeHGlobal(sidOwnerPtr);
                this.kernel32.LocalFree(pSecurityDescriptor);
            }

            return buffer.ToString();
        }

        private string GetShortPathName(LongPath longPath)
        {
            uint sz = this.kernel32.GetShortPathName(longPath, null, 0);
            if (sz == 0)
            {
                throw new Win32Exception();
            }

            var sb = new StringBuilder(Convert.ToInt32(sz) + 1);
            sz = this.kernel32.GetShortPathName(longPath, sb, Convert.ToUInt32(sb.Capacity));
            if (sz == 0)
            {
                throw new Win32Exception();
            }

            return sb.ToString();
        }

        public static string GetManaged(string path)
        {
            var dInfo = new DirectoryInfo(path);
            DirectorySecurity dirSec = dInfo.GetAccessControl(AccessControlSections.Owner);
            IdentityReference ownerNtAccount = dirSec.GetOwner(typeof(NTAccount));
            return ownerNtAccount.ToString();

            //var idRef = dirSec.GetOwner(typeof(SecurityIdentifier));
            //var ntAccount = idRef.Translate(typeof(NTAccount)) as NTAccount;
            //return ntAccount.Value;
        }
    }
}