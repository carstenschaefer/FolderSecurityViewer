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
    using Abstractions;
    using AdServices.AbstractionLayer;
    using Business;
    using Commands;
    using Configuration;
    using Crypto;
    using Extensions.Logging;
    using FileSystem.Interop;
    using Microsoft.Extensions.DependencyInjection;
    using Properties;
    using ViewModel;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();

            LoggingBootstrappingExtensions.AddLogging(services);

            services
                .UsePlatformServices()
                .UseConfigurationServices()
                .UseSecurityServices()
                .UseActiveDirectoryAbstractionLayer()
                .UseBusiness()
                .UseViewModels()
                .UseModelBuilders()
                .UseConsoleServices();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            var displayService = serviceProvider.GetRequiredService<IDisplayService>();

            void HandleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
            {
                displayService.ShowError(Resources.ApplicationTerminated);

                serviceProvider.Dispose();
            }

            Console.CancelKeyPress += HandleCancelKeyPress;

            try
            {
                var app = serviceProvider.GetRequiredService<App>();

                app.AddCommand(new PermissionReportCommand())
                    .AddCommand(new FolderReportCommand())
                    .AddCommand(new OwnerReportCommand())
                    .AddCommand(new UserReportCommand())
                    .AddCommand(new ShareReportCommand());

                app.Run(args);
            }
            catch (Exception ex)
            {
                displayService.ShowError(ex.Message);
            }

            Console.CancelKeyPress -= HandleCancelKeyPress;
        }
    }
}