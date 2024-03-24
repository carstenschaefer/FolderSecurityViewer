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

namespace FSV.ViewModel.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Abstractions;
    using AdMembers;
    using Exporter;
    using Folder;
    using Microsoft.Extensions.Logging;
    using Owner;
    using Permission;
    using ShareReport;
    using ViewModel.UserReport;

    public class ExportService : IExportService
    {
        private readonly IDialogService dialogService;
        private readonly ModelBuilder<Func<ExporterBase, Task>, ExportViewModel> exportModelBuilder;
        private readonly ILogger<ExportService> logger;
        private readonly ModelBuilder<Csv> modelBuilderCsv;
        private readonly ModelBuilder<Excel> modelBuilderExcel;
        private readonly ModelBuilder<Html> modelBuilderHtml;
        private readonly ModelBuilder<Func<ExporterBase, Task>, ExportShareReportViewModel> shareReportExportModelBuilder;

        public ExportService(
            IDialogService dialogService,
            ILogger<ExportService> logger,
            ModelBuilder<Excel> modelBuilderExcel,
            ModelBuilder<Csv> modelBuilderCsv,
            ModelBuilder<Html> modelBuilderHtml,
            ModelBuilder<Func<ExporterBase, Task>, ExportViewModel> exportModelBuilder,
            ModelBuilder<Func<ExporterBase, Task>, ExportShareReportViewModel> shareReportExportModelBuilder)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.modelBuilderExcel = modelBuilderExcel ?? throw new ArgumentNullException(nameof(modelBuilderExcel));
            this.modelBuilderCsv = modelBuilderCsv ?? throw new ArgumentNullException(nameof(modelBuilderCsv));
            this.modelBuilderHtml = modelBuilderHtml ?? throw new ArgumentNullException(nameof(modelBuilderHtml));
            this.exportModelBuilder = exportModelBuilder ?? throw new ArgumentNullException(nameof(exportModelBuilder));
            this.shareReportExportModelBuilder = shareReportExportModelBuilder ?? throw new ArgumentNullException(nameof(shareReportExportModelBuilder));
        }

        public IEnumerable<ExporterBase> GetExporters(IEnumerable<ExportContentType> exportContentTypes)
        {
            if (exportContentTypes is null)
            {
                throw new ArgumentNullException(nameof(exportContentTypes));
            }

            var list = new List<ExporterBase>(exportContentTypes.Count());

            foreach (ExportContentType item in exportContentTypes)
            {
                switch (item)
                {
                    case ExportContentType.Excel:
                        Excel excel = this.modelBuilderExcel.Build();
                        list.Add(excel);
                        break;
                    case ExportContentType.CSV:
                        Csv csv = this.modelBuilderCsv.Build();
                        list.Add(csv);
                        break;
                    case ExportContentType.HTML:
                        Html html = this.modelBuilderHtml.Build();
                        list.Add(html);
                        break;
                }
            }

            return list;
        }

        public void OpenExport(ExportOpenType exportOpenType, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace", nameof(filePath));
            }

            try
            {
                if (exportOpenType == ExportOpenType.CopyPathToClipboard)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(filePath);
                }
                else if (exportOpenType == ExportOpenType.CopyFileToClipboard)
                {
                    Clipboard.Clear();
                    Clipboard.SetFileDropList(new StringCollection { filePath });
                }
                else // Open file.
                {
                    Process.Start(filePath);
                }
            }
            catch (Win32Exception ex)
            {
                this.logger.LogError(ex, "Failed to open exported file {filePath}", filePath);
                throw new ExportServiceException($"Failed to open exported file {filePath}. See inner exception for more details.", ex);
            }
        }

        public void ExportPermissionReports(IEnumerable<PermissionReportBaseViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportUserReports(IEnumerable<UserReportBaseViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportOwnerReports(IEnumerable<OwnerReportViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportFolderReports(IEnumerable<FolderViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportPermissionDifferenceReports(IEnumerable<DifferentItemViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportPrincipalMemberships(IEnumerable<PrincipalMembershipViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportGroupMembers(IEnumerable<GroupMembersViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(items);
            }

            this.ExportReports(ExportCallbackAsync);
        }

        public void ExportShareReport(SharedServerViewModel sharedServer)
        {
            if (sharedServer is null)
            {
                throw new ArgumentNullException(nameof(sharedServer));
            }

            async Task ExportCallbackAsync(ExporterBase exporter)
            {
                await exporter.ExportAsync(sharedServer);
            }

            ExportShareReportViewModel exportViewModel = this.shareReportExportModelBuilder.Build(ExportCallbackAsync);
            this.dialogService.ShowDialog(exportViewModel);
        }

        private void ExportReports(Func<ExporterBase, Task> exportCallbackAsync)
        {
            ExportViewModel exportViewModel = this.exportModelBuilder.Build(exportCallbackAsync);
            this.dialogService.ShowDialog(exportViewModel);
        }
    }
}