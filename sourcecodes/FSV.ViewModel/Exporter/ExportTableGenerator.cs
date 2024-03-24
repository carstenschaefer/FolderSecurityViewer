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
    using Abstractions;
    using AdMembers;
    using Common;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Permission;
    using Resources;
    using ShareReport;
    using ShareServices.Abstractions;
    using UserReport;

    public class ExportTableGenerator
    {
        private readonly IConfigurationManager configurationManager;

        public ExportTableGenerator(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        internal DataTable GetPrincipalMembershipDataTable(PrincipalMembershipViewModel principalMembership)
        {
            if (principalMembership is null)
            {
                throw new ArgumentNullException(nameof(principalMembership));
            }

            return principalMembership.GetExportTable();
        }

        internal DataTable GetGroupMembersDataTable(GroupMembersViewModel groupMember)
        {
            if (groupMember is null)
            {
                throw new ArgumentNullException(nameof(groupMember));
            }

            return groupMember.GetExportTable();
        }

        internal DataTable GetFolderTable(IEnumerable<FolderItemViewModel> folders, string sortProperty, SortOrder sortDirection)
        {
            if (folders is null)
            {
                throw new ArgumentNullException(nameof(folders));
            }

            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportFolder configReport = rootReport.Folder;

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

            if (!string.IsNullOrEmpty(sortProperty))
            {
                folders = sortDirection == SortOrder.Ascending ? folders.OrderBy(sortProperty) : folders.OrderByDescending(sortProperty);
            }

            foreach (FolderItemViewModel item in folders)
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

        internal DataTable GetOwnerTable(IEnumerable<FolderItemViewModel> owners, string sortProperty, SortOrder sortDirection)
        {
            if (owners is null)
            {
                throw new ArgumentNullException(nameof(owners));
            }

            var table = new DataTable();

            table.Columns.Add(OwnerReportResource.FolderCaption);
            table.Columns.Add(OwnerReportResource.CompleteNameCaption);
            table.Columns.Add(OwnerReportResource.OwnerCaption);

            if (!string.IsNullOrEmpty(sortProperty))
            {
                owners = sortDirection == SortOrder.Ascending ? owners.OrderBy(sortProperty) : owners.OrderByDescending(sortProperty);
            }

            foreach (FolderItemViewModel item in owners)
            {
                DataRow row = table.NewRow();
                row[OwnerReportResource.FolderCaption] = item.Name;
                row[OwnerReportResource.CompleteNameCaption] = item.FullName;

                row[OwnerReportResource.OwnerCaption] = item.Owner;

                table.Rows.Add(row);
            }

            return table;
        }

        internal DataTable GetPermissionsSortTable(PermissionReportBaseViewModel permissionReport)
        {
            if (permissionReport is null)
            {
                throw new ArgumentNullException(nameof(permissionReport));
            }

            DataTable allPermissions = permissionReport.AllPermissions;

            if (permissionReport is ISortable sortable && !string.IsNullOrEmpty(sortable.SortColumn))
            {
                DataRow[] rows = permissionReport.AllPermissions.Select(string.Empty, sortable.GetExportSortColumn() + " " + sortable.SortDirection.ToShortString());
                allPermissions = rows.CopyToDataTable();
            }

            return allPermissions;
        }

        internal DataTable GetUserReportSortTable(UserReportBaseViewModel userReport)
        {
            if (userReport is null)
            {
                throw new ArgumentNullException(nameof(userReport));
            }

            if (userReport.AllFolders is null || userReport.AllFolders.Rows.Count == 0)
            {
                return new DataTable();
            }

            DataTable allFolders = userReport.AllFolders;

            if (userReport is ISortable sortable && !string.IsNullOrEmpty(sortable.SortColumn))
            {
                DataRow[] rows = allFolders.Select(string.Empty, sortable.GetExportSortColumn() + " " + sortable.SortDirection.ToShortString());
                allFolders = rows.CopyToDataTable();
            }

            return allFolders;
        }

        internal DataTable GetShareReportTable(SharedServerViewModel server)
        {
            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            var dataTable = new DataTable();

            // Not using column names from Resource because exporters use labels from resource file already.
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Path", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("MaxUsers", typeof(uint));
            dataTable.Columns.Add("ClientConnections", typeof(int));
            dataTable.Columns.Add("Trustees", typeof(DataTable));

            foreach (ShareViewModel share in server.Shares)
            {
                Share shareDetail = server.GetShareDetail(share.DisplayName);
                DataRow row = dataTable.NewRow();

                row.SetField(0, share.DisplayName);
                row.SetField(1, shareDetail.Path);
                row.SetField(2, shareDetail.Description);
                row.SetField(3, shareDetail.MaxUsers);
                row.SetField(4, shareDetail.ClientConnections);

                var trusteesTable = new DataTable();
                trusteesTable.Columns.Add(ExportResource.ShareReportColumnNameCaption, typeof(string));
                trusteesTable.Columns.Add(ExportResource.ShareReportColumnPermissionCaption, typeof(string));
                trusteesTable.Columns.Add(ExportResource.ShareReportColumnSidTypeCaption, typeof(int));
                trusteesTable.Columns.Add(ExportResource.ShareReportColumnWellKnownSidCaption, typeof(bool));

                foreach (ShareTrustee trustee in shareDetail.Trustees)
                {
                    DataRow trusteeRow = trusteesTable.NewRow();
                    trusteeRow.SetField(0, trustee.Name);
                    trusteeRow.SetField(1, $"{trustee.Permission.Access}, {trustee.Permission.Rights}");
                    trusteeRow.SetField(2, trustee.SidType);
                    trusteeRow.SetField(3, trustee.WellKnownSid);

                    trusteesTable.Rows.Add(trusteeRow);
                }

                row.SetField(5, trusteesTable);

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}