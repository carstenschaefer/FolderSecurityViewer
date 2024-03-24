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

namespace FSV.Extensions.Logging
{
    using System;
    using System.IO;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;
    using Logger = Serilog.Core.Logger;

    public static class LoggingBootstrappingExtensions
    {
        public static IServiceCollection AddLogging(
            this IServiceCollection services, LogEventLevel defaultLogLevel = LogEventLevel.Error)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            IConfigurationPaths paths = CreateAndConfigurePaths();
            string logfile = Path.Combine(paths.LogDirectory, "log.txt");

            var levelSwitch = new LoggingLevelSwitch(defaultLogLevel);
            var levelSwitchAdapter = new LoggingLevelSwitchAdapter(levelSwitch);

            Logger logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .MinimumLevel.Override("FSV.FileSystem.Interop.Core", LogEventLevel.Warning)
                .WriteTo.File(logfile, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                .CreateLogger();

            services.AddSingleton(paths);
            services.AddLogging(builder => builder.AddSerilog(logger));
            services.AddSingleton<ILoggingLevelSwitchAdapter>(levelSwitchAdapter);

            return services;
        }

        private static IConfigurationPaths CreateAndConfigurePaths()
        {
            var instance = new ConfigurationPaths();
            instance.CreateApplicationDirectories();
            return instance;
        }
    }
}