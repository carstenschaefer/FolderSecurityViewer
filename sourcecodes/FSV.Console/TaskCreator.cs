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
    using AdServices;
    using AdServices.Abstractions;
    using Business;
    using Business.Abstractions;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core.Abstractions;
    using FSV.Models;
    using Microsoft.Extensions.Logging;
    using Resources;

    internal class TaskCreator : ITaskCreator
    {
        private readonly IAclModelBuilder aclModelBuilder;
        private readonly IAclViewProvider aclViewProvider;
        private readonly IConfigurationManager configurationManager;
        private readonly IDirectoryFolderEnumerator directoryFolderEnumerator;
        private readonly IDirectorySizeService directorySizeService;
        private readonly IFileManagementService fileManagementService;
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;
        private readonly ILogger<TaskCreator> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOwnerService ownerService;
        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;

        public TaskCreator(
            IActiveDirectoryState state,
            IAclModelBuilder aclModelBuilder,
            IAclViewProvider aclViewProvider,
            IConfigurationManager configurationManager,
            IFileManagementService fileManagementService,
            IActiveDirectoryUtility utility,
            IDirectoryFolderEnumerator directoryFolderEnumerator,
            IDirectorySizeService directorySizeService,
            IOwnerService ownerService,
            IKernel32 kernel32,
            IKernel32FindFile kernel32FindFile,
            Func<FolderEnumeratorOptions> folderEnumeratorOptionsFactory,
            ILogger<TaskCreator> logger,
            ILoggerFactory loggerFactory)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.directoryFolderEnumerator = directoryFolderEnumerator ?? throw new ArgumentNullException(nameof(directoryFolderEnumerator));
            this.directorySizeService = directorySizeService ?? throw new ArgumentNullException(nameof(directorySizeService));
            this.ownerService = ownerService ?? throw new ArgumentNullException(nameof(ownerService));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IPermissionTask GetPermissionTask()
        {
            ReportTrustee ReportTrustee()
            {
                return this.configurationManager.ConfigRoot.Report.Trustee;
            }

            ILogger<ActiveDirectoryFinderFactory> finderFactoryLogger = this.loggerFactory.CreateLogger<ActiveDirectoryFinderFactory>();
            var finderFactory = new ActiveDirectoryFinderFactory(this.state,
                this.utility,
                this.loggerFactory,
                finderFactoryLogger,
                ReportTrustee,
                new FsvColumnNames());

            ILogger<PermissionTask> taskLogger = this.loggerFactory.CreateLogger<PermissionTask>();
            return new PermissionTask(this.aclModelBuilder,
                finderFactory, this.aclViewProvider, taskLogger);
        }

        public IPermissionListTask GetPermissionListTask()
        {
            ReportTrustee ReportTrustee()
            {
                return this.configurationManager.ConfigRoot.Report.Trustee;
            }

            ILogger<ActiveDirectoryFinderFactory> finderFactoryLogger = this.loggerFactory.CreateLogger<ActiveDirectoryFinderFactory>();
            var finderFactory = new ActiveDirectoryFinderFactory(
                this.state,
                this.utility,
                this.loggerFactory,
                finderFactoryLogger,
                ReportTrustee,
                new FsvColumnNames());

            ILogger<PermissionListTask> taskLogger = this.loggerFactory.CreateLogger<PermissionListTask>();
            return new PermissionListTask(this.aclModelBuilder,
                this.aclViewProvider,
                finderFactory, taskLogger);
        }

        public IFolderTask GetFolderTask()
        {
            ReportFolder configFolder = this.configurationManager.ConfigRoot.Report.Folder;

            var directoryEnumerator = new DirectoryEnumerator(
                this.fileManagementService,
                this.directorySizeService, this.ownerService, this.kernel32, this.kernel32FindFile,
                this.loggerFactory,
                configFolder.IncludeSubFolder,
                configFolder.IncludeHiddenFolder,
                configFolder.IncludeCurrentFolder,
                configFolder.IncludeFileCount,
                configFolder.IncludeSubFolderFileCount,
                configFolder.Owner);

            return new FolderTask(directoryEnumerator);
        }

        public IUserPermissionTask GetUserPermissionTask()
        {
            var fsvColumnNames = new FsvColumnNames
            {
                Folder = UserReportResource.UserReportFolderCaption,
                CompleteName = UserReportResource.UserReportNameCaption
            };

            ReportTrustee ReportTrustee()
            {
                return this.configurationManager.ConfigRoot.Report.Trustee;
            }

            ILogger<ActiveDirectoryFinderFactory> finderFactoryLogger = this.loggerFactory.CreateLogger<ActiveDirectoryFinderFactory>();
            var finderFactory = new ActiveDirectoryFinderFactory(this.state,
                this.utility,
                this.loggerFactory,
                finderFactoryLogger,
                ReportTrustee,
                fsvColumnNames);

            ReportUser ReportUser()
            {
                return this.configurationManager.ConfigRoot.Report.User;
            }

            ILogger<UserPermissionTask> taskLogger = this.loggerFactory.CreateLogger<UserPermissionTask>();

            static FolderEnumeratorOptions GetDefaultFolderEnumerationOptions()
            {
                return new FolderEnumeratorOptions(true, true, true, false, false, false, null);
            }

            return new UserPermissionTask(this.aclModelBuilder,
                finderFactory,
                this.aclViewProvider,
                this.directoryFolderEnumerator,
                GetDefaultFolderEnumerationOptions,
                taskLogger,
                ReportUser);
        }

        public IAclCompareTask GetAclCompareTask()
        {
            ReportTrustee ReportTrustee()
            {
                return this.configurationManager.ConfigRoot.Report.Trustee;
            }

            return new AclCompareTask(
                this.aclModelBuilder, this.aclViewProvider, this.fileManagementService,
                ReportTrustee);
        }
    }
}