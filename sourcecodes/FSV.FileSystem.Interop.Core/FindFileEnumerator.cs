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
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Abstractions;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    public sealed class FindFileEnumerator : IEnumerator<Win32FindDataWrapper>
    {
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;
        private readonly string path;
        private bool disposed;
        private FindFileHandle handle;

        public FindFileEnumerator(IKernel32 kernel32, IKernel32FindFile kernel32FindFile, LongPath path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
            this.path = path;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.Reset();
            this.disposed = true;
        }

        public bool MoveNext()
        {
            Win32FindData findFileData;

            if (this.handle == null)
            {
                this.handle = this.kernel32FindFile.FindFirstFile(this.path, out findFileData);
                if (this.handle.IsInvalid)
                {
                    uint error = this.kernel32.GetLastError();

                    if (error == WinError.ErrorAccessDenied)
                    {
                        this.Current = new Win32FindDataWrapper(this.kernel32, this.path, error);
                        return true;
                    }

                    this.Throw(error);
                    return false;
                }

                this.Current = new Win32FindDataWrapper(findFileData);
                return true;
            }

            while (this.kernel32FindFile.FindNextFile(this.handle, out findFileData) == 0)
            {
                uint error = this.kernel32.GetLastError();
                switch (error)
                {
                    case WinError.ErrorAccessDenied:
                        continue;

                    case WinError.ErrorNoMoreFiles:
                        return false;
                }

                this.Throw(error);
                return false;
            }

            this.Current = new Win32FindDataWrapper(findFileData);
            return true;
        }

        public void Reset()
        {
            this.Current = default;

            this.handle?.Dispose();
            this.handle = null;
        }

        public Win32FindDataWrapper Current { get; private set; }

        object IEnumerator.Current => this.Current;


        private void Throw(uint error)
        {
            if (error == 0)
            {
                return;
            }

            string errorMessage = this.kernel32.GetErrorMessage(error);

            throw error switch
            {
                WinError.ErrorFileNotFound => new FileNotFoundException(errorMessage),
                WinError.ErrorPathNotFound => new DirectoryNotFoundException(errorMessage),
                _ => new FindFileEnumeratorException(errorMessage, error)
            };
        }
    }
}