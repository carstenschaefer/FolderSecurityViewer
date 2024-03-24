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

namespace FSV.Business
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using FileSystem.Interop.Abstractions;

    public sealed class UserPermissionTaskResult : IDisposable
    {
        internal UserPermissionTaskResult(DataTable result, IEnumerable<IFolderReport> exceptionFolders, bool scanCancelled)
        {
            this.Result = result ?? throw new ArgumentNullException(nameof(result));
            this.ExceptionFolders = exceptionFolders ?? throw new ArgumentNullException(nameof(exceptionFolders));
            this.ScanCancelled = scanCancelled;
        }

        public DataTable Result { get; }
        public IEnumerable<IFolderReport> ExceptionFolders { get; }
        public bool ScanCancelled { get; }

        public void Dispose()
        {
            this.Result.Dispose();
        }
    }
}