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
    using System.Text;
    using Abstractions;

    public sealed class Advapi32Wrapper : IAdvapi32
    {
        public int LookupAccountSid(
            IntPtr lpSystemName, IntPtr sid, StringBuilder lpName, ref uint cchName, StringBuilder referencedDomainName, ref uint cchReferencedDomainName, out SidNameUse peUse)
        {
            return Advapi32.LookupAccountSid(lpSystemName, sid, lpName, ref cchName, referencedDomainName, ref cchReferencedDomainName, out peUse);
        }

        public uint GetNamedSecurityInfo(
            string pObjectName, SeObjectType objectType, SecurityInformation securityInfo, IntPtr pSidOwner, out IntPtr pSidGroup, out IntPtr pDacl, out IntPtr pSacl, out IntPtr pSecurityDescriptor)
        {
            return Advapi32.GetNamedSecurityInfo(pObjectName, objectType, securityInfo, pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);
        }

        public bool ConvertSidToStringSid(
            byte[] pSID, out IntPtr ptrSid)
        {
            return Advapi32.ConvertSidToStringSid(pSID, out ptrSid);
        }

        public bool ConvertSidToStringSid(
            IntPtr pSid, out string strSid)
        {
            return Advapi32.ConvertSidToStringSid(pSid, out strSid);
        }

        public bool GetAclInformation(
            IntPtr pAcl, IntPtr pAclInformation, uint nAclInformationLength, AclInformationClass dwAclInformationClass)
        {
            return Advapi32.GetAclInformation(pAcl, pAclInformation, nAclInformationLength, dwAclInformationClass);
        }

        public bool GetAclInformation(
            IntPtr pAcl, ref AclRevisionInformation pAclInformation, uint nAclInformationLength, AclInformationClass dwAclInformationClass)
        {
            return Advapi32.GetAclInformation(pAcl, ref pAclInformation, nAclInformationLength, dwAclInformationClass);
        }

        public bool GetSecurityDescriptorDacl(
            IntPtr pSecurityDescriptor, out bool bDaclPresent, out IntPtr pDacl, out bool bDaclDefaulted)
        {
            return Advapi32.GetSecurityDescriptorDacl(pSecurityDescriptor, out bDaclPresent, out pDacl, out bDaclDefaulted);
        }

        public bool GetAclInformation(
            IntPtr pAcl, ref AclSizeInformation pAclInformation, uint nAclInformationLength, AclInformationClass dwAclInformationClass)
        {
            return Advapi32.GetAclInformation(pAcl, ref pAclInformation, nAclInformationLength, dwAclInformationClass);
        }

        public bool GetAce(IntPtr aclPtr, int aceIndex, out IntPtr acePtr)
        {
            return Advapi32.GetAce(aclPtr, aceIndex, out acePtr);
        }

        public int GetLengthSid(IntPtr pSid)
        {
            return Advapi32.GetLengthSid(pSid);
        }
    }
}