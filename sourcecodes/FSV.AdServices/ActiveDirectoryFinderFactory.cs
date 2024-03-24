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

namespace FSV.AdServices
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Abstractions;
    using Configuration;
    using Configuration.Sections.ConfigXml;
    using Microsoft.Extensions.Logging;
    using Models;

    public class ActiveDirectoryFinderFactory : IActiveDirectoryFinderFactory
    {
        private const string FolderColumn = "Folder";
        private const string CompleteNameColumn = "CompleteName";

        private readonly Func<ReportTrustee> _configReportTrustee;
        private readonly FsvColumnNames _fsvColumnNames;
        private readonly ILogger<ActiveDirectoryFinderFactory> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IActiveDirectoryState state;
        private readonly IActiveDirectoryUtility utility;

        public ActiveDirectoryFinderFactory(
            IActiveDirectoryState state,
            IActiveDirectoryUtility utility,
            ILoggerFactory loggerFactory,
            ILogger<ActiveDirectoryFinderFactory> logger,
            Func<ReportTrustee> configReportTrustee,
            FsvColumnNames fsvColumnNames)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.utility = utility ?? throw new ArgumentNullException(nameof(utility));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._configReportTrustee = configReportTrustee ?? throw new ArgumentNullException(nameof(configReportTrustee));
            this._fsvColumnNames = fsvColumnNames ?? throw new ArgumentNullException(nameof(fsvColumnNames));

            this.SetColumnNames();
        }

        public IActiveDirectoryFinder CreateActiveDirectoryFinder<T>(
            ActiveDirectoryScanResult<T> finderResult)
        {
            if (finderResult == null)
            {
                throw new ArgumentNullException(nameof(finderResult));
            }

            ReportTrustee reportTrustee = this._configReportTrustee();
            ActiveDirectoryScanOptions finderOptions = this.GetActiveDirectoryScanOptions(reportTrustee);

            this.FillResultDataColumns(finderResult, finderOptions);

            ILogger<ActiveDirectoryFinder<T>> finderLogger = this.loggerFactory.CreateLogger<ActiveDirectoryFinder<T>>();
            return new ActiveDirectoryFinder<T>(
                this.state, this.utility, finderLogger, reportTrustee.Settings.ExcludeDisabledUsers, finderOptions, this._fsvColumnNames,
                finderResult);
        }

        public IUserActiveDirectoryFinder CreateUserActiveDirectoryFinder<T>(
            string userName,
            ActiveDirectoryScanResult<T> finderResult)
        {
            try
            {
                ReportTrustee reportTrustee = this._configReportTrustee();
                ActiveDirectoryScanOptions finderOptions = this.GetActiveDirectoryScanOptions(reportTrustee);

                this.FillUserResultDataColumns(finderResult);

                ILogger<UserActiveDirectoryFinder<T>> finderLogger = this.loggerFactory.CreateLogger<UserActiveDirectoryFinder<T>>();
                return new UserActiveDirectoryFinder<T>(
                    this.state, this.utility, finderLogger,
                    userName, reportTrustee.Settings.ExcludeDisabledUsers, finderOptions, this._fsvColumnNames,
                    finderResult);
            }
            catch (Exception e)
            {
                var errorMessage = $"Failed to create an {nameof(UserActiveDirectoryFinder<T>)} instance due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new ActiveDirectoryFinderFactoryException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        public void Clear()
        {
            this.state.PrincipalContextsCache.Clear();
            this.state.ActiveDirectoryGroupPrincipalCache.Clear();
        }

        private void SetColumnNames()
        {
            ReportTrustee reportTrustee = this._configReportTrustee();
            if (reportTrustee == null)
            {
                throw new InvalidOperationException("Failed to retrieve required report-trustee.");
            }

            IEnumerable<ConfigItem> listOfFields = reportTrustee.TrusteeGridColumns.Where(m => m.Selected);

            foreach (ConfigItem item in listOfFields)
            {
                if (item.Name == FsvColumnConstants.OriginatingGroup)
                {
                    this._fsvColumnNames.OriginatingGroup = item.DisplayName;
                }
                else if (item.Name == FsvColumnConstants.Rigths)
                {
                    this._fsvColumnNames.Rigths = item.DisplayName;
                }
                else if (item.Name == FsvColumnConstants.Domain)
                {
                    this._fsvColumnNames.Domain = item.DisplayName;
                }
            }
        }

        private ActiveDirectoryScanOptions GetActiveDirectoryScanOptions(ReportTrustee reportTrustee)
        {
            if (reportTrustee == null)
            {
                throw new InvalidOperationException("Failed to retrieve required report-trustee.");
            }

            ILogger<ActiveDirectoryScanOptions> scanOptionsLogger = this.loggerFactory.CreateLogger<ActiveDirectoryScanOptions>();
            return new ActiveDirectoryScanOptions(scanOptionsLogger)
            {
                SkipBuiltInGroups = true,
                ExclusionGroups = reportTrustee.ExclusionGroups.ToList(),
                BuiltInGroups = reportTrustee.ExcludedBuiltInGroups
                    .Where(m => m.Excluded)
                    .Select(m => m.Sid)
                    .ToList(),
                PermissionGridColumns = reportTrustee.TrusteeGridColumns.Where(m => m.Selected).ToList(),
                TranslatedItems = reportTrustee.RightsTranslations.ToList()
            };
        }

        private void FillResultDataColumns<T>(ActiveDirectoryScanResult<T> finderResult, ActiveDirectoryScanOptions finderOptions)
        {
            foreach (ConfigItem item in finderOptions.PermissionGridColumns)
            {
                finderResult.Result.Columns.Add(item.DisplayName);
            }

            if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.OriginatingGroup))
            {
                finderResult.Result.Columns.Add(this._fsvColumnNames.OriginatingGroup);
            }

            if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.Rigths))
            {
                finderResult.Result.Columns.Add(this._fsvColumnNames.Rigths);
            }

            if (!string.IsNullOrEmpty(this._fsvColumnNames.Domain))
            {
                if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.Domain))
                {
                    finderResult.Result.Columns.Add(this._fsvColumnNames.Domain);
                }
            }

            finderResult.Result.Columns.Add(new DataColumn(FsvColumnConstants.PermissionGiListColumnName, typeof(List<string>)));
        }

        private void FillUserResultDataColumns<T>(ActiveDirectoryScanResult<T> finderResult)
        {
            finderResult.Result.Columns.Add(new DataColumn(this._fsvColumnNames.Folder));
            finderResult.Result.Columns.Add(new DataColumn(this._fsvColumnNames.CompleteName));

            if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.OriginatingGroup))
            {
                finderResult.Result.Columns.Add(this._fsvColumnNames.OriginatingGroup);
            }

            if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.Rigths))
            {
                finderResult.Result.Columns.Add(this._fsvColumnNames.Rigths);
            }

            if (!string.IsNullOrEmpty(this._fsvColumnNames.Domain))
            {
                if (!finderResult.Result.Columns.Contains(this._fsvColumnNames.Domain))
                {
                    finderResult.Result.Columns.Add(this._fsvColumnNames.Domain);
                }
            }
        }
    }
}