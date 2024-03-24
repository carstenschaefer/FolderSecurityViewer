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

namespace FSV.ViewModel.Services.UserReport
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Business.Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Configuration.Database;
    using Database.Models;
    using Events;
    using Microsoft.Extensions.Logging;
    using Prism.Events;
    using ViewModel.UserReport;

    public class UserReportService : IUserReportService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IUserPermissionReportManager _reportManager;
        private readonly IDatabaseConfigurationManager dbConfigurationManager;
        private readonly ILogger<UserReportService> logger;

        public UserReportService(
            IUserPermissionReportManager reportManager,
            IEventAggregator eventAggregator,
            IDatabaseConfigurationManager dbConfigurationManager,
            ILogger<UserReportService> logger)
        {
            this._reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
            this._eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.dbConfigurationManager = dbConfigurationManager ?? throw new ArgumentNullException(nameof(dbConfigurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<UserPermissionReport> Save(string savedByUserName, UserReportViewModel viewModel)
        {
            return Task.Run(() =>
            {
                try
                {
                    BaseConfiguration configuration = this.dbConfigurationManager.Config;
                    UserPermissionReport report = this._reportManager.Add(savedByUserName, null, viewModel.FolderPath, viewModel.UserName, viewModel.AllFolders, configuration.Encrypted);
                    this.logger.LogInformation("User permission report for folder {Folder} and user {User} has been saved in database.", viewModel.FolderPath, viewModel.UserName);
                    return report;
                }
                catch (Exception ex)
                {
                    const string errorMessage = "Failed to save user-permission-report due to an unhandled error.";
                    this.logger.LogError(ex, errorMessage);
                    throw new BusinessServiceException($"{errorMessage} See inner exception for further information.", ex);
                }
            });
        }

        public DataTable GetAll(int id)
        {
            return this._reportManager.GetAll(id);
        }

        public Task<ResultEnumerableViewModel<SavedUserReportListItemViewModel>> GetAll(string searchKey, string sortKey, bool ascending, int skip, int pageSize)
        {
            return Task.Run(() =>
            {
                IEnumerable<UserPermissionReport> reportItems = this._reportManager.GetAll(searchKey, sortKey, ascending, skip, pageSize, out int total);
                IEnumerable<SavedUserReportListItemViewModel> viewModelItems = reportItems.Select(m => new SavedUserReportListItemViewModel(m, this.OnDescriptionUpdate));
                return new ResultEnumerableViewModel<SavedUserReportListItemViewModel>(viewModelItems, total);
            });
        }

        public Task<ResultEnumerableViewModel<SavedUserReportListItemViewModel>> GetAll(string folder, string user, string searchKey, string sortKey, bool ascending, int skip, int pageSize)
        {
            return Task.Run(() =>
            {
                IEnumerable<UserPermissionReport> reportItems = this._reportManager.GetAll(folder, user, searchKey, sortKey, ascending, skip, pageSize, out int total);
                IEnumerable<SavedUserReportListItemViewModel> viewModelItems = reportItems.Select(m => new SavedUserReportListItemViewModel(m, this.OnDescriptionUpdate));
                return new ResultEnumerableViewModel<SavedUserReportListItemViewModel>(viewModelItems, total);
            });
        }

        public Task<ResultViewModel<IEnumerable<SavedFolderItemViewModel>>> GetAllReportItems(int reportId, string searchKey, string sortKey, bool ascending, int skip, int pageSize)
        {
            return Task.Run(() =>
            {
                IEnumerable<SavedFolderItemViewModel> reportItems = this._reportManager.GetAllReportItems(reportId, searchKey, sortKey, ascending, skip, pageSize, out int total);
                return new ResultViewModel<IEnumerable<SavedFolderItemViewModel>>(reportItems, total);
            });
        }

        public async Task<List<SavedUserReportListItemViewModel>> DeleteAsync(IEnumerable<SavedUserReportListItemViewModel> reports)
        {
            return await Task.Run(() =>
            {
                var removedItems = new List<SavedUserReportListItemViewModel>(reports.Count());

                try
                {
                    foreach (SavedUserReportListItemViewModel report in reports)
                    {
                        this._reportManager.Delete(report.Id);
                        removedItems.Add(report);
                        this.logger.LogInformation("{Folder} dated {Date} has been removed from saved user report.", report.Folder, report.Date);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error occured while removing saved users reports due to an unhandled error.");
                }

                return removedItems;
            });
        }

        private void OnDescriptionUpdate(SavedUserReportListItemViewModel savedReport)
        {
            this._reportManager.Update(savedReport.Report);
            this._eventAggregator.GetEvent<SavedUserPermissionDescriptionUpdatedEvent>().Publish(savedReport);
        }
    }
}