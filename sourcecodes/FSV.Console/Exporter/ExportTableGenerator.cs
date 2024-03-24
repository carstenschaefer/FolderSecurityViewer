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
    using Abstractions;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Models;
    using Resources;

    public class ExportTableGenerator : IExportTableGenerator
    {
        private readonly IConfigurationManager configurationManager;

        public ExportTableGenerator(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public DataTable GetFolderTable(IEnumerable<FolderItem> folders)
        {
            ReportFolder configReport = this.configurationManager.ConfigRoot.Report.Folder;

            var table = new DataTable();

            table.Columns.Add(FolderReportResource.FolderCaption);
            table.Columns.Add(FolderReportResource.CompleteNameCaption);

            if (configReport.Owner)
            {
                table.Columns.Add(FolderReportResource.OwnerCaption);
            }

            if (configReport.IncludeFileCount)
            {
                table.Columns.Add(FolderReportResource.FileCountCaption);
                table.Columns.Add(FolderReportResource.SizeCaption);
            }

            if (configReport.IncludeSubFolderFileCount)
            {
                table.Columns.Add(FolderReportResource.FileCountSumCaption);
                table.Columns.Add(FolderReportResource.SizeSumCaption);
            }

            foreach (FolderItem item in folders)
            {
                DataRow row = table.NewRow();
                row[FolderReportResource.FolderCaption] = item.Name;
                row[FolderReportResource.CompleteNameCaption] = item.FullName;

                if (configReport.Owner)
                {
                    row[FolderReportResource.OwnerCaption] = item.Owner;
                }

                if (configReport.IncludeFileCount)
                {
                    row[FolderReportResource.FileCountCaption] = item.FileCount;
                    row[FolderReportResource.SizeCaption] = item.SizeText;
                }

                if (configReport.IncludeSubFolderFileCount)
                {
                    row[FolderReportResource.FileCountSumCaption] = item.FileCountWithSubFolders;
                    row[FolderReportResource.SizeSumCaption] = item.SizeWithSubFoldersText;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        public DataTable GetOwnerTable(IEnumerable<FolderItem> owners)
        {
            var table = new DataTable();

            table.Columns.Add(OwnerReportResource.FolderCaption);
            table.Columns.Add(OwnerReportResource.CompleteNameCaption);
            table.Columns.Add(OwnerReportResource.OwnerCaption);

            foreach (FolderItem item in owners)
            {
                DataRow row = table.NewRow();
                row[OwnerReportResource.FolderCaption] = item.Name;
                row[OwnerReportResource.CompleteNameCaption] = item.FullName;

                row[OwnerReportResource.OwnerCaption] = item.Owner;

                table.Rows.Add(row);
            }

            return table;
        }
    }
}