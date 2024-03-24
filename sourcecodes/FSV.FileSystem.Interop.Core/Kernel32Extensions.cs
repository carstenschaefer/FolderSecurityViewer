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
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Abstractions;

    public static class Kernel32Extensions
    {
        public static string GetErrorMessage(this IKernel32 kernel32, uint errorCode, CultureInfo culture = null)
        {
            if (kernel32 == null)
            {
                throw new ArgumentNullException(nameof(kernel32));
            }

            IntPtr buffer = IntPtr.Zero;
            const int languageId = 0; // Let the system lookup the current LANGID, or use neutral language; see https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-formatmessage
            const uint flags = Constants.FormatMessageFromSystem | Constants.FormatMessageAllocateBuffer;

            uint result = Kernel32.FormatMessage(flags, IntPtr.Zero, errorCode, languageId, ref buffer, 0, IntPtr.Zero);
            if (result == 0)
            {
                return "Unknown error.";
            }

            try
            {
                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                kernel32.LocalFree(buffer);
            }
        }
    }
}