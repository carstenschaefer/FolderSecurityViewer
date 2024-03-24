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
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Models;

    public class ActiveDirectoryListBuilder
    {
        private readonly IConfigurationManager configurationManager;
        private readonly FsvColumnNames fsvColumnNames;
        private readonly FsvResults fsvResults;
        private int _countingPermissions;

        public ActiveDirectoryListBuilder(
            IConfigurationManager configurationManager,
            FsvResults fsvResults,
            FsvColumnNames fsvColumnNames)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.fsvResults = fsvResults ?? throw new ArgumentNullException(nameof(fsvResults));
            this.fsvColumnNames = fsvColumnNames ?? throw new ArgumentNullException(nameof(fsvColumnNames));
        }

        public void AddToList(
            Principal principal, string domain, string parentGroupName, string origin, bool useDirEntry,
            string originatingGroup,
            List<string> parentGroupsList)
        {
            var user = (UserPrincipal)principal;
            var directoryEntry = (DirectoryEntry)user.GetUnderlyingObject();

            // TODO: inject this dependency as Func<ReportTrustee>
            ReportTrustee reportTrustee = this.configurationManager.ConfigRoot.Report.Trustee;
            if (reportTrustee.Settings.ExcludeDisabledUsers && ActiveDirectoryUtility.IsAccountDisabled(directoryEntry))
            {
                return;
            }

            DataRow newRow = this.fsvResults.WorkerResults.PermissionData.NewRow();

            if (directoryEntry != null)
            {
                foreach (ConfigItem item in this.fsvResults.PermissionGridColumns)
                {
                    if (item.Selected)
                    {
                        object newVal = null;
                        if (!item.DisplayName.Equals(this.fsvColumnNames.OriginatingGroup)
                            && !item.DisplayName.Equals(this.fsvColumnNames.Rigths)
                            && !item.DisplayName.Equals(this.fsvColumnNames.Domain))
                        {
                            //property does not exist: ex. Not the best way to solve this...
                            try
                            {
                                newVal = useDirEntry ? directoryEntry.InvokeGet(item.Name) : user.Name;
                            }
                            catch (Exception)
                            {
                                newVal = $"AD Property Name {item.Name} does not exist";
                            }

                            if (newVal != null)
                            {
                                var valToSet = string.Empty;
                                if (newVal.GetType().IsArray)
                                {
                                    foreach (object innerVal in (IEnumerable)newVal)
                                    {
                                        valToSet += innerVal + "|";
                                    }
                                }
                                else
                                {
                                    valToSet = newVal.ToString();
                                }

                                newRow[item.DisplayName] = valToSet;
                            }
                        }
                    }
                }
            }

            newRow[this.fsvColumnNames.OriginatingGroup] = originatingGroup; //this.GetVal(parentGroupName);
            newRow[this.fsvColumnNames.Rigths] = this.fsvResults.GetTranslatedRight(origin);
            newRow[FsvColumnConstants.PermissionGiListColumnName] = parentGroupsList.ToList();

            if (!string.IsNullOrEmpty(this.fsvColumnNames.Domain))
            {
                newRow[this.fsvColumnNames.Domain] = domain.ToUpper();
            }

            this.fsvResults.WorkerResults.PermissionData.Rows.Add(newRow);

            this.fsvResults.Progress?.Invoke(++this._countingPermissions);
        }

        public void AddToList(Principal principal, string directoryName, string domain, string parentGroupName,
            string origin, bool useDirEntry, string originatingGroup)
        {
            var user = (UserPrincipal)principal;
            var de = (DirectoryEntry)user.GetUnderlyingObject();

            // TODO: inject this dependency as Func<ReportTrustee>
            ReportTrustee reportTrustee = this.configurationManager.ConfigRoot.Report.Trustee;
            if (reportTrustee.Settings.ExcludeDisabledUsers && ActiveDirectoryUtility.IsAccountDisabled(de))
            {
                return;
            }

            DataRow newRow = this.fsvResults.WorkerResults.PermissionData.NewRow();

            if (de != null)
            {
                //foreach (var item in _fsvResults.PermissionGridColumns)
                //{
                //  if (item.Selected)
                //  {
                //    object newVal = null;
                //    if (!item.DisplayName.Equals(_fsvColumnNames.OriginatingGroup)
                //          && !item.DisplayName.Equals(_fsvColumnNames.Rigths)
                //          && !item.DisplayName.Equals(_fsvColumnNames.Domain))
                //    {
                //      //property does not exist: ex. Not the best way to solve this...
                //      try
                //      {
                //        newVal = useDirEntry ? de.InvokeGet(item.Name) : user.Name;
                //      }
                //      catch (Exception)
                //      {
                //        newVal = String.Format("AD Property Name {0} does not exist", item.Name);
                //      }

                //      if (newVal != null)
                //      {
                //        var valToSet = string.Empty;
                //        if (newVal.GetType().IsArray)
                //        {
                //          foreach (var innerVal in (IEnumerable)newVal)
                //          {
                //            valToSet += innerVal + "|";
                //          }
                //        }
                //        else
                //        {
                //          valToSet = newVal.ToString();
                //        }

                //        newRow[item.DisplayName] = valToSet;
                //      }
                //    }
                //  }
                //}
            }

            newRow[this.fsvColumnNames.Folder] = new DirectoryInfo(directoryName).Name;
            newRow[this.fsvColumnNames.CompleteName] = directoryName;
            newRow[this.fsvColumnNames.OriginatingGroup] = originatingGroup; //this.GetVal(parentGroupName);
            newRow[this.fsvColumnNames.Rigths] = this.fsvResults.GetTranslatedRight(origin);
            //newRow["GIList"] = parentGroupsList;

            if (!string.IsNullOrEmpty(this.fsvColumnNames.Domain))
            {
                newRow[this.fsvColumnNames.Domain] = domain.ToUpper();
            }

            lock (this.fsvResults.WorkerResults.PermissionData)
            {
                this.fsvResults.WorkerResults.PermissionData.Rows.Add(newRow);
            }

            if (this.fsvResults.Progress != null)
            {
                this.fsvResults.Progress(++this._countingPermissions);
            }
        }
    }
}