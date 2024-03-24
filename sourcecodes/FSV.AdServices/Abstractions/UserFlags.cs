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

namespace FSV.AdServices.Abstractions
{
    using System;

    [Flags]
    internal enum UserFlags
    {
        Script = 1, // 0x1
        AccountDisabled = 2, // 0x2
        HomeDirectoryRequired = 8, // 0x8
        AccountLockedOut = 16, // 0x10
        PasswordNotRequired = 32, // 0x20
        PasswordCannotChange = 64, // 0x40
        EncryptedTextPasswordAllowed = 128, // 0x80
        TempDuplicateAccount = 256, // 0x100
        NormalAccount = 512, // 0x200
        InterDomainTrustAccount = 2048, // 0x800
        WorkstationTrustAccount = 4096, // 0x1000
        ServerTrustAccount = 8192, // 0x2000
        PasswordDoesNotExpire = 65536, // 0x10000 (Also 66048 )
        MnsLogonAccount = 131072, // 0x20000
        SmartCardRequired = 262144, // 0x40000
        TrustedForDelegation = 524288, // 0x80000
        AccountNotDelegated = 1048576, // 0x100000
        UseDesKeyOnly = 2097152, // 0x200000
        DontRequirePreauth = 4194304, // 0x400000
        PasswordExpired = 8388608, // 0x800000 (Applicable only in Window 2000 and Window Server 2003)
        TrustedToAuthenticateForDelegation = 16777216, // 0x1000000
        NoAuthDataRequired = 33554432 // 0x2000000
    }
}