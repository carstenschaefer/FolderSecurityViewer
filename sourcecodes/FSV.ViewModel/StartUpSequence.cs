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
    using System.Text;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Database;
    using Database.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    public sealed class StartUpSequence : IStartUpSequence
    {
        private readonly ActiveDirectory activeDirectory;
        private readonly IConfigurationManager configurationManager;
        private readonly IConfigurationPaths configurationPaths;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly IDatabaseManager dbManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly ICurrentDomainCheckUtility domainCheckUtility;
        private readonly ILogger<StartUpSequence> logger;
        private readonly IShareConfigurationManager shareConfigurationManager;

        public StartUpSequence(
            IConfigurationManager configurationManager,
            IDatabaseConfigurationManager dbConfigurationManager,
            IDatabaseManager dbManager,
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IShareConfigurationManager shareConfigurationManager,
            ActiveDirectory activeDirectory,
            IConfigurationPaths configurationPaths,
            ICurrentDomainCheckUtility domainCheckUtility,
            ILogger<StartUpSequence> logger)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.shareConfigurationManager = shareConfigurationManager ?? throw new ArgumentNullException(nameof(shareConfigurationManager));
            this.activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.domainCheckUtility = domainCheckUtility ?? throw new ArgumentNullException(nameof(domainCheckUtility));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
        }

        public async Task LoadAppSettings()
        {
            try
            {
                this.configurationPaths.CreateApplicationDirectories();

                this.LogAppDetail();

                await this.InitConfigsAsync();
                await this.EnumerateDomainAsync();
                await this.CheckUserAndDomainAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load app settings.", ex);
            }
        }

        private async Task EnumerateDomainAsync()
        {
            await Task.Run(this.activeDirectory.TryInitiateDomainEnumeration);
        }

        private async Task CheckUserAndDomainAsync()
        {
            bool joinedAndConnected = this.domainCheckUtility.IsComputerJoinedAndConnectedToDomain();
            bool domainUserLoggedIn = !this.domainCheckUtility.IsLocalAccountLoggedIn();

            var builder = new StringBuilder();

            if (!joinedAndConnected)
            {
                builder.Append(ErrorResource.WorkstationOutOfDomain);
            }

            if (!domainUserLoggedIn)
            {
                builder.AppendLine();
                builder.Append(ErrorResource.NoDomainUser);
            }

            if (builder.Length > 0)
            {
                var message = builder.ToString();
                await this.dispatcherService.InvokeAsync(() => { this.dialogService.ShowMessage(message); });
            }
        }

        private async Task InitConfigsAsync()
        {
            await Task.Run(() =>
            {
                this.configurationManager.LogInitialSettings();
                NetworkConfigurationManager.InitConfig();
                this.dbConfigurationManager.InitializeConfiguration();
                this.shareConfigurationManager.InitConfig();

                if (this.dbConfigurationManager.HasConfiguredDatabaseProvider())
                {
                    try
                    {
                        this.dbManager.InitDatabase(this.dbConfigurationManager.Config);
                        this.dbManager.CreateNewTables(this.logger);
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e, "Failed to initialize the application configuration due to an unhandled error.");
                    }
                }
            });
        }

        private void LogAppDetail()
        {
            this.logger.LogInformation($"Application Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} by {Environment.UserName} on {Environment.MachineName}");
            this.logger.LogInformation(string.Concat("Current Domain: ", Environment.UserDomainName));
        }
    }
}