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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Business.Abstractions;
    using Core;
    using Microsoft.Extensions.Logging;
    using Models;
    using Resources;

    public class PermissionItemACLDifferenceViewModel : PermissionItemBase
    {
        private static readonly int Limit = 1000;

        private readonly IAclCompareTask _aclCompareTask;
        private readonly IPermissionListTask _permissionListTask;
        private readonly IDialogService dialogService;

        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly ILogger<PermissionItemACLDifferenceViewModel> logger;
        private readonly INavigationService navigationService;

        /// <summary>
        ///     Gets a command for DifferentItemViewModel to start permission report of selected path.
        ///     The binding must pass path as Command Parameter.
        /// </summary>
        private readonly ICommand openInTabCommand;

        private int _differences;
        private IList<DifferentItemViewModel> _differentPaths;
        private CommandViewModel _exportCommandViewModel;

        private bool _exportStarted;

        public PermissionItemACLDifferenceViewModel(
            IDispatcherService dispatcherService,
            INavigationService navigationService,
            IDialogService dialogService,
            IPermissionListTask permissionHierarchyTask,
            IAclCompareTask aclCompareTask,
            ILogger<PermissionItemACLDifferenceViewModel> logger,
            IExportService exportService,
            string folderPath) : base(folderPath)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this._aclCompareTask = aclCompareTask ?? throw new ArgumentNullException(nameof(aclCompareTask));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this._permissionListTask = permissionHierarchyTask ?? throw new ArgumentNullException(nameof(permissionHierarchyTask));

            this.CanResize = true;
            this.Icon = "CompareDifferencesIcon";
            this.DifferentPaths = new ObservableCollection<DifferentItemViewModel>();

            this.DisplayName = PermissionResource.ProgressDifferencesText;
            this.openInTabCommand = new RelayCommand(this.StartPermissionReport);

            this.StartScanAsync().FireAndForgetSafeAsync();
        }

        public IList<DifferentItemViewModel> DifferentPaths
        {
            get => this._differentPaths;
            private set => this.Set(ref this._differentPaths, value, nameof(this.DifferentPaths));
        }

        internal void StopScan()
        {
            if (this._aclCompareTask.IsBusy)
            {
                this._aclCompareTask.Cancel();
            }
        }

        private async Task StartScanAsync()
        {
            this.DoProgress();

            try
            {
                await this._aclCompareTask.RunAsync(this.FolderPath, this.OnCompareProgress);

                this.DoneScan();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to start scan for folder {Path} due to an unhandled error.", this.FolderPath);
            }

            this.StopProgress();
        }

        private void DoneScan()
        {
            string compareText;

            if (this._differences >= Limit)
            {
                compareText = string.Concat(PermissionResource.MoreThanCaption, " ", string.Format(PermissionResource.DifferencesFoundText, this._differences));
            }
            else
            {
                compareText = string.Format(PermissionResource.DifferencesFoundText, this._differences);
            }

            this.DisplayName = compareText;

            if (this.DifferentPaths.Count > 0)
            {
                this._exportCommandViewModel = new CommandViewModel(this.BeginExport, string.Empty, "ExportIcon", CommonResource.ExportAllCaption);
                this.TitleCommands.Add(this._exportCommandViewModel);
            }
        }

        private void OnCompareProgress(AclComparisonResult result)
        {
            if (result is AclComparisonError)
            {
                return;
            }

            if (this._differences >= Limit)
            {
                this._aclCompareTask.Cancel();
            }
            else
            {
                this._differences++;

                this.DifferentPaths.Add(new DifferentItemViewModel(result.FullName, this.openInTabCommand));
                this.DisplayName = string.Concat(PermissionResource.StartComparisonCaption, string.Format(PermissionResource.DifferencesFoundText, this._differences));
            }
        }

        private async Task BeginExport(object _)
        {
            if (this._permissionListTask.CancelRequested)
            {
                return;
            }

            if (this._exportStarted)
            {
                this._permissionListTask.Cancel();
                this._exportCommandViewModel.Tip = CommonResource.CancellingTip;
            }
            else
            {
                this._exportCommandViewModel.Tip = CommonResource.CancelButtonCaption;
                await this.ExportAll();
            }
        }

        private async Task ExportAll()
        {
            if (!this.dialogService.Ask(PermissionResource.ExportDifferencePermissionReportText))
            {
                return;
            }

            this._exportStarted = true;
            this._exportCommandViewModel.Icon = "StopIcon";

            try
            {
                IEnumerable<string> paths = this.DifferentPaths.Select(m => m.Path);

                async void OnCompleteAsync(DataTable result, int index)
                {
                    await this.OnPermissionScanCompleteAsync(result, index);
                }

                async void OnProgressAsync(int count, int index)
                {
                    await this.OnPermissionProgressAsync(count, index);
                }

                await this._permissionListTask.RunAsync(paths, OnProgressAsync, OnCompleteAsync);

                this.exportService.ExportPermissionDifferenceReports(this.DifferentPaths);
            }
            catch (PermissionTaskExecutionException e)
            {
                const string errorMessage = "Failed to export permission differences due to an error.";
                this.logger.LogError(e, errorMessage);
            }
            catch (OperationCanceledException e)
            {
                const string errorMessage = "Failed to export permission differences due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
            }

            this._exportCommandViewModel.Icon = "ExportIcon";
            this._exportCommandViewModel.Tip = CommonResource.ExportAllCaption;
            this._exportStarted = false;
        }

        private async Task OnPermissionScanCompleteAsync(DataTable result, int index)
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                DifferentItemViewModel differentPath = this.DifferentPaths[index];
                if (result == null)
                {
                    differentPath.State = DifferentItemState.Failed;
                    differentPath.ExportCount = 0;
                }
                else
                {
                    differentPath.State = DifferentItemState.Exported;
                    differentPath.ExportCount = result.Rows.Count;
                    differentPath.ExportItems = result;
                }
            });
        }

        private async Task OnPermissionProgressAsync(int count, int index)
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                DifferentItemViewModel differentPath = this.DifferentPaths[index];

                differentPath.State = DifferentItemState.Exporting;
                differentPath.ExportCount = count;
            });
        }

        private void StartPermissionReport(object obj)
        {
            if (obj is string path && !string.IsNullOrEmpty(path))
            {
                this.navigationService.NavigateWithAsync<PermissionsViewModel>(path);
            }
        }
    }
}