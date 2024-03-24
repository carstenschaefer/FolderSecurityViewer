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

    internal static class ErrorHelper
    {
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
        private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;

        internal static string GetErrorMessage(uint errorCode)
        {
            var lpBuffer = default(IntPtr);
            const uint languageId = 0;
            // must be zero, otherwise no error messages can be obtained (probably an ERROR_RESOURCE_LANG_NOT_FOUND)

            if (NativeMethods.FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER, IntPtr.Zero, errorCode, languageId, ref lpBuffer, 0, IntPtr.Zero) == 0)
            {
                return "Unknown error.";
            }

            return Marshal.PtrToStringAuto(lpBuffer);
        }
    }
}