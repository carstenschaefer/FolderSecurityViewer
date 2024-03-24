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

namespace FSV.Console.Abstractions
{
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using FileSystem.Interop.Abstractions;
    using Models;

    public interface IExport
    {
        Task<string> ExportPermissionsAsync(DataTable permissions, IList<IAclModel> accessControlList);

        Task<string> ExportPermissionsAsync(IList<DifferenceExportItem> permissions);

        Task<string> ExportFoldersAsync(IList<FolderItem> folderList);

        Task<string> ExportOwnersAsync(IList<FolderItem> folderList);

        Task<string> ExportUserReportAsync(DataTable folders, string user, IEnumerable<string> skippedFolders);

        Task<string> ExportShareReportAsync(DataTable shares);
    }
}