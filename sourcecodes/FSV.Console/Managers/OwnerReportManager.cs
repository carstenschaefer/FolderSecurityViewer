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

namespace FSV.Console.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Business.Abstractions;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;
    using Properties;
    using Services;

    public class OwnerReportManager : IReportManager
    {
        private readonly IDisplayService displayService;
        private readonly IExportBuilder exportBuilder;
        private readonly ILogger<OwnerReportManager> logger;
        private readonly UserFolderData ownerFolderData;
        private readonly ITaskCreator taskCreator;

        public OwnerReportManager(
            ITaskCreator taskCreator,
            IExportBuilder exportBuilder,
            IDisplayService displayService,
            ILogger<OwnerReportManager> logger,
            UserFolderData ownerFolderData)
        {
            this.taskCreator = taskCreator ?? throw new ArgumentNullException(nameof(taskCreator));
            this.exportBuilder = exportBuilder ?? throw new ArgumentNullException(nameof(exportBuilder));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.ownerFolderData = ownerFolderData ?? throw new ArgumentNullException(nameof(ownerFolderData));
        }

        public async Task StartScanAndExportReportAsync()
        {
            this.displayService.ShowText(Resources.OwnerReportScanningText, this.ownerFolderData.ScanDirectory, this.ownerFolderData.User);

            try
            {
                using IFolderTask folderTask = this.taskCreator.GetFolderTask();
                IEnumerable<IFolderReport> folders = await folderTask.RunAsync(this.ownerFolderData.ScanDirectory, this.ownerFolderData.User);

                folders.EnumerateFoldersWithExceptionAndLog(this.logger, this.displayService);
                await this.EnumerateFoldersWithoutExceptionAndExport(folders);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to initialize scan for folder {Path} and user {User}.", this.ownerFolderData.ScanDirectory, this.ownerFolderData.User);
                throw new ReportManagerException(Resources.OwnerReportCaption, e);
            }
        }

        private async Task<string> ExportToFileAsync(string scanDirectory, string exportType, string exportFile, IList<FolderItem> folderItems)
        {
            IExport exporter = this.exportBuilder.Build(exportType, scanDirectory, exportFile);
            return await exporter.ExportOwnersAsync(folderItems);
        }

        private async Task EnumerateFoldersWithoutExceptionAndExport(IEnumerable<IFolderReport> folders)
        {
            List<FolderItem> foldersWithoutException = folders.Where(m => m.Exception == null)
                .Select(m => new FolderItem(m))
                .ToList();

            this.displayService.ShowText(Resources.FolderScanCompleteText, foldersWithoutException.Count);

            if (foldersWithoutException.Any())
            {
                this.displayService.ShowText(Resources.ExportingText);

                string filePath = await this.ExportToFileAsync(this.ownerFolderData.ScanDirectory, this.ownerFolderData.ExportType, this.ownerFolderData.ExportPath,
                    foldersWithoutException);

                this.displayService.ShowInfo(Resources.OwnerReportExportedText, filePath);
            }
            else
            {
                this.displayService.ShowError(Resources.NoResultToExportText);
            }
        }
    }
}