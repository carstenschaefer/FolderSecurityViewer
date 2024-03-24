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
    using FileSystem.Interop.Abstractions;
    using Folder;
    using HtmlExporter;
    using Owner;
    using Permission;
    using Resources;
    using ShareReport;
    using UserReport;

    public class Html : ExporterBase
    {
        private readonly ExportTableGenerator exportTableGenerator;

        public Html(IConfigurationPaths configurationPaths, ExportTableGenerator exportTableGenerator) : base(configurationPaths)
        {
            this.exportTableGenerator = exportTableGenerator ?? throw new ArgumentNullException(nameof(exportTableGenerator));
            this.Name = "HTML";
        }

        protected override string FileExtension => ".html";

        public override Task ExportAsync(IEnumerable<PermissionReportBaseViewModel> accessControlList)
        {
            // Initializes the thread to process the export asynchronously.
            return Task.Run(() =>
            {
                var export = new HtmlExportManager(this.FilePath);

                // Iterates through each item in ACL & Trustees list.
                foreach (PermissionReportBaseViewModel aclItem in accessControlList)
                {
                    // During a scan, a trustee detail should not be stored as it is incomplete.
                    if (aclItem.IsWorking)
                    {
                        continue;
                    }

                    DataTable allPermissions = this.exportTableGenerator.GetPermissionsSortTable(aclItem);

                    if (aclItem is PermissionsViewModel)
                    {
                        var permissions = aclItem as PermissionsViewModel;
                        // Writes data of Trustees and ACL in worksheet.
                        export.WriteLargeData(permissions.DisplayName, allPermissions, permissions.AccessControlList.ToList(), permissions.ExportDate);
                    }
                    else
                    {
                        // Writes data of Trustees and ACL in worksheet.
                        export.WriteLargeData(aclItem.DisplayName, allPermissions, Enumerable.Empty<IAclModel>(), aclItem.ExportDate); // Typecast required to fend off ambiguous call.
                    }
                }

                export.Save();
            });
        }

        public override Task ExportAsync(IEnumerable<FolderViewModel> folderList)
        {
            // Initializes the thread to process the export asynchronously.
            return Task.Run(() =>
            {
                var exportManager = new HtmlExportManager(this.FilePath);

                // Iterates through each item in Folders' list.
                foreach (FolderViewModel folderViewModel in folderList)
                {
                    // During a scan, a folder detail should not be stored as it is incomplete.
                    if (folderViewModel.IsWorking)
                    {
                        continue;
                    }

                    DataTable table = this.exportTableGenerator.GetFolderTable(folderViewModel.AllItems, folderViewModel.GetExportSortColumn(), folderViewModel.SortDirection);

                    exportManager.WriteLargeData(folderViewModel.DisplayName, table, ExportResource.FolderReportCaption, folderViewModel.ExportDate);
                }

                exportManager.Save();
            });
        }

        public override Task ExportAsync(IEnumerable<OwnerReportViewModel> ownerList)
        {
            // Initializes the thread to process the export asynchronously.
            return Task.Run(() =>
            {
                var exportManager = new HtmlExportManager(this.FilePath);

                // Iterates through each item in Owners list.
                foreach (OwnerReportViewModel ownerViewModel in ownerList)
                {
                    // During a scan, an owner detail should not be stored as it is incomplete.
                    if (ownerViewModel.IsWorking)
                    {
                        continue;
                    }

                    DataTable table = this.exportTableGenerator.GetOwnerTable(ownerViewModel.AllItems, ownerViewModel.GetExportSortColumn(), ownerViewModel.SortDirection);

                    exportManager.WriteLargeData(ownerViewModel.DisplayName, table, ExportResource.OwnerReportCaption, ownerViewModel.ExportDate);
                }

                exportManager.Save();
            });
        }

        public override async Task ExportAsync(IEnumerable<UserReportBaseViewModel> userReports)
        {
            var exportManager = new HtmlExportManager(this.FilePath);

            // Iterates through each item in User Report list.
            foreach (UserReportBaseViewModel report in userReports)
            {
                // During a scan, an User Report detail should not be stored as it is incomplete.
                if (report.IsWorking)
                {
                    continue;
                }

                exportManager.WriteLargeData(report.DisplayName, this.exportTableGenerator.GetUserReportSortTable(report), report.SkippedFolders, report.ExportDate);
            }

            ;

            await exportManager.SaveAsync();
        }

        public override Task ExportAsync(SharedServerViewModel sharedServer)
        {
            return Task.Run(() =>
            {
                this.FileName = $"{sharedServer.DisplayName} - {this.FileName}";

                DataTable dataTable = this.exportTableGenerator.GetShareReportTable(sharedServer);

                var exportManager = new HtmlExportManager(this.FilePath);

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
                var export = new HtmlExportManager(this.FilePath);

                // Iterates through each item in ACL & Trustees list.
                foreach (DifferentItemViewModel item in models)
                {
                    if (item.ExportItems == null)
                    {
                        continue;
                    }

                    // Writes data of Trustees and ACL in worksheet.
                    export.WriteLargeData(
                        item.Path,
                        item.ExportItems,
                        Enumerable.Empty<IAclModel>(),
                        DateTime.Now);
                }

                export.Save();
            });
        }

        public override async Task ExportAsync(IEnumerable<PrincipalMembershipViewModel> principalMemberships)
        {
            if (principalMemberships is null)
            {
                throw new ArgumentNullException(nameof(principalMemberships));
            }

            var exportManager = new HtmlExportManager(this.FilePath);

            foreach (PrincipalMembershipViewModel report in principalMemberships)
            {
                if (report.IsWorking)
                {
                    continue;
                }

                exportManager.WriteLargeData(
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

            var exportManager = new HtmlExportManager(this.FilePath);

            foreach (GroupMembersViewModel report in groupMembers)
            {
                if (report.IsWorking)
                {
                    continue;
                }

                exportManager.WriteLargeData(
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