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

namespace FSV.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Exporter;
    using Microsoft.Extensions.Logging;
    using Resources;

    public class ExportViewModel : WorkspaceViewModel
    {
        private static readonly IEnumerable<ExportContentType> AllExportTypes = new[]
        {
            ExportContentType.Excel,
            ExportContentType.CSV,
            ExportContentType.HTML
        };

        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;

        private readonly Func<ExporterBase, Task> exportActionAsync;
        private readonly IExportService exportService;
        private readonly ILogger<ExportViewModel> logger;
        private bool _enableButtons;
        private ICommand _exportCommand;
        private ExporterBase _exportType;
        private List<ExporterBase> _list;

        public ExportViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IExportService exportService,
            ILogger<ExportViewModel> logger,
            Func<ExporterBase, Task> exportAction) :
            this(dispatcherService,
                dialogService,
                exportService,
                logger,
                exportAction,
                AllExportTypes)
        {
        }

        protected ExportViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IExportService exportService,
            ILogger<ExportViewModel> logger,
            Func<ExporterBase, Task> exportAction,
            IEnumerable<ExportContentType> exportContentTypes)
        {
            if (exportContentTypes is null)
            {
                throw new ArgumentNullException(nameof(exportContentTypes));
            }

            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportActionAsync = exportAction ?? throw new ArgumentNullException(nameof(exportAction));

            this.DisplayName = CommonResource.ExportCaption;

            this.FillExporters(exportContentTypes);
        }

        public IList<ExporterBase> ExportTypes => this._list;

        public ExporterBase SelectedExportType
        {
            get => this._exportType;
            set => this.SetExportTypeAsync(value).FireAndForgetSafeAsync(); // Intentional call. Important to call this method.
        }

        public bool EnableButtons
        {
            get => this._enableButtons;
            private set => this.Set(ref this._enableButtons, value, nameof(this.EnableButtons));
        }

        public ICommand ExportCommand => this._exportCommand ??= new AsyncRelayCommand(this.ExportAsync);

        private async Task ExportAsync(object obj)
        {
            if (this.exportActionAsync is null)
            {
                throw new InvalidOperationException("ExportAction callback is not assigned.");
            }

            if (this._exportType is null)
            {
                throw new InvalidOperationException("Export type is not selected.");
            }

            if (obj is not ExportOpenType openAs)
            {
                throw new InvalidOperationException($"{nameof(this.ExportCommand)} parameter must be one of the values of {nameof(ExportOpenType)}");
            }

            this.DoProgress();

            try
            {
                await this.exportActionAsync(this._exportType);
                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.exportService.OpenExport(openAs, this._exportType.FilePath);
                    this.CancelCommand.Execute(null);
                });
            }
            catch (IOException ex)
            {
                this.logger.LogError(ex, "Failed to export report to {file}. See inner exception for details.", this._exportType.FilePath);
                this.dialogService.ShowMessage(string.Format(ExportResource.ExportIOError, this._exportType.FilePath));
            }
            catch (ExportServiceException ex)
            {
                this.logger.LogError(ex, "Failed to open exported file {file}. See inner exception for details.", this._exportType.FilePath);
                this.dialogService.ShowMessage(string.Format(ExportResource.ExportServiceError, this._exportType.FilePath));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to export report. See inner exception for details.", this._exportType.FilePath);
                this.dialogService.ShowMessage(ex.Message);
            }

            this.StopProgress();
        }

        private void FillExporters(IEnumerable<ExportContentType> exportContentTypes)
        {
            this._list = this.exportService.GetExporters(exportContentTypes).ToList();

            IList<ExporterBase> accessList = this._list;
            if (accessList.Count() != this._list.Count)
            {
                this._exportType = accessList.FirstOrDefault();
                this.EnableButtons = true;
            }
        }

        /// <summary>
        ///     Sets Exporter and raises PropertyChanged event.
        ///     WPF doesn't acknowledge PropertyChanged, means selection is not reset to original object, if there is no change in
        ///     value event
        ///     if changed state is raised.
        /// </summary>
        /// <param name="selectedExporter"></param>
        private async Task SetExportTypeAsync(ExporterBase selectedExporter)
        {
            // This will assign keep the object selected in view even if it is not allowed.
            // The view will received the notification that property is changed.
            await this.dispatcherService.InvokeAsync(() =>
            {
                this._exportType = selectedExporter;
                this.RaisePropertyChanged(nameof(this.SelectedExportType));
                this.EnableButtons = true;
            });
        }
    }
}