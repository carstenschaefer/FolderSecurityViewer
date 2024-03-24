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

namespace FSV.Business.Worker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Linq;
    using AdServices;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;
    using Resources;

    public class UserWorker : BackgroundWorker
    {
        private readonly ActiveDirectory _adService;
        private readonly FsvResults _fsvResults;
        private readonly string _scanDirectory;
        private readonly string _user;
        private readonly IAclViewProvider aclViewProvider;
        private readonly IFileManagementService fileManagementService;
        private readonly ILogger<UserWorker> logger;

        private readonly IAclModelBuilder modelBuilder;
        private readonly int scanDepth;
        private FsvColumnNames _fsvColumnNames;
        private int _scanLevel = 1;

        public UserWorker(
            IAclModelBuilder modelBuilder,
            IAclViewProvider aclViewProvider,
            IFileManagementService fileManagementService,
            IConfigurationManager configurationManager,
            ActiveDirectoryFactory activeDirectoryFactory,
            ILogger<UserWorker> logger,
            string scanDirectory, string user)
        {
            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }

            if (activeDirectoryFactory == null)
            {
                throw new ArgumentNullException(nameof(activeDirectoryFactory));
            }

            if (string.IsNullOrWhiteSpace(scanDirectory))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(scanDirectory));
            }

            this.modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._scanDirectory = scanDirectory;
            this._user = user;

            ConfigRoot configRoot = configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportTrustee reportTrustee = rootReport.Trustee;

            this.scanDepth = rootReport.User.ScanLevel;

            this._fsvResults = new FsvResults(this.logger)
            {
                AlreadyScannedGroups = new List<string>(),
                ExclusionGroups = new List<ConfigItem>(),
                BuiltInGroups = reportTrustee.ExcludedBuiltInGroups.Where(m => m.Excluded).Select(m => m.Sid).ToList(),
                SkipBuiltInGroups = true,
                PermissionGridColumns = reportTrustee.TrusteeGridColumns.ToList(),
                TranslatedItems = reportTrustee.RightsTranslations.ToList(),
                WorkerResults = new PermissionWorkerResult(),
                Progress = this.ReportProgress
            };

            this.GetGridColumnNames();

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;

            this._adService = activeDirectoryFactory.Create(this._fsvResults, this._fsvColumnNames, this._user);
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            this.GetPermissionView(this._scanDirectory);

            if (this.CancellationPending)
            {
                e.Result = null;
            }
            else
            {
                PermissionWorkerResult permissionWorkerResult = this._fsvResults.WorkerResults;
                permissionWorkerResult.RowCount = permissionWorkerResult.PermissionData.Rows.Count;
                e.Result = permissionWorkerResult;
            }

            base.OnDoWork(e);
        }

        private bool GetCancellationPending()
        {
            return this.CancellationPending;
        }

        private void GetPermissionView(string dirName)
        {
            if (this.fileManagementService.IsAccessDenied(dirName))
            {
                return;
            }

            IEnumerable<IAcl> aclList = this.aclViewProvider.GetAclView(dirName);

            foreach (IAcl acl in aclList)
            {
                if (this.GetCancellationPending())
                {
                    break;
                }

                bool skipScan = this._fsvResults.CheckExclusionGroups(acl.AccountName, string.Empty);

                if (skipScan)
                {
                    break;
                }

                IAclModel aclView = this.modelBuilder.Build(acl);

                bool userFound = this._adService.FindAdObjectForUser(this.GetCancellationPending,
                    dirName,
                    aclView.Account,
                    aclView.InheritanceFlagsString,
                    aclView.TypeString + ": " + aclView.RightsString,
                    dirName.GetServerName(),
                    string.Empty,
                    aclView.Account); // here we need the originating group!

                if (userFound)
                {
                    break;
                }
            }

            if (this.scanDepth != 0 && this._scanLevel > this.scanDepth)
            {
                return;
            }

            if (this.fileManagementService.HasSubFolders(dirName))
            {
                ++this._scanLevel;
                this.IterateSubDirectories(dirName);
                --this._scanLevel;
            }
        }

        private void IterateSubDirectories(string dirName)
        {
            try
            {
                IFolder[] folders = this.fileManagementService.GetDirectories(dirName).ToArray();
                Array.ForEach(folders, item =>
                {
                    if (this.GetCancellationPending())
                    {
                        return;
                    }

                    this.GetPermissionView(item.FullName);
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to iterate sub-directories due to an unhandled error.");
            }
        }

        private void GetGridColumnNames()
        {
            try
            {
                this._fsvColumnNames = new FsvColumnNames();

                List<ConfigItem> listOfFields = this._fsvResults.PermissionGridColumns.Where(p => p.Selected).ToList();

                foreach (ConfigItem item in listOfFields)
                {
                    if (item.Name == FsvColumnConstants.OriginatingGroup)
                    {
                        this._fsvColumnNames.OriginatingGroup = item.DisplayName;
                    }

                    if (item.Name == FsvColumnConstants.Rigths)
                    {
                        this._fsvColumnNames.Rigths = item.DisplayName;
                    }

                    if (item.Name == FsvColumnConstants.Domain)
                    {
                        this._fsvColumnNames.Domain = item.DisplayName;
                    }
                }

                this._fsvResults.WorkerResults.PermissionData.Columns.Add(new DataColumn(UserReportResource.UserReportFolderCaption));
                this._fsvResults.WorkerResults.PermissionData.Columns.Add(new DataColumn(UserReportResource.UserReportNameCaption));

                this._fsvColumnNames.CompleteName = UserReportResource.UserReportNameCaption;
                this._fsvColumnNames.Folder = UserReportResource.UserReportFolderCaption;

                if (!this._fsvResults.WorkerResults.PermissionData.Columns.Contains(this._fsvColumnNames.OriginatingGroup))
                {
                    this._fsvResults.WorkerResults.PermissionData.Columns.Add(this._fsvColumnNames.OriginatingGroup);
                }

                if (!this._fsvResults.WorkerResults.PermissionData.Columns.Contains(this._fsvColumnNames.Rigths))
                {
                    this._fsvResults.WorkerResults.PermissionData.Columns.Add(this._fsvColumnNames.Rigths);
                }

                if (!string.IsNullOrEmpty(this._fsvColumnNames.Domain))
                {
                    if (!this._fsvResults.WorkerResults.PermissionData.Columns.Contains(this._fsvColumnNames.Domain))
                    {
                        this._fsvResults.WorkerResults.PermissionData.Columns.Add(this._fsvColumnNames.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed retrieve grid column-names due to an unhandled error.");
            }
        }
    }
}