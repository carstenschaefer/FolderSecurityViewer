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
    using CsvExporter;
    using FileSystem.Interop.Abstractions;
    using Folder;
    using Microsoft.Extensions.Logging;
    using Owner;
    using Permission;
    using Resources;
    using UserReport;

    public class Csv : ExporterBase
    {
        private readonly ExportTableGenerator exportTableGenerator;
        private readonly ILogger<Csv> logger;

        public Csv(
            IConfigurationPaths configurationPaths,
            ExportTableGenerator exportTableGenerator,
            ILogger<Csv> logger) : base(configurationPaths)
        {
            this.exportTableGenerator = exportTableGenerator ?? throw new ArgumentNullException(nameof(exportTableGenerator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Name = "CSV";
        }

        protected override string FileExtension => ".csv";

        public override Task ExportAsync(IEnumerable<PermissionReportBaseViewModel> accessControlList)
        {
            string filePath = this.FilePath;
            this.logger.LogInformation("Starting CSV export for permission report to file {CsvFile}.", filePath);

            return Task.Run(async () =>
            {
                await using CsvExportManager export = CsvExportManager.FromFile(filePath);

                // Iterates through each item in ACL & Trustees list.
                foreach (PermissionReportBaseViewModel aclItem in accessControlList)
                {
                    // During a scan, a trustee detail should not be stored as it is incomplete.
                    if (aclItem.IsWorking)
                    {
                        continue;
                    }

                    DataTable allPermissions = this.exportTableGenerator.GetPermissionsSortTable(aclItem);

                    if (aclItem is PermissionsViewModel permissions)
                        // Writes data of Trustees and ACL in worksheet.
                    {
                        export.WriteLargeData(permissions.DisplayName, allPermissions, permissions.AccessControlList, permissions.ExportDate);
                    }
                    else
                        // Writes data of Trustees and ACL in worksheet.
                    {
                        export.WriteLargeData(aclItem.DisplayName, allPermissions, Enumerable.Empty<IAclModel>(), aclItem.ExportDate);
                    }
                }
            });
        }

        public override Task ExportAsync(IEnumerable<FolderViewModel> folderList)
        {
            string filePath = this.FilePath;
            this.logger.LogInformation("Starting CSV export for folder report to file {CsvFile}.", filePath);

            return Task.Run(async () =>
            {
                await using CsvExportManager exportManager = CsvExportManager.FromFile(filePath);

                foreach (FolderViewModel folderViewModel in folderList)
                {
                    // During a scan, folders' detail should not be stored as it is incomplete.
                    if (folderViewModel.IsWorking)
                    {
                        continue;
                    }

                    using DataTable table = this.exportTableGenerator.GetFolderTable(folderViewModel.AllItems, folderViewModel.GetExportSortColumn(), folderViewModel.SortDirection);
                    exportManager.WriteLargeData(folderViewModel.DisplayName, table, ExportResource.FolderReportCaption, folderViewModel.ExportDate);
                }
            });
        }

        public override Task ExportAsync(IEnumerable<OwnerReportViewModel> ownerList)
        {
            string filePath = this.FilePath;
            this.logger.LogInformation("Starting CSV export for owner report to file {CsvFile}.", filePath);

            return Task.Run(async () =>
            {
                await using CsvExportManager exportManager = CsvExportManager.FromFile(filePath);

                foreach (OwnerReportViewModel ownerViewModel in ownerList)
                {
                    // During a scan, owners' detail should not be stored as it is incomplete.
                    if (ownerViewModel.IsWorking)
                    {
                        continue;
                    }

                    using DataTable table = this.exportTableGenerator.GetOwnerTable(ownerViewModel.AllItems, ownerViewModel.GetExportSortColumn(), ownerViewModel.SortDirection);

                    exportManager.WriteLargeData(ownerViewModel.DisplayName, table, ExportResource.OwnerReportCaption, ownerViewModel.ExportDate);
                }
            });
        }

        public override Task ExportAsync(IEnumerable<UserReportBaseViewModel> userReports)
        {
            string filePath = this.FilePath;
            this.logger.LogInformation("Starting CSV export for user report to file {CsvFile}.", filePath);

            return Task.Run(async () =>
            {
                await using CsvExportManager exportManager = CsvExportManager.FromFile(filePath);

                foreach (UserReportBaseViewModel report in userReports)
                {
                    // During a scan, an User Report detail should not be stored as it is incomplete.
                    if (report.IsWorking)
                    {
                        continue;
                    }

                    exportManager.WriteLargeData(report.DisplayName, this.exportTableGenerator.GetUserReportSortTable(report), ExportResource.UserReportCaption, report.ExportDate);
                }
            });
        }

        public override Task ExportAsync(IEnumerable<DifferentItemViewModel> models)
        {
            string filePath = this.FilePath;
            this.logger.LogInformation("Starting CSV export for differences report to file {CsvFile}.", filePath);

            return Task.Run(async () =>
            {
                await using CsvExportManager export = CsvExportManager.FromFile(filePath);

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
            });
        }

        public override async Task ExportAsync(IEnumerable<PrincipalMembershipViewModel> principalMemberships)
        {
            if (principalMemberships is null)
            {
                throw new ArgumentNullException(nameof(principalMemberships));
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.FilePath);

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
        }

        public override async Task ExportAsync(IEnumerable<GroupMembersViewModel> groupMembers)
        {
            if (groupMembers is null)
            {
                throw new ArgumentNullException(nameof(groupMembers));
            }

            await using CsvExportManager exportManager = CsvExportManager.FromFile(this.FilePath);

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
        }
    }
}