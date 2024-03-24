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

    public class GroupMembersViewModel : FlyoutViewModel
    {
        private readonly IAdBrowserService adBrowserService;
        private readonly List<GroupMemberItemViewModel> allGroupMembers = new();
        private readonly IConfigurationManager configurationManager;

        private readonly IDispatcherService dispatcherService;
        private readonly IExportService exportService;
        private readonly IFlyOutService flyOutService;
        private readonly ILogger<GroupMembersViewModel> log;
        private readonly ObservableCollection<GroupMemberItemViewModel> pagedGroupMembers = new();
        private readonly object syncObject = new();
        private DataTable exportTable;

        private GroupMemberItemViewModel selectedItem;

        public GroupMembersViewModel(
            IDispatcherService dispatcherService,
            IFlyOutService flyOutService,
            IConfigurationManager configurationManager,
            IAdBrowserService adBrowserService,
            ILogger<GroupMembersViewModel> log,
            IExportService exportService,
            string account)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));
            ;
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.Account = !string.IsNullOrWhiteSpace(account) ? account : throw new ArgumentNullException(nameof(account));

            this.Width = 650;
            this.Pagination = this.CreatePagination();
            this.Header = new HeaderViewModel(this.Pagination);
            this.DisplayName = string.Format(GroupMemberResource.DisplayName, account.Contains('\\') ? account.Split('\\').Last() : account);

            this.ShowGroupMembersCommand = new AsyncRelayCommand(this.ShowGroupMembersAsync, this.CanShowGroupMembersAsync);
            this.ExportCommand = new RelayCommand(this.Export, this.CanExport);
        }

        public string Account { get; }

        public PaginationViewModel Pagination { get; }

        public IList<GroupMemberItemViewModel> GroupMembers => this.pagedGroupMembers;

        public GroupMemberItemViewModel SelectedItem
        {
            get => this.selectedItem;
            set => this.Set(ref this.selectedItem, value, nameof(this.SelectedItem));
        }

        public ICommand ShowGroupMembersCommand { get; }
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
            table.Columns.Add(GroupMemberResource.DisplayNameCaption);
            table.Columns.Add(GroupMemberResource.TypeCaption);
            table.Columns.Add(GroupMemberResource.OUCaption);
            table.Columns.Add(GroupMemberResource.DomainCaption);

            lock (this.syncObject)
            {
                foreach (GroupMemberItemViewModel item in this.allGroupMembers)
                {
                    table.Rows.Add(
                        item.AccountName,
                        item.DisplayName,
                        item.AccountType,
                        item.OU,
                        item.Domain);
                }
            }

            this.exportTable = table;

            return this.exportTable;
        }

        private async Task<bool> LoadGroupMembersAsync()
        {
            try
            {
                IEnumerable<GroupMemberItemViewModel> groupMembers = await this.adBrowserService.GetMembersOfGroupAsync(this.Account, QueryType.SamAccountName);
                var count = 0;

                lock (this.syncObject)
                {
                    this.allGroupMembers.AddRange(groupMembers);

                    count = this.allGroupMembers.Count;
                    this.Pagination.TotalRows = count;
                }

                return count > 0;
            }
            catch (ActiveDirectoryServiceException ex)
            {
                this.log.LogError(ex, "Failed to fetch members of group {Account} from Active Directory due to unhandled Active Directory error.", this.Account);
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to fetch members of group from Active Directory.");
            }

            return false;
        }

        private async Task ChainTasksAsync(bool load = false)
        {
            await this.dispatcherService.InvokeAsync(this.StartOfChainTasks);

            if (load)
            {
                bool result = await this.LoadGroupMembersAsync();
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
                this.Header.SetText(GroupMemberResource.ListEmptyText, true);
            }
            else if (count == 1)
            {
                this.Header.SetText(GroupMemberResource.OneMemberCaption);
            }
            else
            {
                this.Header.SetText(string.Format(GroupMemberResource.TotalMembersCaption, count));
            }

            this.Header.EndProgress();
        }

        private async Task DoChangePageAsync(int pageNo)
        {
            int skip = this.Pagination.PageSize * (pageNo - 1);

            IEnumerable<GroupMemberItemViewModel> theList;

            lock (this.syncObject)
            {
                theList = this.allGroupMembers.Skip(skip).Take(this.Pagination.PageSize);
            }

            await this.dispatcherService.InvokeAsync(() =>
            {
                lock (this.syncObject)
                {
                    this.pagedGroupMembers.Clear();
                    foreach (GroupMemberItemViewModel item in theList)
                    {
                        this.pagedGroupMembers.Add(item);
                    }
                }
            });
        }

        private async Task ShowGroupMembersAsync(object _)
        {
            await this.flyOutService.ShowAsync<GroupMembersViewModel>(this.SelectedItem.AccountName);
        }

        private async Task<bool> CanShowGroupMembersAsync(object _)
        {
            return await Task.FromResult(this.SelectedItem != null && this.SelectedItem.IsGroup);
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
            this.exportService.ExportGroupMembers(new[] { this });
        }

        private PaginationViewModel CreatePagination()
        {
            async Task PageChangeAsyncCallback(PageChangeMode p)
            {
                await this.ChainTasksAsync();
            }

            return new PaginationViewModel(this.configurationManager, PageChangeAsyncCallback) { ShowText = GroupMemberResource.ShowMembersCaption };
        }
    }
}