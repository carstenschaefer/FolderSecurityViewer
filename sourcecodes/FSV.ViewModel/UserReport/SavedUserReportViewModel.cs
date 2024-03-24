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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Common;
    using Configuration.Abstractions;
    using Core;
    using Database.Models;
    using Resources;

    /// <summary>
    ///     Manages list of folders in a saved report.
    /// </summary>
    public class SavedUserReportViewModel : UserReportBaseViewModel, ISortable, IEquatable<SavedUserReportViewModel>
    {
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IUserReportService userReportService;
        private DataTable _allFolders;
        private ICommand _exportCommand;
        private bool _isSelected;

        public SavedUserReportViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IUserReportService userReportService,
            IConfigurationManager configurationManager,
            IExportService exportService,
            UserPermissionReport report) :
            base(report?.Folder, report?.ReportUser)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.userReportService = userReportService ?? throw new ArgumentNullException(nameof(userReportService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            this.Report = report ?? throw new ArgumentNullException(nameof(report));

            this.ReportTypeCaption = UserReport;
            this.ReportType = ReportType.SavedUser;
            this.DisplayName = string.Format(UserReportResource.SavedReportDisplayName, this.Report.Date.ToString("g"), this.Report.ReportUser);
            this.Title = string.Format(UserReportResource.SavedReportTitle, this.Report.Date.ToString("g"), this.FolderPath, this.Report.ReportUser);
        }

        internal UserPermissionReport Report { get; }

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export);

        public bool IsSelected
        {
            get => this._isSelected;
            set => this.Set(ref this._isSelected, value, nameof(this.IsSelected));
        }

        public int Id => this.Report.Id;

        public string Folder => this.Report.Folder;

        public string ReportUser => this.Report.ReportUser;

        public string Date => this.Report.Date.ToString("g");

        public string Description => this.Report.Description;

        public string User => this.Report.User;

        public ObservableCollection<SavedFolderItemViewModel> Folders { get; } = new();

        public override DataTable AllFolders
        {
            get => this._allFolders ??= this.userReportService.GetAll(this.Id);
            protected set => this._allFolders = value;
        }

        public bool Equals(SavedUserReportViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(this.Report, other.Report);
        }

        public string SortColumn { get; set; }

        public SortOrder SortDirection { get; set; }

        public ICommand SortCommand { get; private set; }

        public string GetExportSortColumn()
        {
            return SavedFolderItemViewModel.DisplayColumns[this.SortColumn];
        }

        public override async Task RefreshContentAsync()
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                if (this.Header?.IsWorking ?? false)
                {
                    return;
                }

                this.Pagination = new PaginationViewModel(this.configurationManager, this.ChangePageAsync);

                this.Header = new HeaderViewModel(this.Pagination)
                {
                    SearchCommand = new AsyncRelayCommand(this.SearchAsync)
                };

                this.SortCommand = new AsyncRelayCommand(this.SortAsync);
                this.SortDirection = SortOrder.Ascending;

                this.RaisePropertyChanged(() => this.Folders);
            });


            await this.ChainTasksAsync();
        }

        private async Task ChainTasksAsync()
        {
            this.Header.ShowProgress();
            await this.MakeResultAsync();
            this.Header.EndProgress();
        }

        private async Task MakeResultAsync()
        {
            int end, start;
            int pageNo = this.Pagination.CurrentPage == 0 ? 1 : this.Pagination.CurrentPage;

            if (this.Pagination.PageSize > 0)
            {
                end = this.Pagination.PageSize;
                start = this.Pagination.PageSize * (pageNo - 1);
            }
            else
            {
                end = this.Pagination.TotalRows;
                start = 0;
            }

            ResultViewModel<IEnumerable<SavedFolderItemViewModel>> details = await this.userReportService.GetAllReportItems(this.Report.Id, this.Header.SearchText, this.SortColumn, this.SortDirection.Equals(SortOrder.Ascending), start, end);
            int total = details.Total;

            if (pageNo == 1)
            {
                this.Pagination.TotalRows = this.LimitTotalRows(total);
            }

            IEnumerable<SavedFolderItemViewModel> items = this.LimitData(details.Result, pageNo, this.Pagination.PageSize, this.Pagination.TotalPages, total);

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Folders.Clear();

                foreach (SavedFolderItemViewModel item in items)
                {
                    this.Folders.Add(item);
                }

                bool searched = !string.IsNullOrEmpty(this.Header.SearchText);

                if (this.Folders.Count == 0)
                {
                    this.Pagination.TotalRows = 0;
                    this.Header.SearchDisabled = !searched;

                    this.Header.SetText(!searched ? UserReportResource.ReportEmptyCaption : string.Format(UserReportResource.SavedReportSearchResultsCaption, total, this.Header.SearchText));
                }
                else
                {
                    this.Header.SearchDisabled = false;
                    this.Header.SetText(
                        searched ? string.Format(total == 1 ? UserReportResource.SavedReportSearchResultCaption : UserReportResource.SavedReportSearchResultsCaption, total, this.Header.SearchText) : string.Format(total == 1 ? UserReportResource.SavedReportResultCaption : UserReportResource.SavedReportResultsCaption, total)
                    );
                }
            });
        }

        private async Task ChangePageAsync(PageChangeMode mode)
        {
            await this.ChainTasksAsync();
        }

        private async Task SearchAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object p)
        {
            await this.ChainTasksAsync();
        }

        private void Export(object _)
        {
            this.exportService.ExportUserReports(new[] { this });
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((SavedUserReportViewModel)obj);
        }

        public override int GetHashCode()
        {
            return this.Report != null ? this.Report.GetHashCode() : 0;
        }
    }
}