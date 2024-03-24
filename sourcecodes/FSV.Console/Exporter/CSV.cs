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

namespace FSV.Console.Exporter
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using CsvExporter;
    using FileSystem.Interop.Abstractions;
    using Models;
    using Properties;

    public class Csv : IExport
    {
        private const string Extension = ".csv";
        private readonly ExportFilePath exportFilePath;

        private readonly IExportTableGenerator exportTableGenerator;
        private readonly string filePath;
        private readonly string scannedDirectory;

        public Csv(
            IExportTableGenerator exportTableGenerator,
            ExportFilePath exportFilePath,
            string scannedDirectory)
        {
            this.exportTableGenerator = exportTableGenerator ?? throw new ArgumentNullException(nameof(exportTableGenerator));
            this.exportFilePath = exportFilePath ?? throw new ArgumentNullException(nameof(exportFilePath));
            this.scannedDirectory = scannedDirectory ?? throw new ArgumentException(Resources.ValueNullOrWhitespaceException);

            this.filePath = this.exportFilePath.GetFilePath(Extension);
        }

        public async Task<string> ExportFoldersAsync(IList<FolderItem> folderList)
        {
            if (folderList?.Count == 0)
            {
                return null;
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.filePath);
            using DataTable table = this.exportTableGenerator.GetFolderTable(folderList);
            exportManager.WriteLargeData(this.scannedDirectory, table, Resources.FolderReportCaption, DateTime.Now);

            return this.filePath;
        }

        public async Task<string> ExportOwnersAsync(IList<FolderItem> folderList)
        {
            if (folderList?.Count == 0)
            {
                return null;
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.filePath);
            using DataTable table = this.exportTableGenerator.GetOwnerTable(folderList);
            exportManager.WriteLargeData(this.scannedDirectory, table, Resources.OwnerReportCaption, DateTime.Now);

            return this.filePath;
        }

        public async Task<string> ExportPermissionsAsync(DataTable permissions, IList<IAclModel> accessControlList)
        {
            if (permissions?.Rows.Count == 0)
            {
                return null;
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.filePath);
            exportManager.WriteLargeData(this.scannedDirectory, permissions, accessControlList, DateTime.Now);

            return this.filePath;
        }

        public async Task<string> ExportUserReportAsync(DataTable folders, string user, IEnumerable<string> _)
        {
            if (folders?.Rows.Count == 0)
            {
                return null;
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.filePath);
            string title = string.Format(Resources.UserInDirectoryText, user, this.scannedDirectory);
            exportManager.WriteLargeData(title, folders, Resources.UserPermissionReportCaption, DateTime.Now);

            return this.filePath;
        }

        public async Task<string> ExportPermissionsAsync(IList<DifferenceExportItem> differenceItems)
        {
            if (differenceItems?.Count == 0)
            {
                return null;
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.filePath);

            foreach (DifferenceExportItem item in differenceItems)
            {
                if (item.Result == null)
                {
                    continue;
                }

                exportManager.WriteLargeData(
                    item.Path,
                    item.Result,
                    Enumerable.Empty<IAclModel>(),
                    DateTime.Now);
            }

            return this.filePath;
        }

        public async Task<string> ExportShareReportAsync(DataTable shares)
        {
            return await Task.FromResult(string.Empty);
        }
    }
}