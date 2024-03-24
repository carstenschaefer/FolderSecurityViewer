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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Resources;

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class GroupPermissionsViewModel : PermissionReportBaseViewModel, ISortable, IEquatable<GroupPermissionsViewModel>
    {
        private readonly DataTable _allPermissions;
        private readonly IDispatcherService _dispatcherService;
        private readonly IExportService _exportService;
        private readonly HeaderViewModel _header;
        private readonly PaginationViewModel _pagination;
        private readonly string _path;
        private readonly object _syncObject = new();

        private ICommand _exportCommand;
        private ICommand _sortCommand;

        public GroupPermissionsViewModel(
            IDispatcherService dispatcherService,
            IExportService exportService,
            ModelBuilder<Func<PageChangeMode, Task>, string, PaginationViewModel> paginationViewModelBuilder,
            DataTable permissions,
            string path) : base(path)
        {
            if (paginationViewModelBuilder is null)
            {
                throw new ArgumentNullException(nameof(paginationViewModelBuilder));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            this._dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this._exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this._allPermissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            this._path = path;

            this.ReportType = ReportType.Permission;
            this.ReportTypeCaption = HomeResource.GroupPermissionsReportCaption;
            this.DisplayName = HomeResource.GroupPermissionsReportCaption;

            this._pagination = paginationViewModelBuilder.Build(this.ChangePageAsync, PermissionResource.GroupPermissionsPagesCaption);
            this._header = new HeaderViewModel(this._pagination);

            this.Header = this._header;
            this.AllPermissions = this._allPermissions;
            this.PagedPermissions = this._allPermissions.Clone();
        }

        public DataTable PagedPermissions { get; }

        public override ICommand ExportCommand => this._exportCommand ??= new RelayCommand(this.Export, this.CanExport);

        public bool Equals(GroupPermissionsViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this._path == other._path;
        }

        public string SortColumn { get; set; }
        public SortOrder SortDirection { get; set; }
        public ICommand SortCommand => this._sortCommand ??= new AsyncRelayCommand(this.SortAsync);

        public string GetExportSortColumn()
        {
            return this.SortColumn;
        }

        public override async Task InitializeAsync()
        {
            await this.ChainTasksAsync(true);
        }

        private async Task ChangePageAsync(PageChangeMode _)
        {
            await this.ChainTasksAsync();
        }

        private async Task SortAsync(object _)
        {
            await this.ChainTasksAsync();
        }

        private void Export(object _)
        {
            this._exportService.ExportPermissionReports(new[] { this });
        }

        private bool CanExport(object _)
        {
            lock (this._syncObject)
            {
                return this._allPermissions.Rows.Count > 0;
            }
        }

        private async Task ChainTasksAsync(bool isInit = false)
        {
            this._header.ShowProgress();

            if (isInit)
            {
                this.SetPagination();
            }

            await this.MakeResultAsync();

            this._header.EndProgress();
        }

        private void SetPagination()
        {
            lock (this._syncObject)
            {
                int totalRows = this._allPermissions.Rows.Count;
                string text = totalRows == 1 ? PermissionResource.GroupPermissionsResultText : PermissionResource.GroupPermissionsResultsText;

                this._pagination.TotalRows = totalRows;
                this._header.SetText(string.Format(text, totalRows));
            }
        }

        private async Task MakeResultAsync()
        {
            lock (this._syncObject)
            {
                if (this._allPermissions.Rows.Count == 0)
                {
                    return;
                }
            }

            int end, start;
            int pageNo = this._pagination.CurrentPage == 0 ? 1 : this._pagination.CurrentPage;

            if (this._pagination.PageSize > 0)
            {
                end = this._pagination.PageSize * pageNo;
                start = this._pagination.PageSize * (pageNo - 1);
            }
            else
            {
                end = this._pagination.TotalRows;
                start = 0;
            }

            void FillPagedPermissions()
            {
                string sortExpression = string.IsNullOrEmpty(this.SortColumn) ? string.Empty : $"{this.SortColumn} {this.SortDirection.ToShortString()}";

                lock (this._syncObject)
                {
                    DataRow[] rows = this._allPermissions.Select(string.Empty, sortExpression);
                    this.PagedPermissions.Clear();

                    for (int index = start; index < end; index++)
                    {
                        if (index == this._allPermissions.Rows.Count)
                        {
                            break;
                        }

                        DataRow newRow = this.PagedPermissions.NewRow();
                        newRow.ItemArray = rows[index].ItemArray;
                        this.PagedPermissions.Rows.Add(newRow);
                    }
                }

                this.RaisePropertyChanged(nameof(this.PagedPermissions));
            }

            await this._dispatcherService.InvokeAsync(FillPagedPermissions);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is GroupPermissionsViewModel other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            return this._path != null ? this._path.GetHashCode() : 0;
        }
    }
}