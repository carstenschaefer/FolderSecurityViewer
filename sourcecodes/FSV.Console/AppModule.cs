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
    using Exporter;
    using Extensions.DependencyInjection;
    using Managers;
    using Microsoft.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;
    using Services;

    public sealed class AppModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<ITaskCreator, TaskCreator>();
            services.AddTransient<IExportTableGenerator, ExportTableGenerator>();
            services.AddTransient<IExportBuilder, ExportBuilder>();
            services.AddTransient<IReportManagerBuilder, ReportManagerBuilder>();
            services.AddTransient<IDisplayService, DisplayService>();
            services.AddTransient<IArgumentValidationService, ArgumentValidationService>();

            services.AddSingleton(new CommandLineApplication { Name = "fsv" });
            services.AddSingleton<App>();
        }
    }
}