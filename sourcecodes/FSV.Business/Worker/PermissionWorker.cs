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

    public class PermissionWorker : BackgroundWorker
    {
        private readonly ActiveDirectory _adService;
        private readonly FsvResults _fsvResults;
        private readonly string _scanDirectory;
        private readonly IAclModelBuilder aclModelBuilder;
        private readonly IAclViewProvider aclViewProvider;
        private readonly ILogger<PermissionWorker> logger;
        private FsvColumnNames _fsvColumnNames;

        public PermissionWorker(
            IAclModelBuilder aclModelBuilder,
            IAclViewProvider aclViewProvider,
            IConfigurationManager configurationManager,
            ActiveDirectoryFactory activeDirectoryFactory,
            ILogger<PermissionWorker> logger,
            string scanDirectory)
        {
            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }

            this.aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._scanDirectory = scanDirectory;

            ConfigRoot configRoot = configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportTrustee reportTrustee = rootReport.Trustee;

            this._fsvResults = new FsvResults(this.logger)
            {
                AlreadyScannedGroups = new List<string>(),
                ExclusionGroups = reportTrustee.ExclusionGroups.ToList(),
                //BuiltInGroups = this.GetBuiltInGroup(),
                BuiltInGroups = reportTrustee.ExcludedBuiltInGroups.Where(m => m.Excluded).Select(m => m.Sid).ToList(),
                SkipBuiltInGroups = true, // Configuration.ConfigurationManager.GetSkipBuiltInGroups();
                PermissionGridColumns = reportTrustee.TrusteeGridColumns.ToList(),
                TranslatedItems = reportTrustee.RightsTranslations.ToList(),
                WorkerResults = new PermissionWorkerResult(),
                Progress = this.ReportProgress
            };

            this.GetGridColumnNames();

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;

            this._adService = activeDirectoryFactory.Create(this._fsvResults, this._fsvColumnNames);
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            // GetPermissionView(e.Argument.ToString());
            this.GetPermissionView(this._scanDirectory);

            if (this.CancellationPending)
            {
                e.Result = null;
            }
            else
            {
                this._fsvResults.WorkerResults.RowCount = this._fsvResults.WorkerResults.PermissionData.Rows.Count;
                e.Result = this._fsvResults.WorkerResults;
            }
        }

        private bool GetCancellationPending()
        {
            return this.CancellationPending;
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

                    this._fsvResults.WorkerResults.PermissionData.Columns.Add(item.DisplayName);
                }

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

                this._fsvResults.WorkerResults.PermissionData.Columns.Add(new DataColumn(FsvColumnConstants.PermissionGiListColumnName, typeof(List<string>)));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to obtain grid column-names due to an unhandled error.");
            }
        }

        private void GetPermissionView(string dirName)
        {
            try
            {
                IEnumerable<IAclModel> items = this.aclViewProvider.GetAclView(dirName).Select(this.aclModelBuilder.Build);
                foreach (IAclModel item in items)
                {
                    bool skipScan = this._fsvResults.CheckExclusionGroups(item.Account, string.Empty);

                    if (!skipScan)
                    {
                        var fileSystemRight = $"{item.TypeString}: {item.RightsString}";
                        this._adService.FindAdObject(this.GetCancellationPending, item.Account, item.InheritanceFlagsString, fileSystemRight, dirName.GetServerName(), string.Empty, item.Account);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to retrieve permission view due to an unhandled error.");
            }
        }

        private List<string> GetBuiltInGroup()
        {
            //https://support.microsoft.com/en-us/kb/243330
            //http://serverfault.com/questions/80924/what-are-the-five-built-in-groups-in-windows-server-2008

            var grps = new List<string>
            {
                "S-1-1-0",
                "S-1-2",
                "S-1-3",
                "S-1-3-0",
                "S-1-3-1",
                "S-1-3-2",
                "S-1-3-3",
                "S-1-4",
                "S-1-5",
                "S-1-5-1",
                "S-1-5-2",
                "S-1-5-3",
                "S-1-5-4",
                "S-1-5-6",
                "S-1-5-7",
                "S-1-5-11",
                "S-1-5-18",
                "S-1-5-19",
                "S-1-5-20",
                "S-1-5-32-544",
                "S-1-5-32-545",
                "S-1-5-32-546",
                "S-1-5-32-547",
                "S-1-5-32-548",
                "S-1-5-32-549",
                "S-1-5-32-550",
                "S-1-5-32-551",
                "S-1-5-32-552"
            };
            return grps;
        }
    }
}