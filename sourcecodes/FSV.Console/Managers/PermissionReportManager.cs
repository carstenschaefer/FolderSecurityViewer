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
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Business.Abstractions;
    using Configuration.Abstractions;
    using FileSystem.Interop.Abstractions;
    using FSV.Models;
    using Microsoft.Extensions.Logging;
    using Models;
    using Properties;
    using Services;
    using ViewModel.Abstractions;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class PermissionReportManager : IReportManager
    {
        private readonly IAclModelBuilder aclModelBuilder;
        private readonly IAclViewProvider aclViewProvider;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly IDisplayService displayService;
        private readonly IExportBuilder exportBuilder;
        private readonly ILogger<PermissionReportManager> logger;
        private readonly PermissionData permissionData;
        private readonly IPermissionReportManager permissionReportManager;
        private readonly ITaskCreator taskCreator;

        public PermissionReportManager(
            ITaskCreator taskCreator,
            IExportBuilder exportBuilder,
            IAclModelBuilder aclModelBuilder,
            IPermissionReportManager permissionReportManager,
            IDatabaseConfigurationManager dbConfigurationManager,
            IDisplayService displayService,
            IAclViewProvider aclViewProvider,
            PermissionData permissionData,
            ILogger<PermissionReportManager> logger)
        {
            this.taskCreator = taskCreator ?? throw new ArgumentNullException(nameof(taskCreator));
            this.exportBuilder = exportBuilder ?? throw new ArgumentNullException(nameof(exportBuilder));
            this.aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.permissionReportManager = permissionReportManager ?? throw new ArgumentNullException(nameof(permissionReportManager));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.permissionData = permissionData ?? throw new ArgumentNullException(nameof(permissionData));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartScanAndExportReportAsync()
        {
            void OnEmptyProgress(int _)
            {
            }

            ;

            this.displayService.ShowText(Resources.ScanningText, this.permissionData.ScanDirectory);

            using IPermissionTask permissionTask = this.taskCreator.GetPermissionTask();
            using DataTable dataTable = await permissionTask.RunAsync(this.permissionData.ScanDirectory, OnEmptyProgress);

            if (dataTable == null)
            {
                throw new ReportManagerException(Resources.PermissionReportNotExportedText);
            }

            await this.ExportOrSavePermissionsAsync(dataTable);
            await this.InitiateDifferenceScanAsync();
        }

        private async Task ExportOrSavePermissionsAsync(DataTable dataTable)
        {
            if (dataTable.Rows.Count == 0)
            {
                this.displayService.ShowError(Resources.NoResultToExportText);
                return;
            }

            this.displayService.ShowText(Resources.PermissionReportScanCompleteText, dataTable.Rows.Count);

            if (this.permissionData.ExportToDatabase)
            {
                await this.ExportToDatabaseAsync(this.permissionData.ScanDirectory, dataTable);
                this.displayService.ShowInfo(Resources.PermissionReportSavedInDbText);
            }
            else
            {
                List<IAclModel> acl = await this.LoadAclAsync(this.permissionData.ScanDirectory);

                string filePath = await this.ExportToFileAsync(this.permissionData.ScanDirectory, // the directory that is scanned.
                    this.permissionData.ExportType, // Export type - excel, html, csv.
                    this.permissionData.ExportPath, // The name of file in which the content to be saved.
                    dataTable,
                    acl
                );

                this.displayService.ShowInfo(Resources.PermissionReportExportedText, filePath);
            }
        }

        private async Task InitiateDifferenceScanAsync()
        {
            if (!this.permissionData.Difference)
            {
                return;
            }

            IList<string> differences = await this.StartCompareAsync(this.permissionData.ScanDirectory);
            if (differences.Count == 0)
            {
                this.displayService.ShowInfo(Resources.PermissionReportNoDifferenceText);
                return;
            }

            IList<DifferenceExportItem> differenceItems = await this.ScanSubDirectoriesAsync(differences);
            string filePath = await this.ExportPermissionListAsync(
                differenceItems, this.permissionData.ExportType, this.permissionData.DifferenceExportPath);

            if (!string.IsNullOrEmpty(filePath))
            {
                this.displayService.ShowInfo("\n" + Resources.PermissionReportDifferenceExportedText, filePath);
            }
        }

        private Task<List<IAclModel>> LoadAclAsync(string directory)
        {
            return Task.Run(() =>
            {
                IEnumerable<IAcl> result = this.aclViewProvider.GetAclView(directory).ToList();
                return result.Select(this.aclModelBuilder.Build).ToList();
            });
        }

        private async Task<IList<string>> StartCompareAsync(string directory)
        {
            IList<string> differences = new List<string>();

            using IAclCompareTask aclCompareTask = this.taskCreator.GetAclCompareTask();

            void OnAclCompareProgress(AclComparisonResult comparisonResult)
            {
                if (differences.Count > 1000)
                {
                    aclCompareTask.Cancel();
                }
                else
                {
                    differences.Add(comparisonResult.FullName);
                }
            }

            await aclCompareTask.RunAsync(directory, OnAclCompareProgress);

            return differences;
        }

        private async Task<string> ExportToFileAsync(string scanDirectory, string exportType, string exportFile, DataTable result, IList<IAclModel> aclModels)
        {
            Exception WrapAndLogException(Exception ex, string message)
            {
                var errorMessage = $"{message}: {ex.Message}.";
                this.logger.LogError(ex, errorMessage);
                return new ReportManagerException(errorMessage, ex);
            }

            try
            {
                IExport exporter = this.exportBuilder.Build(exportType, scanDirectory, exportFile);
                return await exporter.ExportPermissionsAsync(result, aclModels);
            }
            catch (InvalidExportTypeException ex)
            {
                throw WrapAndLogException(ex, Resources.PermissionReportFileExportException);
            }
            catch (FeatureNotAccessibleException ex)
            {
                throw WrapAndLogException(ex, Resources.PermissionReportFileExportException);
            }
            catch (Exception ex)
            {
                throw WrapAndLogException(ex, Resources.PermissionReportDbException);
            }
        }

        private async Task ExportToDatabaseAsync(string scanDirectory, DataTable result)
        {
            try
            {
                await Task.Run(() => this.permissionReportManager.Add(Environment.UserName, null, scanDirectory, result, this.dbConfigurationManager.Config.Encrypted));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to export the permission report to the database.");
                throw new ReportManagerException($"{Resources.PermissionReportDbException} See inner exception for further details.", ex);
            }
        }

        private async Task<IList<DifferenceExportItem>> ScanSubDirectoriesAsync(IList<string> differences)
        {
            IList<DifferenceExportItem> differenceItems = new List<DifferenceExportItem>();

            static void OnProgress(int count, int index)
            {
            }

            void OnComplete(DataTable result, int index)
            {
                string difference = differences[index];
                if (result == null)
                {
                    this.displayService.ShowError(Resources.PermissionReportDifferenceScanFailedText, difference);
                }
                else
                {
                    lock (differenceItems)
                    {
                        differenceItems.Add(new DifferenceExportItem(difference) { Result = result });
                    }

                    this.displayService.ShowText(Resources.PermissionReportDifferenceScanCompleteText, difference, result.Rows.Count);
                }
            }

            try
            {
                using IPermissionListTask permissionListTask = this.taskCreator.GetPermissionListTask();
                await permissionListTask.RunAsync(differences, OnProgress, OnComplete);

                return differenceItems;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to scan sub-directories due to an unhandled error.");
                throw new ReportManagerException($"{Resources.PermissionReportDifferenceUnhandledException} See inner exception for further details.", e);
            }
        }

        private async Task<string> ExportPermissionListAsync(
            IList<DifferenceExportItem> differences,
            string exportType,
            string exportFileName)
        {
            try
            {
                IExport exporter = this.exportBuilder.Build(exportType, string.Empty, exportFileName);
                return await exporter.ExportPermissionsAsync(differences);
            }
            catch (FeatureNotAccessibleException ex)
            {
                this.logger.LogError(ex, "Failed to export permission list due to an unhandled error.");
                throw new ReportManagerException($"{Resources.PermissionReportDifferenceException} See inner exception for further details.", ex);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to export permission list due to an unhandled error.");
                throw new ReportManagerException($"{Resources.PermissionReportDifferenceUnhandledException} See inner exception for further details.", ex);
            }
        }
    }
}