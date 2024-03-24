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

namespace FSV.Console
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices;
    using Configuration;
    using Configuration.Abstractions;
    using Database.Abstractions;
    using Managers;
    using Microsoft.Extensions.CommandLineUtils;
    using Properties;
    using Services;

    public class App
    {
        private readonly ActiveDirectory activeDirectory;
        private readonly CommandLineApplication application;
        private readonly IConfigurationManager configurationManager;
        private readonly IConfigurationPaths configurationPaths;
        private readonly IDatabaseConfigurationManager databaseConfigurationManager;
        private readonly IDatabaseManager databaseManager;
        private readonly IDisplayService displayService;
        private readonly IReportManagerBuilder reportManagerBuilder;

        public App(
            IConfigurationManager configurationManager,
            IReportManagerBuilder reportManagerBuilder,
            IDatabaseManager databaseManager,
            IDatabaseConfigurationManager databaseConfigurationManager,
            IDisplayService displayService,
            ActiveDirectory activeDirectory,
            IConfigurationPaths configurationPaths,
            CommandLineApplication application)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.reportManagerBuilder = reportManagerBuilder ?? throw new ArgumentNullException(nameof(reportManagerBuilder));
            this.databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
            this.databaseConfigurationManager = databaseConfigurationManager ?? throw new ArgumentNullException(nameof(databaseConfigurationManager));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));

            application.HelpOption("-?|-h|--help");
        }

        public void Run(string[] args)
        {
            this.Initialize();

            AssemblyName assemblyInfo = Assembly.GetEntryAssembly()?.GetName();

            this.displayService.ShowText($"{Resources.ApplicationVersion}\n", assemblyInfo.Name, assemblyInfo.Version);

            this.application.Execute(args);
            if (args?.Length == 0)
            {
                this.application.ShowHelp();
            }
        }

        public App AddCommand(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            void ExecuteCommand(CommandLineApplication application)
            {
                this.RunCommand(application, command);
            }

            this.application.Command(command.CommandName, ExecuteCommand);

            return this;
        }

        private void Initialize()
        {
            this.configurationPaths.CreateApplicationDirectories();
            this.configurationManager.CreateDefaultConfigFileAsync(false);

            this.activeDirectory.TryInitiateDomainEnumeration();

            this.databaseConfigurationManager.InitializeConfiguration();

            if (this.databaseConfigurationManager.HasConfiguredDatabaseProvider())
            {
                this.databaseManager.InitializeCurrentDatabase();
            }
        }

        private void RunCommand(CommandLineApplication commandLineApplication, ICommand command)
        {
            commandLineApplication.Description = command.CommandDescription;
            commandLineApplication.HelpOption(command.HelpText);

            if (command.Arguments == null)
            {
                this.displayService.ShowError(Resources.PrimaryArgumentError);
                return;
            }

            foreach (CommandArgument argument in command.Arguments)
            {
                commandLineApplication.Arguments.Add(argument);
            }

            foreach (CommandOption option in command.Options)
            {
                commandLineApplication.Options.Add(option);
            }

            async Task<int> CommandExecute()
            {
                try
                {
                    IReportManager reportManager = this.reportManagerBuilder.Build(command);

                    this.displayService.ShowText(command.CommandDescription);

                    var cancellationTokenSource = new CancellationTokenSource();

                    this.ShowProgressIndicator(cancellationTokenSource.Token);

                    await reportManager.StartScanAndExportReportAsync();

                    cancellationTokenSource.Cancel();
                }
                catch (ReportManagerException e)
                {
                    this.HandleRunCommandException(e);
                }
                catch (Exception ex)
                {
                    this.HandleRunCommandException(ex);
                }

                return 0;
            }

            commandLineApplication.OnExecute(CommandExecute);
        }

        private void HandleRunCommandException(Exception ex)
        {
            this.displayService.ShowError(ex.Message);
        }

        private async void ShowProgressIndicator(CancellationToken cancellationToken)
        {
            char[] chars = { '\\', '|', '/', '-' };
            var counter = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                this.displayService.ShowProgress($"[{chars[counter]}]");
                await Task.Delay(500);

                if (counter == chars.Length - 1)
                {
                    counter = 0;
                }
                else
                {
                    counter++;
                }
            }
        }
    }
}