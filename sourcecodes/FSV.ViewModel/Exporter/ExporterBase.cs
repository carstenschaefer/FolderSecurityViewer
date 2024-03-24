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
    using System.IO;
    using System.Threading.Tasks;
    using AdMembers;
    using Configuration.Abstractions;
    using Folder;
    using Owner;
    using Permission;
    using ShareReport;
    using UserReport;

    public abstract class ExporterBase
    {
        private readonly IConfigurationPaths configurationPaths;
        private string _filePath;

        protected ExporterBase(IConfigurationPaths configurationPaths)
        {
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
        }

        protected abstract string FileExtension { get; }
        protected string FileName { get; set; } = DateTime.Now.ToString("yyyy-MMM-dd_HH-mm-ss");

        public string FilePath => string.IsNullOrEmpty(this._filePath) ? this._filePath = this.MakeFilePath() : this._filePath;

        public string Name { get; protected set; }

        public abstract Task ExportAsync(IEnumerable<PermissionReportBaseViewModel> accessControlList);
        public abstract Task ExportAsync(IEnumerable<FolderViewModel> folderList);
        public abstract Task ExportAsync(IEnumerable<OwnerReportViewModel> folderList);
        public abstract Task ExportAsync(IEnumerable<UserReportBaseViewModel> userReports);
        public abstract Task ExportAsync(IEnumerable<DifferentItemViewModel> models);

        public virtual Task ExportAsync(SharedServerViewModel sharedServer)
        {
            return Task.CompletedTask;
        }

        public abstract Task ExportAsync(IEnumerable<PrincipalMembershipViewModel> memberships);
        public abstract Task ExportAsync(IEnumerable<GroupMembersViewModel> groupMembers);

        private string MakeFilePath()
        {
            string fileName = Path.ChangeExtension(this.FileName, this.FileExtension);
            return this.configurationPaths.GetExportFilePath(fileName);
        }
    }
}