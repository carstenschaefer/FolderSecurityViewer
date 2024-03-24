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

namespace FSV.ViewModel.Abstractions
{
    using System.Collections.Generic;
    using AdMembers;
    using Exporter;
    using Folder;
    using Owner;
    using Permission;
    using ShareReport;
    using UserReport;

    public interface IExportService
    {
        IEnumerable<ExporterBase> GetExporters(IEnumerable<ExportContentType> exportTypes);
        void OpenExport(ExportOpenType exportOpenType, string path);

        void ExportPermissionReports(IEnumerable<PermissionReportBaseViewModel> items);
        void ExportUserReports(IEnumerable<UserReportBaseViewModel> items);
        void ExportOwnerReports(IEnumerable<OwnerReportViewModel> items);
        void ExportFolderReports(IEnumerable<FolderViewModel> items);
        void ExportPermissionDifferenceReports(IEnumerable<DifferentItemViewModel> items);
        void ExportPrincipalMemberships(IEnumerable<PrincipalMembershipViewModel> items);
        void ExportGroupMembers(IEnumerable<GroupMembersViewModel> items);
        void ExportShareReport(SharedServerViewModel sharedServer);
    }
}