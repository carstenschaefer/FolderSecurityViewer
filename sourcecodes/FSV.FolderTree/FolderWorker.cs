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

namespace FSV.FolderTree
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;

    public class FolderWorker : BackgroundWorker
    {
        private readonly IFileManagementService fileManagementService;
        private readonly FolderModelBuilder folderModelBuilder;
        private readonly ILogger<FolderWorker> logger;
        private readonly FolderWorkerState state;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderWorker" /> class.
        /// </summary>
        public FolderWorker(
            IFileManagementService fileManagementService,
            FolderModelBuilder folderModelBuilder,
            FolderWorkerState state,
            ILogger<FolderWorker> logger)
        {
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.folderModelBuilder = folderModelBuilder ?? throw new ArgumentNullException(nameof(folderModelBuilder));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;
        }

        /// <summary>
        ///     Adds root of UncPath in container.
        /// </summary>
        /// <param name="path">A path of unc directory.</param>
        /// <returns>
        ///     True when path is UNC and added in container, false when path is already available in container, and null when
        ///     path is not UNC.
        /// </returns>
        public bool? AddUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            var uri = new Uri(path);
            if (!uri.IsUnc)
            {
                return null;
            }

            DirectoryInfo directoryInfo = this.fileManagementService.GetDirectoryInfo(path);
            string pathRoot = directoryInfo.Root.FullName;
            return this.state.TryAddUncPath(pathRoot);
        }

        /// <summary>
        ///     Removes path from container.
        /// </summary>
        /// <param name="path">An Unc Path.</param>
        /// <returns>True if path is found and removed, otherwise false.</returns>
        public bool RemoveUncPath(string path)
        {
            var uri = new Uri(path);
            if (uri.IsUnc)
            {
                DirectoryInfo directoryInfo = this.fileManagementService.GetDirectoryInfo(path);
                if (directoryInfo == null)
                {
                    return false;
                }

                string pathRoot = directoryInfo.Root.FullName;
                return this.state.TryRemoveUncPath(pathRoot);
            }

            return false;
        }

        /// <summary>
        ///     Raises the <see cref="E:System.ComponentModel.BackgroundWorker.DoWork" /> event.
        /// </summary>
        /// <param name="e">
        ///     An <see cref="T:System.EventArgs" /> that contains the event data.
        /// </param>
        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            if (!(e.Argument is string path))
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    this.LoadDrives();
                }
                else
                {
                    this.LoadFolders(path);
                }
            }
            catch (FolderWorkerException ex)
            {
                this.logger.LogError(ex, "The folder worker could not complete due to an error.");
            }
        }

        /// <summary>
        ///     Loads the folders.
        /// </summary>
        /// <param name="path">The path.</param>
        private void LoadFolders(string path)
        {
            try
            {
                this.logger.LogDebug("Get Folders for: {Path}", path);

                var count = 0;

                foreach (IFolder folder in this.fileManagementService.GetDirectories(path))
                {
                    bool folderAccessDenied = this.fileManagementService.IsAccessDenied(folder);
                    folder.AccessDenied = folderAccessDenied;
                    folder.HasSubFolders = !folderAccessDenied && this.fileManagementService.HasSubFolders(folder);

                    count++;

                    FolderModel folderModel = FolderModelBuilder.GetFolderItem(folder, path);

                    this.logger.LogDebug("Add Subfolder: {Folder}", folderModel.Name);

                    this.ReportProgress(count, folderModel);
                }

                this.logger.LogDebug("Subfolders found: {NumFolders}", count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load folders for path ({Path}) due to an unhandled error.", path);
                var message = $"Failed to load folders for path ({path}) due to an unhandled error.";
                throw new FolderWorkerException($"{message} See inner exception for further details.", ex);
            }
        }

        /// <summary>
        ///     Loads the drives.
        /// </summary>
        private void LoadDrives()
        {
            try
            {
                IEnumerable<FolderModel> drives = DriveInfo.GetDrives().Select(this.folderModelBuilder.GetDriveItem);
                IEnumerable<FolderModel> uncPaths = this.state.GetUncPaths().Select(this.folderModelBuilder.GetUncPathItem);
                List<FolderModel> list = drives.Union(uncPaths).ToList();

                int total = list.Count;
                var count = 0;
                foreach (FolderModel folderModel in list)
                {
                    count++;
                    int percentProgress = GetPercentage(total, count);
                    this.ReportProgress(percentProgress, folderModel);
                    this.logger.LogDebug("Get Drive: {Folder}", folderModel.Name);
                }
            }
            catch (Exception ex)
            {
                var message = "Failed to load drives due to an unhandled error.";
                this.logger.LogError(ex, message);
                throw new FolderWorkerException($"{message} See inner exception for further details.", ex);
            }
        }

        /// <summary>
        ///     Gets the percentage.
        /// </summary>
        /// <param name="total">The total.</param>
        /// <param name="count">The count.</param>
        /// <returns>percentage done of operation</returns>
        private static int GetPercentage(int total, int count)
        {
            return 100 * count / total;
        }
    }
}