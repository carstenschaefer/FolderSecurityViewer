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

namespace FSV.ViewModel.Exporter
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using AdMembers;
    using Configuration.Abstractions;
    using ExcelExporter;
    using FileSystem.Interop.Abstractions;
    using Folder;
    using Owner;
    using Permission;
    using Resources;
    using ShareReport;
    using UserReport;

    public class Excel : ExporterBase
    {
        private readonly ExportTableGenerator exportTableGenerator;

        public Excel(IConfigurationPaths configurationPaths, ExportTableGenerator exportTableGenerator) : base(configurationPaths)
        {
            this.exportTableGenerator = exportTableGenerator ?? throw new ArgumentNullException(nameof(exportTableGenerator));

            this.Name = "Excel";
        }

        protected override string FileExtension => ".xlsx";

        public override Task ExportAsync(IEnumerable<PermissionReportBaseViewModel> accessControlList)
        {
            // Initializes the thread to process the export asynchronously.
            return Task.Run(() =>
            {
                // Initializes a workbook.
                var exportManager = new ExcelExportManager(this.FilePath);

                var counter = 0; // A counter that represents the name of each worksheet.

                // Iterates through each item in ACL & Trustees list.
                foreach (PermissionReportBaseViewModel aclItem in accessControlList)
                {
                    // During a scan, a trustee detail should not be stored as it is incomplete.
                    if (aclItem.IsWorking)
                    {
                        continue;
                    }

                    // Adds a new worksheet with numeric name.
                    exportManager.AddWorksheet((++counter).ToString());

                    DataTable allPermissions = this.exportTableGenerator.GetPermissionsSortTable(aclItem);

                    if (aclItem is PermissionsViewModel)
                    {
                        var permissions = aclItem as PermissionsViewModel;

                        // Writes data of Trustees and ACL in worksheet.
                        exportManager.WriteLargeData(counter.ToString(), permissions.DisplayName, allPermissions, permissions.AccessControlList, permissions.ExportDate);
                    }
                    else
                    {
                        // Writes data of Trustees and ACL in worksheet.
                        exportManager.WriteLargeData(counter.ToString(), aclItem.DisplayName, allPermissions, Enumerable.Empty<IAclModel>(), aclItem.ExportDate);
                    }
                }

                // Saves the excel file.
                exportManager.Save();
            });
        }

        public override Task ExportAsync(IEnumerable<FolderViewModel> folderList)
        {
            return Task.Run(() =>
            {
                var exportManager = new ExcelExportManager(this.FilePath);
                var counter = 0;

                foreach (FolderViewModel folderViewModel in folderList)
                {
                    if (folderViewModel.IsWorking)
                    {
                        continue;
                    }

                    exportManager.AddWorksheet((++counter).ToString());
                    DataTable table = this.exportTableGenerator.GetFolderTable(folderViewModel.AllItems, folderViewModel.GetExportSortColumn(), folderViewModel.SortDirection);

                    exportManager.WriteLargeData(counter.ToString(), folderViewModel.DisplayName, table, ExportResource.FolderReportCaption, folderViewModel.ExportDate);
                }

                exportManager.Save();
            });
        }

        public override Task ExportAsync(IEnumerable<OwnerReportViewModel> ownerList)
        {
            return Task.Run(() =>
            {
                var exportManager = new ExcelExportManager(this.FilePath);
                var counter = 0;

                foreach (OwnerReportViewModel ownerViewModel in ownerList)
                {
                    if (ownerViewModel.IsWorking)
                    {
                        continue;
                    }

                    exportManager.AddWorksheet((++counter).ToString());
                    DataTable table = this.exportTableGenerator.GetOwnerTable(ownerViewModel.AllItems, ownerViewModel.GetExportSortColumn(), ownerViewModel.SortDirection);

                    exportManager.WriteLargeData(counter.ToString(), ownerViewModel.DisplayName, table, ExportResource.OwnerReportCaption, ownerViewModel.ExportDate);
                }

                exportManager.Save();
            });
        }

        public override async Task ExportAsync(IEnumerable<UserReportBaseViewModel> userReports)
        {
            var exportManager = new ExcelExportManager(this.FilePath);
            var counter = 0;

            foreach (UserReportBaseViewModel userReport in userReports)
            {
                if (userReport.IsWorking)
                {
                    continue;
                }

                exportManager.AddWorksheet((++counter).ToString());
                exportManager.WriteLargeData(
                    counter.ToString(),
                    userReport.DisplayName,
                    this.exportTableGenerator.GetUserReportSortTable(userReport),
                    userReport.SkippedFolders,
                    userReport.ExportDate);
            }

            await exportManager.SaveAsync();
        }

        public override Task ExportAsync(SharedServerViewModel sharedServer)
        {
            return Task.Run(() =>
            {
                this.FileName = $"{sharedServer.DisplayName} - {this.FileName}";
                DataTable dataTable = this.exportTableGenerator.GetShareReportTable(sharedServer);

                var exportManager = new ExcelExportManager(this.FilePath);

                if (sharedServer.IsWorking)
                {
                    return;
                }

                exportManager.WriteLargeData(dataTable, $"{ExportResource.ShareReportCaption} - {sharedServer.DisplayName}", sharedServer.ExportDate);
                exportManager.Save();
            });
        }

        public override Task ExportAsync(IEnumerable<DifferentItemViewModel> models)
        {
            // Initializes the thread to process the export asynchronously.
            return Task.Run(() =>
            {
                var counter = 0;
                var exportManager = new ExcelExportManager(this.FilePath);

                // Iterates through each item in ACL & Trustees list.
                foreach (DifferentItemViewModel item in models)
                {
                    counter++;
                    if (item.ExportItems == null)
                    {
                        continue;
                    }

                    // Writes data of Trustees and ACL in worksheet.
                    exportManager.WriteLargeData(
                        counter.ToString(),
                        item.Path,
                        item.ExportItems,
                        Enumerable.Empty<IAclModel>(),
                        DateTime.Now);
                }

                exportManager.Save();
            });
        }

        public override async Task ExportAsync(IEnumerable<PrincipalMembershipViewModel> principalMemberships)
        {
            if (principalMemberships is null)
            {
                throw new ArgumentNullException(nameof(principalMemberships));
            }

            var counter = 0;
            var exportManager = new ExcelExportManager(this.FilePath);

            foreach (PrincipalMembershipViewModel report in principalMemberships)
            {
                if (report.IsWorking)
                {
                    continue;
                }

                exportManager.WriteLargeData(
                    (++counter).ToString(),
                    string.Empty,
                    this.exportTableGenerator.GetPrincipalMembershipDataTable(report),
                    report.DisplayName,
                    report.ExportDate);
            }

            ;

            await exportManager.SaveAsync();
        }

        public override async Task ExportAsync(IEnumerable<GroupMembersViewModel> groupMembers)
        {
            if (groupMembers is null)
            {
                throw new ArgumentNullException(nameof(groupMembers));
            }

            var counter = 0;
            var exportManager = new ExcelExportManager(this.FilePath);

            foreach (GroupMembersViewModel report in groupMembers)
            {
                if (report.IsWorking)
                {
                    continue;
                }

                exportManager.WriteLargeData(
                    (++counter).ToString(),
                    string.Empty,
                    this.exportTableGenerator.GetGroupMembersDataTable(report),
                    report.DisplayName,
                    report.ExportDate);
            }

            ;

            await exportManager.SaveAsync();
        }
    }
}