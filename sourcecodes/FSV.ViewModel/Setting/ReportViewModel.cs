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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    public class ReportViewModel : SettingWorkspaceViewModel
    {
        private readonly ReportShareViewModel _reportShareViewModel;
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly ILogger<ReportViewModel> logger;
        private bool disposed;

        public ReportViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IConfigurationManager configurationManager,
            ILogger<ReportViewModel> logger,
            ModelBuilder<ReportPermissionViewModel> reportPermissionViewModelBuilder,
            ModelBuilder<ReportFolderViewModel> reportFolderViewModelBuilder,
            ModelBuilder<ReportUserViewModel> reportUserViewModelBuilder,
            ReportShareViewModel reportShareViewModel) : base(dispatcherService, dialogService, false, true)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._reportShareViewModel = reportShareViewModel ?? throw new ArgumentNullException(nameof(reportShareViewModel));

            this.DisplayName = ConfigurationResource.ReportCaption;

            this.Items = new List<ReportWorkspaceViewModel>(4)
            {
                reportPermissionViewModelBuilder.Build(),
                reportFolderViewModelBuilder.Build(),
                reportUserViewModelBuilder.Build(),
                this._reportShareViewModel
            };

            this.UsesSave = this.IsEnabled;
        }

        public IList<ReportWorkspaceViewModel> Items { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.disposed = true;
            }
        }

        internal override async Task<bool> Save()
        {
            this.DoProgress();

            try
            {
                await this.configurationManager.SaveAsync();
                await this._reportShareViewModel.Save();

                return true;
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to save the report due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(errorMessage));
                return false;
            }
            finally
            {
                this.StopProgress();
            }
        }
    }
}