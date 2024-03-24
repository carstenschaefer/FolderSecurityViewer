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

namespace FSV.ViewModel.UserReport
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Core;
    using Microsoft.Extensions.Logging;
    using Passables;
    using Prism.Events;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class SavedFolderUserReportListViewModel : SavedUserReportListViewModel
    {
        private readonly IUserReportService userReportService;

        public SavedFolderUserReportListViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IUserReportService userReportService,
            IConfigurationManager configurationManager,
            ILogger<SavedUserReportViewModel> logger,
            UserPath userPath)
            : base(dialogService, dispatcherService, eventAggregator, navigationService, userReportService, configurationManager, logger, userPath)
        {
            this.userReportService = userReportService ?? throw new ArgumentNullException(nameof(userReportService));
            this.ReportType = ReportType.SavedUser;
            this.ReportTypeCaption = UserReport;

            this.Closable = true;

            this.InitializeAsync().FireAndForgetSafeAsync();
        }

        protected override async Task<ResultEnumerableViewModel<SavedUserReportListItemViewModel>> GetListItemsAsync(string searchKey, string sortKey, bool ascending, int skip, int pageSize)
        {
            return await this.userReportService.GetAll(this.FolderPath, this.UserName, this.Header.SearchText, this.SortColumn, ascending, skip, this.Pagination.PageSize);
        }
    }
}