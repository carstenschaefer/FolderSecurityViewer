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
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Database.Abstractions;
    using Database.Models;
    using Events;
    using Microsoft.Extensions.Logging;
    using Models;
    using Permission;
    using Prism.Events;

    public class PermissionReportManager : IPermissionReportManager
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<PermissionReportManager> logger;
        private readonly IUnitOfWorkFactory workFactory;

        public PermissionReportManager(
            IEventAggregator eventAggregator,
            IUnitOfWorkFactory unitOfWorkFactory,
            IDatabaseConfigurationManager dbConfigurationManager,
            IConfigurationManager configurationManager,
            ILogger<PermissionReportManager> logger)
        {
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.workFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SavedReportItemViewModel Add(string user, string description, string folder, DataTable allPermissions, bool encrypt)
        {
            if (allPermissions == null)
            {
                throw new ArgumentNullException(nameof(allPermissions));
            }

            using IUnitOfWork work = this.workFactory.Create();

            try
            {
                var report = new PermissionReport
                {
                    User = user,
                    Description = description,
                    Date = DateTime.Now,
                    Folder = folder,
                    Encrypted = encrypt
                };

                work.PermissionReportRepository.Add(report);

                if (allPermissions.Rows.Count > 0)
                {
                    report.PermissionReportDetails = new List<PermissionReportDetail>(allPermissions.Rows.Count);

                    ConfigRoot configRoot = this.configurationManager.ConfigRoot;
                    Report rootReport = configRoot.Report;
                    ReportTrustee reportTrustee = rootReport.Trustee;

                    IEnumerable<ConfigItem> columns = reportTrustee.TrusteeGridColumns.Where(m => m.Selected);

                    foreach (DataRow row in allPermissions.Rows)
                    {
                        var detail = new PermissionReportDetail { PermissionReportId = report.Id };

                        foreach (PropertyInfo property in detail.GetType().GetProperties())
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

                        report.PermissionReportDetails.Add(detail);
                    }

                    work.PermissionDetailReportRepository.Add(report.PermissionReportDetails);
                }

                work.Commit();

                return new SavedReportItemViewModel(report, this.OnUpdate);
            }
            catch (Exception e)
            {
                work.Rollback();

                const string errorMessage = "Failed to store permission report to the database.";
                this.logger.LogError(e, errorMessage);
                throw new PermissionReportDataServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        public void Update(PermissionReport report)
        {
            using IUnitOfWork work = this.workFactory.Create();
            work.PermissionReportRepository.Update(report);
            work.Commit();
        }

        public void Delete(int id)
        {
            using IUnitOfWork work = this.workFactory.Create();
            try
            {
                work.PermissionDetailReportRepository.DeleteByReportId(id);
                work.PermissionReportRepository.Delete(id);
                work.Commit();
            }
            catch (Exception)
            {
                work.Rollback();
                throw;
            }
        }

        /// <summary>
        ///     Gets a list of permission reports of given path.
        /// </summary>
        /// <param name="path">A path of directory.</param>
        /// <returns></returns>
        public IList<SavedReportItemViewModel> Get(string path)
        {
            if (this.dbConfigurationManager.HasConfiguredDatabaseProvider() == false)
            {
                return new List<SavedReportItemViewModel>(0);
            }

            using IUnitOfWork work = this.workFactory.Create();
            IEnumerable<PermissionReport> reports = work.PermissionReportRepository.GetAll(path);
            return reports.Select(m => new SavedReportItemViewModel(m, this.OnUpdate)).ToList();
        }

        public IList<SavedReportItemViewModel> GetAll(string searchKey, string sortKey, bool ascending, int skip, int pageSize, out int total)
        {
            using IUnitOfWork work = this.workFactory.Create();
            total = 0;

            IEnumerable<PermissionReport> reportDetails = null;
            if (string.IsNullOrEmpty(sortKey))
            {
                reportDetails = work.PermissionReportRepository.GetAll(searchKey, m => m.Date, false, skip, pageSize, out total);
            }
            else if (sortKey == "SelectedFolderPath")
            {
                reportDetails = work.PermissionReportRepository.GetAll(searchKey, m => m.Folder, ascending, skip, pageSize, out total);
            }
            else if (sortKey == "Date")
            {
                reportDetails = work.PermissionReportRepository.GetAll(searchKey, m => m.Date, ascending, skip, pageSize, out total);
            }

            return (reportDetails ?? throw new InvalidOperationException())
                .Select(m => new SavedReportItemViewModel(m, this.OnUpdate))
                .ToList();
        }

        /// <summary>
        ///     Gets paged list of permission detail items.
        /// </summary>
        /// <param name="id">A saved report id.</param>
        /// <param name="skip">Number of rows to skip.</param>
        /// <param name="pageSize">Number of rows to fetch</param>
        /// <param param name="total">Total rows available in database.</param>
        /// <returns></returns>
        public IEnumerable<SavedReportDetailItemViewModel> GetAll(int id, int skip, int pageSize, out int total)
        {
            using IUnitOfWork work = this.workFactory.Create();
            PermissionReport permissionReport = work.PermissionReportRepository.Get(id);

            total = work.PermissionDetailReportRepository.GetTotalRows(id);

            IEnumerable<PermissionReportDetail> reportDetails = work.PermissionDetailReportRepository.Get(id, skip, pageSize);

            return reportDetails.Select(m => new SavedReportDetailItemViewModel(m, permissionReport.Encrypted)).ToList();
        }

        public IEnumerable<SavedReportDetailItemViewModel> GetAll(int id, string searchText, string sortColumn, bool ascending, int skip, int numberOfRows, out int total)
        {
            total = 0;
            using IUnitOfWork work = this.workFactory.Create();
            PermissionReport permissionReport = work.PermissionReportRepository.Get(id);

            IEnumerable<PermissionReportDetail> reportDetails = work.PermissionDetailReportRepository.Get(id, searchText, GetDetailReportSortExpression(sortColumn), ascending, skip, numberOfRows, out total);

            return reportDetails.Select(m => new SavedReportDetailItemViewModel(m, permissionReport.Encrypted)).ToList();
        }

        /// <summary>
        ///     Gets all rows of permission detail items in DataTable.
        /// </summary>
        /// <param name="id">A report id.</param>
        /// <returns>A <see cref="DataTable" /> object.</returns>
        public DataTable GetAll(int id)
        {
            using IUnitOfWork work = this.workFactory.Create();
            PermissionReport permissionReport = work.PermissionReportRepository.Get(id);

            var dataTable = new DataTable(FsvColumnConstants.PermissionTableName);

            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportTrustee reportTrustee = rootReport.Trustee;

            IEnumerable<ConfigItem> columns = reportTrustee.TrusteeGridColumns.Where(m => m.Selected);

            foreach (ConfigItem column in columns)
            {
                dataTable.Columns.Add(new DataColumn(column.DisplayName) { DataType = typeof(string) });
            }

            foreach (PermissionReportDetail item in work.PermissionDetailReportRepository.GetAll(id))
            {
                DataRow dataRow = dataTable.NewRow();
                PropertyInfo[] properties = item.GetType().GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    if (property.GetCustomAttributes(typeof(MapAtttribute), true).FirstOrDefault() is MapAtttribute attribute)
                    {
                        ConfigItem column = columns.FirstOrDefault(m => m.Name == attribute.Name);
                        if (column == null)
                        {
                            continue;
                        }

                        object value = property.GetValue(item);

                        dataRow[column.DisplayName] = permissionReport.Encrypted ? value?.ToString().Decrypt() : value?.ToString();
                    }
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private static Expression<Func<PermissionReportDetail, string>> GetDetailReportSortExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "AccountName":
                    return t => t.AccountName;
                case "FirstName":
                    return t => t.FirstName;
                case "LastName":
                    return t => t.LastName;
                case "Department":
                    return t => t.Department;
                case "Division":
                    return t => t.Division;
                case "Email":
                    return t => t.Email;
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

        private void OnUpdate(SavedReportItemViewModel viewModel)
        {
            this.Update(viewModel.Report);
            this.eventAggregator.GetEvent<SavedPermissionUpdatedEvent>().Publish(viewModel);
        }
    }
}