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

namespace FSV.ViewModel.UserReport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FileSystem.Interop.Abstractions;

    public class UserReportSkippedFoldersViewModel : SubspaceItemBase
    {
        public UserReportSkippedFoldersViewModel(IEnumerable<IFolderReport> exceptionFolders) : base("Skipped Folders", "SkippedFoldersIcon")
        {
            if (exceptionFolders == null)
            {
                throw new ArgumentNullException(nameof(exceptionFolders));
            }

            this.Folders = exceptionFolders.Select(m => m.FullName);
        }

        public IEnumerable<string> Folders { get; }
    }
}