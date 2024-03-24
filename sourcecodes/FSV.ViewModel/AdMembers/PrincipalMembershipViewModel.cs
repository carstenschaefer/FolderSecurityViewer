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

namespace FSV.ViewModel.AdMembers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration.Abstractions;
    using Core;
    using Microsoft.Extensions.Logging;
    using Resources;

    public class PrincipalMembershipViewModel : FlyoutViewModel
    {
        private readonly IAdBrowserService adBrowserService;
        private readonly List<GroupMemberItemViewModel> allGroupMembers = new();

        private readonly IConfigurationManager configurationManager;
        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IFlyOutService flyOutService;
        private readonly ILogger<PrincipalMembershipViewModel> log;
        private readonly ObservableCollection<GroupMemberItemViewModel> pagedGroupMembers = new();
        private readonly object syncObject = new();
        private DataTable exportTable;

        private GroupMemberItemViewModel selectedItem;

        public PrincipalMembershipViewModel(
            IConfigurationManager configurationManager,
            IDispatcherService dispatcherService,
            IFlyOutService flyOutService,
            IAdBrowserService adBrowserService,
            ILogger<PrincipalMembershipViewModel> log,
            IExportService exportService,
            string samAccountName)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.SAMAccountName = !string.IsNullOrWhiteSpace(samAccountName) ? samAccountName : throw new ArgumentNullException(nameof(samAccountName));

            this.Width = 650;
            this.Pagination = this.CreatePagination();
            this.Header = new HeaderViewModel(this.Pagination);
            this.DisplayName = string.Format(GroupMemberResource.MembershipDisplayName, samAccountName.Contains('\\') ? samAccountName.Split('\\').Last() : samAccountName);

            this.ShowMembershipsCommand = new AsyncRelayCommand(this.ShowMembershipsAsync, this.CanShowMembershipsAsync);
            this.ExportCommand = new RelayCommand(this.Export, this.CanExport);
        }

        public PaginationViewModel Pagination { get; }

        public string SAMAccountName { get; }

        public IList<GroupMemberItemViewModel> Groups => this.pagedGroupMembers;

        public GroupMemberItemViewModel SelectedItem
        {
            get => this.selectedItem;
            set => this.Set(ref this.selectedItem, value, nameof(this.SelectedItem));
        }

        public ICommand ShowMembershipsCommand { get; }
        public ICommand ExportCommand { get; }

        public override async Task InitializeAsync()
        {
            await this.ChainTasksAsync(true);
        }

        public DataTable GetExportTable()
        {
            if (this.exportTable != null)
            {
                return this.exportTable;
            }

            var table = new DataTable();
            table.Columns.Add(GroupMemberResource.AccountNameCaption);
            table.Columns.Add(GroupMemberResource.TypeCaption);
            table.Columns.Add(GroupMemberResource.OUCaption);
            table.Columns.Add(GroupMemberResource.DomainCaption);

            lock (this.syncObject)
            {
                foreach (GroupMemberItemViewModel item in this.allGroupMembers)
                {
                    table.Rows.Add(
                        item.AccountName,
                        item.AccountType,
                        item.OU,
                        item.Domain);
                }
            }

            this.exportTable = table;

            return this.exportTable;
        }

        private async Task ChainTasksAsync(bool loading = false)
        {
            await this.dispatcherService.InvokeAsync(this.StartOfChainTasks);

            if (loading)
            {
                bool result = await this.LoadMembershipsAsync();
                if (result)
                {
                    await this.DoChangePageAsync(this.Pagination.CurrentPage);
                }
            }
            else
            {
                await this.DoChangePageAsync(this.Pagination.CurrentPage);
            }

            await this.dispatcherService.InvokeAsync(this.EndOfChainTasks);
        }

        private void StartOfChainTasks()
        {
            this.Header.ShowProgress();
            this.Header.SetText(CommonResource.LoadingText);
        }

        private void EndOfChainTasks()
        {
            int count;

            lock (this.syncObject)
            {
                count = this.allGroupMembers.Count;
            }

            if (count == 0)
            {
                this.Header.SetText(GroupMemberResource.MembershipListEmptyText, true);
            }
            else if (count == 1)
            {
                this.Header.SetText(GroupMemberResource.MembershipOneGroupCaption);
            }
            else
            {
                this.Header.SetText(string.Format(GroupMemberResource.MembershipTotalGroupsCaption, count));
            }

            this.Header.EndProgress();
        }

        private async Task<bool> LoadMembershipsAsync()
        {
            try
            {
                IEnumerable<GroupMemberItemViewModel> groups = await this.adBrowserService.GetMembershipListAsync(this.SAMAccountName, QueryType.SamAccountName);
                var count = 0;

                lock (this.syncObject)
                {
                    this.allGroupMembers.AddRange(groups);
                    count = this.allGroupMembers.Count;
                }

                this.Pagination.TotalRows = count;

                return count > 0;
            }
            catch (ActiveDirectoryServiceException ex)
            {
                this.log.LogError(ex, "Failed to fetch membership list of {SAMAccountName} due to unhandled Active Directory error.", this.SAMAccountName);
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to fetch membership list of {SAMAccountName}.", this.SAMAccountName);
            }

            return false;
        }

        private async Task DoChangePageAsync(int pageNo)
        {
            int skip = this.Pagination.PageSize * (pageNo - 1);

            IEnumerable<GroupMemberItemViewModel> theList;
            lock (this.syncObject)
            {
                theList = this.allGroupMembers.Skip(skip).Take(this.Pagination.PageSize);
            }

            void DispatchFillFromTheList()
            {
                lock (this.syncObject)
                {
                    this.pagedGroupMembers.Clear();
                    foreach (GroupMemberItemViewModel item in theList)
                    {
                        this.pagedGroupMembers.Add(item);
                    }
                }
            }

            await this.dispatcherService.InvokeAsync(DispatchFillFromTheList);
        }

        private PaginationViewModel CreatePagination()
        {
            async Task PageChangeAsyncCallback(PageChangeMode p)
            {
                await this.ChainTasksAsync();
            }

            return new PaginationViewModel(this.configurationManager, PageChangeAsyncCallback)
            {
                ShowText = GroupMemberResource.MembershipPagesCaption
            };
        }

        private async Task ShowMembershipsAsync(object _)
        {
            await this.flyOutService.ShowAsync<PrincipalMembershipViewModel>(this.SelectedItem.AccountName);
        }

        private async Task<bool> CanShowMembershipsAsync(object _)
        {
            return await Task.FromResult(this.SelectedItem != null);
        }

        private bool CanExport(object _)
        {
            lock (this.syncObject)
            {
                return this.allGroupMembers.Count > 0;
            }
        }

        private void Export(object _)
        {
            this.exportService.ExportPrincipalMemberships(new[] { this });
        }
    }
}