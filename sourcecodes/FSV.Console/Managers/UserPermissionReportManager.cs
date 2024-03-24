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
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Business;
    using Business.Abstractions;
    using Microsoft.Extensions.Logging;
    using Properties;
    using Services;

    public class UserPermissionReportManager : IReportManager
    {
        private readonly IDisplayService displayService;
        private readonly IExportBuilder exportBuilder;
        private readonly ILogger<UserPermissionReportManager> logger;
        private readonly ITaskCreator taskCreator;
        private readonly UserFolderData userFolderData;

        public UserPermissionReportManager(
            ITaskCreator taskCreator,
            IExportBuilder exportBuilder,
            IDisplayService displayService,
            ILogger<UserPermissionReportManager> logger,
            UserFolderData userFolderData)
        {
            this.taskCreator = taskCreator ?? throw new ArgumentNullException(nameof(taskCreator));
            this.exportBuilder = exportBuilder ?? throw new ArgumentNullException(nameof(exportBuilder));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.userFolderData = userFolderData ?? throw new ArgumentNullException(nameof(userFolderData));
        }

        public async Task StartScanAndExportReportAsync()
        {
            void EmptyOnProgress(int _)
            {
            }

            this.displayService.ShowText(Resources.UserPermissionReportScanningText, this.userFolderData.ScanDirectory, this.userFolderData.User);

            try
            {
                using IUserPermissionTask userPermissionTask = this.taskCreator.GetUserPermissionTask();
                using UserPermissionTaskResult reportResult = await userPermissionTask.RunAsync(
                    this.userFolderData.User,
                    this.userFolderData.ScanDirectory,
                    EmptyOnProgress);

                if (reportResult == null || reportResult.ScanCancelled)
                {
                    this.displayService.ShowError(Resources.UserPermissionReportNotExportedText);
                    return;
                }

                this.displayService.ShowText(Resources.PermissionReportScanCompleteText, reportResult.Result.Rows.Count);

                if (reportResult.Result.Rows.Count == 0)
                {
                    this.displayService.ShowError(Resources.NoResultToExportText);
                    return;
                }

                this.displayService.ShowText(Resources.ExportingText);

                string filePath = await this.ExportToFileAsync(
                    this.userFolderData.User,
                    this.userFolderData.ScanDirectory,
                    this.userFolderData.ExportType,
                    this.userFolderData.ExportPath,
                    reportResult);

                this.displayService.ShowInfo(Resources.UserPermissionReportExportedText, filePath);
            }
            catch (BusinessServiceException e)
            {
                this.HandleUserPermissionTaskException(e);
            }
            catch (Exception e)
            {
                this.HandleUserPermissionTaskException(e);
            }
        }

        private void HandleUserPermissionTaskException(Exception exception)
        {
            string errorMessage = string.Format(Resources.UserPermissionReportFileExportException);
            this.logger.LogError(exception, errorMessage);
            this.displayService.ShowError(errorMessage);
            throw new ReportManagerException("Failed to scan user permissions and export the report due to an unhandled error. See inner exception for further details. See inner exception for further details.", exception);
        }

        private async Task<string> ExportToFileAsync(string user, string scanDirectory, string exportType, string exportFile, UserPermissionTaskResult result)
        {
            IExport exporter = this.exportBuilder.Build(exportType, scanDirectory, exportFile);
            return await exporter.ExportUserReportAsync(result.Result, user, result.ExceptionFolders.Select(m => m.FullName));
        }
    }
}