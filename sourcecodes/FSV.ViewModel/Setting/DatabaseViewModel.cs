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
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration.Abstractions;
    using Configuration.Database;
    using Database;
    using Database.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    public sealed class DatabaseViewModel : SettingWorkspaceViewModel
    {
        private readonly ModelBuilder<DatabaseNoneViewModel> databaseNoneViewModelBuilder;
        private readonly ModelBuilder<DatabaseSQLiteViewModel> databaseSqliteViewModelBuilder;
        private readonly ModelBuilder<DatabaseSqlServerViewModel> databaseSqlServerViewModelBuilder;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly IDatabaseManager dbManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly ILogger<DatabaseViewModel> logger;
        private bool _isConfigured;

        private BaseConfiguration databaseConfig;
        private bool disposed;
        private DatabaseTypeViewModel selectedProvider;

        public DatabaseViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IDatabaseConfigurationManager dbConfigurationManager,
            IDatabaseManager dbManager,
            IConfigurationManager configurationManager,
            ILogger<DatabaseViewModel> logger,
            ModelBuilder<DatabaseNoneViewModel> databaseNoneViewModelBuilder,
            ModelBuilder<DatabaseSQLiteViewModel> databaseSqliteViewModelBuilder,
            ModelBuilder<DatabaseSqlServerViewModel> databaseSqlServerViewModelBuilder) : base(dispatcherService, dialogService, true, true)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.databaseNoneViewModelBuilder = databaseNoneViewModelBuilder ?? throw new ArgumentNullException(nameof(databaseNoneViewModelBuilder));
            this.databaseSqliteViewModelBuilder = databaseSqliteViewModelBuilder ?? throw new ArgumentNullException(nameof(databaseSqliteViewModelBuilder));
            this.databaseSqlServerViewModelBuilder = databaseSqlServerViewModelBuilder ?? throw new ArgumentNullException(nameof(databaseSqlServerViewModelBuilder));
            this.DisplayName = SettingDatabaseResource.DatabaseCaption;

            this.dbConfigurationManager.ConfigChanged += this.HandleDatabaseConfigurationChanged;

            this.databaseConfig = dbConfigurationManager.Config;
            this.IsEnabled = !configurationManager.ConfigRoot?.SettingLocked ?? false;
            this.FillProviders();
        }

        public IList<DatabaseTypeViewModel> Providers { get; private set; }

        public DatabaseTypeViewModel SelectedProvider
        {
            get => this.selectedProvider;
            set => this.Set(ref this.selectedProvider, value, nameof(this.SelectedProvider));
        }

        public bool IsConfigured
        {
            get => this._isConfigured;
            set => this.Set(ref this._isConfigured, value, nameof(this.IsConfigured));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.dbConfigurationManager.ConfigChanged -= this.HandleDatabaseConfigurationChanged;
                this.disposed = true;
            }
        }

        private void HandleDatabaseConfigurationChanged(object s, DatabaseConfigurationChangedEventArgs e)
        {
            this.databaseConfig = e.Configuration;
        }

        private void FillProviders()
        {
            this.Providers = new List<DatabaseTypeViewModel>(3)
            {
                this.databaseNoneViewModelBuilder.Build(),
                this.databaseSqliteViewModelBuilder.Build(),
                this.databaseSqlServerViewModelBuilder.Build()
            };

            if (this.databaseConfig.DatabaseProvider != DatabaseProviders.None)
            {
                DatabaseTypeViewModel selectedProvider = this.Providers.First(m => m.DatabaseProvider == this.databaseConfig.DatabaseProvider);

                if (selectedProvider != null)
                {
                    selectedProvider.SetConfig(this.databaseConfig);
                    this.SelectedProvider = selectedProvider;
                    this.IsConfigured = true;
                }
            }
        }

        internal override async Task<bool> Save()
        {
            DatabaseTypeViewModel selectedProvider = this.SelectedProvider;
            if (selectedProvider == null)
            {
                return true;
            }

            this.DoProgress();
            bool result = await Task.Run(async () =>
            {
                try
                {
                    // Apply changes from selected db to config.
                    BaseConfiguration config = selectedProvider.GetConfig();
                    if (config == null)
                    {
                        return true;
                    }

                    this.dbManager.ChangeDatabase(config, this.logger);
                    this.dbConfigurationManager.Save(config);

                    this.IsConfigured = true;

                    this.logger.LogInformation("Database configuration saved.");

                    await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(ConfigurationResource.RestartRequired));

                    return true;
                }
                catch (Exception ex)
                {
                    string errorMessage = string.Format(ConfigurationResource.ConnectionStringChangeError, ex.Message);
                    this.logger.LogError(ex, "Failed to save the configuration to the database due to an unhandled error.");
                    await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(errorMessage));

                    return false;
                }
            });

            this.StopProgress();

            return result;
        }
    }
}