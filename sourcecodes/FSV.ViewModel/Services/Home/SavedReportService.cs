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

namespace FSV.ViewModel.Services.Home
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Database.Abstractions;
    using Database.Models;
    using Events;
    using Permission;
    using Prism.Events;
    using ViewModel.Home;
    using ViewModel.UserReport;

    public class SavedReportService : ISavedReportService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ModelBuilder<AllSavedReportListViewModel> allSavedReportListViewModelBuilder;
        private readonly IConfigurationManager configurationManager;
        private readonly ModelBuilder<SavedUserReportListViewModel> savedUserReportListViewModelBuilder;

        public SavedReportService(
            IEventAggregator eventAggregator,
            IUnitOfWorkFactory unitOfWorkFactory,
            IConfigurationManager configurationManager,
            ModelBuilder<AllSavedReportListViewModel> allSavedReportListViewModelBuilder,
            ModelBuilder<SavedUserReportListViewModel> savedUserReportListViewModelBuilder)
        {
            this._eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this._unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.allSavedReportListViewModelBuilder = allSavedReportListViewModelBuilder ?? throw new ArgumentNullException(nameof(allSavedReportListViewModelBuilder));
            this.savedUserReportListViewModelBuilder = savedUserReportListViewModelBuilder ?? throw new ArgumentNullException(nameof(savedUserReportListViewModelBuilder));
        }

        public async Task<SavedReportItems> GetSavedReports()
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            int defaultPageSize = this.configurationManager.ConfigRoot.PageSize;
            AllSavedReportListViewModel savedPermissions = await this.GetSavedPermissionsAsync(unitOfWork, defaultPageSize);
            SavedUserReportListViewModel savedUserPermissions = await this.GetSavedUserPermissionsAsync(unitOfWork, defaultPageSize);

            return new SavedReportItems(savedPermissions, savedUserPermissions);
        }

        public async Task<AllSavedReportListViewModel> GetSavedPermissionsAsync(IUnitOfWork work, int pageSize)
        {
            return await Task.Run(() =>
            {
                List<PermissionReport> reportDetails = work.PermissionReportRepository.GetAll(string.Empty, m => m.Date, false, 0, pageSize, out int total).ToList();
                IEnumerable<SavedReportItemViewModel> reportDetailViewModels = reportDetails.Select(m => new SavedReportItemViewModel(m, this.OnPermissionUpdate));
                var permissionsResult = new ResultEnumerableViewModel<SavedReportItemViewModel>(reportDetailViewModels, total);
                AllSavedReportListViewModel model = this.allSavedReportListViewModelBuilder.Build();
                return model.InitializeAsync(permissionsResult);
            });
        }

        public async Task<SavedUserReportListViewModel> GetSavedUserPermissionsAsync(IUnitOfWork work, int pageSize)
        {
            return await Task.Run(() =>
            {
                List<UserPermissionReport> reportDetails = work.UserPermissionReportRepository.GetAll(string.Empty, m => m.Date, false, 0, pageSize, out int total).ToList();
                IEnumerable<SavedUserReportListItemViewModel> reportDetailViewModels = reportDetails.Select(m => new SavedUserReportListItemViewModel(m, this.OnUserPermissionUpdate));
                var userReportsResult = new ResultEnumerableViewModel<SavedUserReportListItemViewModel>(reportDetailViewModels, total);
                SavedUserReportListViewModel model = this.savedUserReportListViewModelBuilder.Build();
                return model.InitializeAsync(userReportsResult);
            });
        }

        private void OnUserPermissionUpdate(SavedUserReportListItemViewModel viewModel)
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            unitOfWork.UserPermissionReportRepository.Update(viewModel.Report);
            unitOfWork.Commit();

            this._eventAggregator.GetEvent<SavedUserPermissionDescriptionUpdatedEvent>().Publish(viewModel);
        }

        private void OnPermissionUpdate(SavedReportItemViewModel viewModel)
        {
            using IUnitOfWork unitOfWork = this._unitOfWorkFactory.Create();
            unitOfWork.PermissionReportRepository.Update(viewModel.Report);
            unitOfWork.Commit();

            this._eventAggregator.GetEvent<SavedPermissionUpdatedEvent>().Publish(viewModel);
        }
    }
}