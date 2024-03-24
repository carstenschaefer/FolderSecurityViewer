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
    using System.Globalization;
    using System.Threading;
    using Abstractions;
    using Business;
    using Business.Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Database;
    using Database.Abstractions;
    using Database.Models;
    using Exporter;
    using Extensions.DependencyInjection;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core.Abstractions;
    using FolderTree;
    using FSV.Templates;
    using Home;
    using Managers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Models;
    using Prism.Events;
    using Resources;
    using Services;
    using Services.Home;
    using Services.Setting;
    using Services.Shares;
    using Services.UserReport;

    public class ViewModelServicesModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            // Cloning current culture so that original system culture can be retained.
            services.AddSingleton(Thread.CurrentThread.CurrentCulture.Clone() as CultureInfo);
            services.AddSingleton<IStartUpSequence, StartUpSequence>();
            services.AddSingleton<IDatabaseManager, DatabaseManager>();
            services.AddTransient<IUnitOfWorkFactory, DatabaseUnitOfWorkFactory>();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IFlyOutService, FlyOutService>();
            services.AddTransient<ISavedReportService, SavedReportService>();
            services.AddSingleton<IAdBrowserService, AdBrowserService>();
            services.AddTransient<IPermissionReportManager, PermissionReportManager>();
            services.AddTransient<IUserReportService, UserReportService>();
            services.AddTransient<IUserPermissionReportManager, UserPermissionReportManager>();
            services.AddTransient<UserPermissionReport>();
            services.AddTransient<ICompareService, CompareService>();
            services.AddTransient<PermissionReport>();
            services.AddSingleton<FolderWorkerState>();
            services.AddTransient<FolderModelBuilder>();
            services.AddTransient<FolderWorker>();

            services.AddSingleton(c => TemplateFileFactory.GetTemplateFile(ConfigPath.GetOrCreateApplicationDataFolderIfNotExists()));
            services.AddTransient<ISettingShareService, SettingShareService>();
            services.AddSingleton(GetReportTrusteeFactory);
            services.AddSingleton(GetReportUserFactory);
            services.AddSingleton(provider => this.GetFsvColumnNames());
            services.AddTransient(GetDirectoryEnumerator);
            services.AddTransient<Func<FolderEnumeratorOptions>>(provider => () => GetDirectoryFolderEnumeratorOptions(provider));
            services.AddTransient<IPermissionTask, PermissionTask>();
            services.AddTransient<IPermissionListTask, PermissionListTask>();
            services.AddTransient<IAclCompareTask, AclCompareTask>();
            services.AddTransient<IUserPermissionTask, UserPermissionTask>();
            services.AddTransient<IFolderTask, FolderTask>();
            services.AddTransient<IShareScannerFactory, ShareScannerFactory>();

            services.AddTransient<IExportService, ExportService>();
            services.AddTransient<ExportTableGenerator>();
            services.AddTransient<Excel>();
            services.AddTransient<Csv>();
            services.AddTransient<Html>();

            services.AddTransient<Func<Func<IEnumerable<FolderTreeItemViewModel>>, IFolderTreeItemSelector>>(_ =>
                items => new FolderTreeItemSelector(items));
        }

        private static Func<ReportUser> GetReportUserFactory(IServiceProvider c)
        {
            var configurationManager = c.GetRequiredService<IConfigurationManager>();
            return () => configurationManager.ConfigRoot.Report.User;
        }

        private static Func<ReportTrustee> GetReportTrusteeFactory(IServiceProvider serviceProvider)
        {
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            return () =>
            {
                ConfigRoot configRoot = configurationManager.ConfigRoot;
                Report rootReport = configRoot?.Report;
                return rootReport?.Trustee;
            };
        }

        private FsvColumnNames GetFsvColumnNames()
        {
            return new FsvColumnNames
            {
                Folder = UserReportResource.UserReportFolderCaption,
                CompleteName = UserReportResource.UserReportNameCaption
            };
        }

        private static IDirectoryEnumerator GetDirectoryEnumerator(IServiceProvider serviceProvider)
        {
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            var fileManagementService = serviceProvider.GetRequiredService<IFileManagementService>();
            var directorySizeService = serviceProvider.GetRequiredService<IDirectorySizeService>();
            var ownerService = serviceProvider.GetRequiredService<IOwnerService>();
            var kernel32 = serviceProvider.GetRequiredService<IKernel32>();
            var kernel32FindFile = serviceProvider.GetRequiredService<IKernel32FindFile>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            ConfigRoot configRoot = configurationManager.ConfigRoot;
            Report configRootReport = configRoot?.Report;
            ReportFolder configReportFolder = configRootReport?.Folder;

            return new DirectoryEnumerator(
                fileManagementService,
                directorySizeService,
                ownerService,
                kernel32,
                kernel32FindFile,
                loggerFactory,
                configReportFolder?.IncludeSubFolder ?? false,
                configReportFolder?.IncludeHiddenFolder ?? false,
                configReportFolder?.IncludeCurrentFolder ?? false,
                configReportFolder?.IncludeFileCount ?? false,
                configReportFolder?.IncludeSubFolderFileCount ?? false,
                configReportFolder?.Owner ?? false);
        }

        private static FolderEnumeratorOptions GetDirectoryFolderEnumeratorOptions(IServiceProvider serviceProvider)
        {
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();

            ConfigRoot configRoot = configurationManager.ConfigRoot;
            Report configRootReport = configRoot?.Report;
            ReportFolder configReportFolder = configRootReport?.Folder;

            const string ownerFilter = null;
            var folderEnumeratorOptions = new FolderEnumeratorOptions(
                configReportFolder?.IncludeSubFolder ?? false,
                configReportFolder?.IncludeHiddenFolder ?? false,
                configReportFolder?.IncludeCurrentFolder ?? false,
                configReportFolder?.IncludeFileCount ?? false,
                configReportFolder?.IncludeSubFolderFileCount ?? false,
                configReportFolder?.Owner ?? false,
                ownerFilter);

            return folderEnumeratorOptions;
        }
    }
}