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

namespace FSV.ViewModel.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Database.Abstractions;
    using Database.Models;
    using UserReport;

    public class UserPermissionReportManager : IUserPermissionReportManager
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly IConfigurationManager configurationManager;

        public UserPermissionReportManager(IUnitOfWorkFactory unitOfWorkFactory, IConfigurationManager configurationManager)
        {
            this._unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public UserPermissionReport Add(string user, string description, string folder, string reportUser, DataTable allFolders, bool encrypt)
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            try
            {
                var report = new UserPermissionReport
                {
                    User = user,
                    Description = description,
                    Date = DateTime.Now,
                    Folder = folder,
                    ReportUser = reportUser,
                    Encrypted = encrypt
                };

                unitOfWork.UserPermissionReportRepository.Add(report);

                if (allFolders.Rows.Count > 0)
                {
                    report.UserPermissionReportDetails = new List<UserPermissionReportDetail>(allFolders.Rows.Count);
                    ConfigRoot configRoot = this.configurationManager.ConfigRoot;
                    Report configRootReport = configRoot.Report;
                    IEnumerable<ConfigItem> columns = configRootReport.Trustee.TrusteeGridColumns.Where(m => m.Selected);

                    Type detailsType = typeof(UserPermissionReportDetail);

                    foreach (DataRow row in allFolders.Rows)
                    {
                        var detail = new UserPermissionReportDetail
                        {
                            UserPermissionReportId = report.Id,
                            SubFolder = report.Encrypted ? row["Folder"]?.ToString().Encrypt() : row["Folder"]?.ToString(),
                            CompleteName = report.Encrypted ? row["Complete Name"]?.ToString().Encrypt() : row["Complete Name"]?.ToString()
                        };

                        foreach (PropertyInfo property in detailsType.GetProperties())
                        {
                            if (property.GetCustomAttributes(typeof(MapAtttribute), true).FirstOrDefault() is MapAtttribute attribute)
                            {
                                ConfigItem column = columns.FirstOrDefault(m => m.Name.Equals(attribute.Name));
                                if (column == null)
                                {
                                    continue;
                                }

                                var value = row.Field<string>(column.DisplayName);

                                if (!string.IsNullOrEmpty(value))
                                {
                                    property.SetValue(detail, report.Encrypted ? value.Encrypt() : value);
                                }
                            }
                        }

                        report.UserPermissionReportDetails.Add(detail);
                    }

                    unitOfWork.UserPermissionDetailReportRepository.Add(report.UserPermissionReportDetails);
                }

                unitOfWork.Commit();

                return report;
            }
            catch (Exception)
            {
                unitOfWork.Rollback();
                throw;
            }
        }

        public void Update(UserPermissionReport report)
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            unitOfWork.UserPermissionReportRepository.Update(report);
            unitOfWork.Commit();
        }

        public void Delete(int reportId)
        {
            using IUnitOfWork work = this._unitOfWorkFactory.Create();
            work.UserPermissionDetailReportRepository.DeleteAll(reportId);
            work.UserPermissionReportRepository.Delete(reportId);
            work.Commit();
        }

        public DataTable GetAll(int id)
        {
            using IUnitOfWork work = this._unitOfWorkFactory.Create();
            UserPermissionReport permissionReport = work.UserPermissionReportRepository.Get(id);
            IReadOnlyDictionary<string, string> propertyMap = SavedFolderItemViewModel.DisplayColumns;

            var dataTable = new DataTable();

            foreach (KeyValuePair<string, string> property in propertyMap)
            {
                dataTable.Columns.Add(new DataColumn(property.Value) { DataType = typeof(string) });
            }

            foreach (SavedFolderItemViewModel item in work.UserPermissionDetailReportRepository.GetAll(id).Select(m => new SavedFolderItemViewModel(m, permissionReport.Encrypted)))
            {
                DataRow dataRow = dataTable.NewRow();
                PropertyInfo[] properties = item.GetType().GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    if (propertyMap.ContainsKey(property.Name))
                    {
                        dataRow[propertyMap[property.Name]] = property.GetValue(item);
                    }
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public IEnumerable<UserPermissionReport> GetAll(string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total)
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            total = 0;

            IEnumerable<UserPermissionReport> reportDetails = null;
            if (string.IsNullOrEmpty(sortKey))
            {
                reportDetails = unitOfWork.UserPermissionReportRepository.GetAll(searchKey, m => m.Date, false, skip, pageSize, out total);
            }
            else
            {
                reportDetails = unitOfWork.UserPermissionReportRepository.GetAll(searchKey, GetReportSortExpression(sortKey), ascending, skip, pageSize, out total);
            }

            return reportDetails.ToList();
        }

        public IEnumerable<UserPermissionReport> GetAll(string folder, string user, string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total)
        {
            total = 0;
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            IEnumerable<UserPermissionReport> reportDetails = null;
            if (string.IsNullOrEmpty(sortKey))
            {
                reportDetails = unitOfWork.UserPermissionReportRepository.GetAll(folder, user, searchKey, m => m.Date, false, skip, pageSize, out total).AsEnumerable();
            }
            else
            {
                reportDetails = unitOfWork.UserPermissionReportRepository.GetAll(folder, user, searchKey, GetReportSortExpression(sortKey), ascending, skip, pageSize, out total).AsEnumerable();
            }

            return reportDetails.ToList();
        }

        public IEnumerable<SavedFolderItemViewModel> GetAllReportItems(int reportId, string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total)
        {
            total = 0;
            using IUnitOfWork work = this._unitOfWorkFactory.Create();
            UserPermissionReport permissionReport = work.UserPermissionReportRepository.Get(reportId);
            if (permissionReport != null)
            {
                IList<UserPermissionReportDetail> reportDetails = work.UserPermissionDetailReportRepository.Get(reportId, searchKey, GetDetailReportSortExpression(sortKey), ascending, skip, pageSize, out total);
                return reportDetails.Select(m => new SavedFolderItemViewModel(m, permissionReport.Encrypted)).ToList();
            }

            return null;
        }

        public async Task<IEnumerable<UserReportDetailListItemViewModel>> GetReportAsync(int userReportId)
        {
            IEnumerable<UserReportDetailListItemViewModel> result = await Task.Run(() =>
            {
                using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
                UserPermissionReport userReport = unitOfWork.UserPermissionReportRepository.Get(userReportId);
                return unitOfWork.UserPermissionDetailReportRepository.GetAll(userReportId)
                    .Select(m => new UserReportDetailListItemViewModel(m, userReport.Encrypted));
            });

            return result;
        }

        private static Expression<Func<UserPermissionReport, string>> GetReportSortExpression(string sortKey)
        {
            switch (sortKey)
            {
                case "FolderPath":
                    return t => t.Folder;
                case "Date":
                    return t => t.Date.ToString();
                case "UserName":
                    return t => t.ReportUser;
                case "User":
                    return t => t.User;
                default:
                    return null;
            }
        }

        private static Expression<Func<UserPermissionReportDetail, string>> GetDetailReportSortExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "CompleteName":
                    return t => t.CompleteName;
                case "SubFolder":
                    return t => t.SubFolder;
                case "Domain":
                    return t => t.Domain;
                case "OriginatingGroup":
                    return t => t.OriginatingGroup;
                case "Permissions":
                    return t => t.Permissions;
                default:
                    return null;
            }
        }
    }
}