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

namespace FSV.FileSystem.Interop.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text;
    using Abstractions;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
        Justification = "Reviewed. Suppression is OK here.")]
    internal static class Advapi32
    {
        [DllImport("advapi32.dll", EntryPoint = "LookupAccountSid", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int LookupAccountSid(
            IntPtr lpSystemName,
            IntPtr sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder referencedDomainName,
            ref uint cchReferencedDomainName,
            out SidNameUse peUse);

        [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfo", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetNamedSecurityInfo(
            string pObjectName,
            SeObjectType objectType,
            SecurityInformation securityInfo,
            [Out] IntPtr pSidOwner,
            out IntPtr pSidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor);

        [DllImport("advapi32", EntryPoint = "ConvertSidToStringSid", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(
            [MarshalAs(UnmanagedType.LPArray)] byte[] pSID, out IntPtr ptrSid);

        [DllImport("advapi32", EntryPoint = "ConvertSidToStringSid", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

        [DllImport("advapi32.dll", EntryPoint = "GetAclInformation", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetAclInformation(
            IntPtr pAcl, IntPtr pAclInformation, uint nAclInformationLength, AclInformationClass dwAclInformationClass);

        [DllImport("advapi32.dll", EntryPoint = "GetAclInformation", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetAclInformation(
            IntPtr pAcl,
            ref AclRevisionInformation pAclInformation,
            uint nAclInformationLength,
            AclInformationClass dwAclInformationClass);

        [DllImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorDacl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetSecurityDescriptorDacl(
            IntPtr pSecurityDescriptor, out bool bDaclPresent, out IntPtr pDacl, out bool bDaclDefaulted);

        [DllImport("advapi32.dll", EntryPoint = "GetAclInformation", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetAclInformation(
            IntPtr pAcl,
            ref AclSizeInformation pAclInformation,
            uint nAclInformationLength,
            AclInformationClass dwAclInformationClass);

        [DllImport("advapi32.dll", EntryPoint = "GetAce", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetAce(IntPtr aclPtr, int aceIndex, out IntPtr acePtr);

        [DllImport("advapi32.dll", EntryPoint = "GetLengthSid", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetLengthSid(IntPtr pSid);
    }
}