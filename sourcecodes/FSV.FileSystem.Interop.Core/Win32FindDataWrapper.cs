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
    using Abstractions;

    public sealed class Win32FindDataWrapper
    {
        private readonly uint error;
        private readonly Win32FindData findFileData;
        private readonly IKernel32 kernel32;

        internal Win32FindDataWrapper(IKernel32 kernel32, uint error)
        {
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.error = error;
        }

        internal Win32FindDataWrapper(IKernel32 kernel32, string path, uint error) : this(kernel32, error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            this.Path = path;
        }

        public Win32FindDataWrapper(Win32FindData findFileData)
        {
            this.findFileData = findFileData;
        }

        public string Path { get; }
        public bool IsValid => this.error == WinError.NoError;

        public bool IsAccessDenied()
        {
            return this.error == WinError.ErrorAccessDenied;
        }

        public string GetErrorMessage()
        {
            return this.kernel32.GetErrorMessage(this.error);
        }

        public Win32FindData GetWin32FindData(bool allowThrow = true)
        {
            if (this.error == WinError.NoError)
            {
                return this.findFileData;
            }

            if (allowThrow)
            {
                string errorMessage = this.GetErrorMessage();

                switch (this.error)
                {
                    case WinError.ErrorAccessDenied:
                        throw new UnauthorizedAccessException(errorMessage);
                }
            }

            return default;
        }
    }
}