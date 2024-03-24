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

namespace FSV.Business.Worker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop.Abstractions;

    public class FolderWorker : BackgroundWorker
    {
        private readonly string _scanDirectory;
        private readonly IConfigurationManager configurationManager;
        private readonly IDirectoryFolderEnumerator directoryFolderEnumerator;
        private int _folderCount;

        public FolderWorker(
            IConfigurationManager configurationManager,
            IDirectoryFolderEnumerator directoryFolderEnumerator,
            string scanDirectory)
        {
            if (string.IsNullOrWhiteSpace(scanDirectory))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(scanDirectory));
            }

            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.directoryFolderEnumerator = directoryFolderEnumerator ?? throw new ArgumentNullException(nameof(directoryFolderEnumerator));
            this._scanDirectory = scanDirectory;

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportFolder configFolder = rootReport.Folder;

            const string ownerFilter = null;
            var options = new FolderEnumeratorOptions(
                configFolder.IncludeSubFolder, configFolder.IncludeHiddenFolder, configFolder.IncludeCurrentFolder,
                configFolder.IncludeFileCount,
                configFolder.IncludeSubFolderFileCount,
                configFolder.Owner, ownerFilter);
            IEnumerable<IFolderReport> folderReports = this.directoryFolderEnumerator.GetStructure(this._scanDirectory, options, this.Increment, () => this.CancellationPending);
            e.Result = folderReports.ToList();

            base.OnDoWork(e);
        }

        private void Increment()
        {
            this._folderCount++;
            if (this._folderCount % 9 == 0)
            {
                this.ReportProgress(this._folderCount);
            }
        }
    }
}