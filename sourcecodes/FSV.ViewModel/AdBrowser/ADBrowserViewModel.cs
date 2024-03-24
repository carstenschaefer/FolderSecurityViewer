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

namespace FSV.ViewModel.AdBrowser
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdServices.Abstractions;
    using AdServices.EnumOU;
    using Core;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ADBrowserViewModel : WorkspaceViewModel
    {
        private readonly IAdBrowserService adBrowserService;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly ICurrentDomainCheckUtility domainCheckUtility;
        private readonly ModelBuilder<AdTreeViewModel, ADBrowserType, DomainViewModel> domainItemViewModelBuilder;
        private readonly ILogger<ADBrowserViewModel> logger;

        private readonly object syncObject = new();

        private ICommand _domainChangeCommand;

        private bool _domainsVisible;
        private string _principalName;
        private IAsyncCommand _searchPrincipalCommand;
        private string _selectedDomain;

        private IPrincipalViewModel _selectedPrincipal;
        private AdTreeViewModel _selectedUser;
        private ICommand _showDomainListCommand;
        private bool _showUserList;

        public ADBrowserViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IAdBrowserService adBrowserService,
            ModelBuilder<AdTreeViewModel, ADBrowserType, DomainViewModel> domainItemViewModelBuilder,
            ICurrentDomainCheckUtility domainCheckUtility,
            ILogger<ADBrowserViewModel> logger,
            ADBrowserType type)
        {
            this.domainItemViewModelBuilder = domainItemViewModelBuilder ?? throw new ArgumentNullException(nameof(domainItemViewModelBuilder));
            this.domainCheckUtility = domainCheckUtility ?? throw new ArgumentNullException(nameof(domainCheckUtility));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            this.DisplayName = SharedServersResource.ADBrowserCaption;
            this.Placeholder = type == ADBrowserType.Computers ? SharedServersResource.ADComputerPlaceholder : SharedServersResource.ADUserPlaceholder;

            this.AdBrowserType = type;
        }

        public string Placeholder { get; }

        public string PrincipalName
        {
            get => this._principalName;
            set
            {
                this._principalName = value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.CanReport = false;
                }

                this.RaisePropertyChanged(nameof(this.PrincipalName));
            }
        }

        public RangeObservableCollection<AdTreeViewModel> UserPrincipals { get; } = new();

        public bool ShowUserList
        {
            get => this._showUserList;
            set => this.Set(ref this._showUserList, value, nameof(this.ShowUserList));
        }

        public AdTreeViewModel SelectedUser
        {
            get => this._selectedUser;
            set
            {
                this.Set(ref this._selectedUser, value, nameof(this.SelectedUser));

                this.ShowUserList = false;
                this.CanReport = true;

                if (this._selectedUser is null)
                {
                    return;
                }

                this.PrincipalName = this._selectedUser.SamAccountName;
                this.InitiateSelectAsync(this._selectedUser).FireAndForgetSafeAsync();
            }
        }

        public string SelectedDomain
        {
            get => this._selectedDomain;
            set => this.Set(ref this._selectedDomain, value, nameof(this.SelectedDomain));
        }

        public IPrincipalViewModel SelectedPrincipal
        {
            get => this._selectedPrincipal;
            set => this.Set(ref this._selectedPrincipal, value, nameof(this.SelectedPrincipal));
        }

        public bool DomainsVisible
        {
            get => this._domainsVisible;
            set => this.Set(ref this._domainsVisible, value, nameof(this.DomainsVisible));
        }

        public ObservableCollection<IPrincipalViewModel> Principals { get; } = new();

        public IEnumerable<DomainViewModel> Domains { get; private set; }

        public ICommand DomainChangeCommand => this._domainChangeCommand ??= new AsyncRelayCommand(this.ChangeDomainAsync /*, (p) => !string.IsNullOrEmpty(this.SelectedDomain)*/);

        public ICommand ShowDomainListCommand => this._showDomainListCommand ??= new RelayCommand(this.ShowDomainList);

        public IAsyncCommand SearchPrincipalCommand =>
            this._searchPrincipalCommand ??=
                new AsyncRelayCommand(this.SearchPrincipalAsync, async p => await Task.FromResult((this.PrincipalName ?? string.Empty).Trim('*').Length > 0));

        public ADBrowserType AdBrowserType { get; }

        internal bool CanReport { get; private set; }

        private async Task ChangeDomainAsync(object p)
        {
            lock (this.syncObject)
            {
                this.Principals.Clear();
            }

            AdTreeViewModel adTreeViewModel = this.GetDomainAdTreeModel(this.SelectedDomain);

            DomainViewModel principalViewModel = this.domainItemViewModelBuilder.Build(adTreeViewModel, this.AdBrowserType);

            lock (this.syncObject)
            {
                this.Principals.Add(principalViewModel);
            }

            await principalViewModel.InitializeAsync();

            this.DomainsVisible = false;
        }

        private void ShowDomainList(object obj)
        {
            this.DomainsVisible = obj != null && bool.TryParse(obj.ToString(), out bool result) && result;
        }

        private async Task SearchPrincipalAsync(object _)
        {
            if (string.IsNullOrWhiteSpace(this._principalName))
            {
                return;
            }

            this.DoProgress();

            IEnumerable<AdTreeViewModel> users = (await this.adBrowserService.FindUsersAndGroupsAsync(this._principalName)).ToList();

            this.StopProgress();

            switch (users.Count())
            {
                case 0:
                    this.CanReport = false;
                    this.ShowUserList = false;
                    this.dialogService.ShowMessage(SharedServersResource.UserOrGroupNotFound);
                    break;
                case 1:
                    this.FillUsers(users);
                    await this.InitiateSelectAsync(users.First());
                    // The collection contains only one entry.

                    this.CanReport = true;
                    this.ShowUserList = false;
                    break;
                default:
                    // Collection contains more than one users. Make sure to display list so that end user can select it.
                    this.FillUsers(users);

                    this.CanReport = false;
                    this.ShowUserList = true;
                    break;
            }
        }

        public override async Task InitializeAsync()
        {
            try
            {
                if (this.domainCheckUtility.IsComputerJoinedAndConnectedToDomain())
                {
                    string domainName = this.domainCheckUtility.GetComputerDomainName() ?? string.Empty;

                    AdTreeViewModel adTreeViewModel = this.GetDomainAdTreeModel(domainName);

                    DomainViewModel principalViewModel = this.domainItemViewModelBuilder.Build(adTreeViewModel, this.AdBrowserType);

                    lock (this.syncObject)
                    {
                        this.Principals.Add(principalViewModel);
                    }

                    await principalViewModel.InitializeAsync();

                    await this.FillDomainsAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to initialize model.");
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(ex.Message));
            }
        }

        private async Task FillDomainsAsync()
        {
            try
            {
                IEnumerable<string> domains = await Task.Run(this.domainCheckUtility.GetAllDomainsOfForest);
                this.Domains = domains.Select(this.GetDomainItem).ToList();
                this.RaisePropertyChanged(nameof(this.Domains));
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to obtain domains from forest due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(ex.Message));
            }
        }

        private DomainViewModel GetDomainItem([NotNull] string domainName)
        {
            AdTreeViewModel adTree = this.GetDomainAdTreeModel(domainName);

            return this.domainItemViewModelBuilder.Build(adTree, this.AdBrowserType);
        }

        private AdTreeViewModel GetDomainAdTreeModel([NotNull] string domainName)
        {
            string distinguishedName = GetDomainDistinguishedName(domainName);

            return new AdTreeViewModel(domainName, distinguishedName, domainName, TreeViewNodeType.Domain);
        }

        private async Task InitiateSelectAsync(AdTreeViewModel adTreeItem)
        {
            this.DoProgress();
            this.PrincipalName = adTreeItem.SamAccountName;

            if (!this.Principals.Any())
            {
                return;
            }

            try
            {
                await this.MakeSelectionAsync(this.Principals, adTreeItem);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to select principal {principal} in active directory browser.", adTreeItem.SamAccountName);
                this.dialogService.ShowMessage(e.Message);
            }

            this.StopProgress();
        }

        private async Task MakeSelectionAsync(IList<IPrincipalViewModel> principals, AdTreeViewModel item)
        {
            BasePrincipalViewModel foundPrincipal;

            lock (this.syncObject)
            {
                foundPrincipal = principals.FirstOrDefault(m => item.DistinguishedName.Equals(m.DistinguishedName, StringComparison.InvariantCultureIgnoreCase)) as BasePrincipalViewModel;
            }

            if (foundPrincipal is not null)
            {
                foundPrincipal.Selected = true;
                return;
            }

            lock (this.syncObject)
            {
                foundPrincipal = principals.FirstOrDefault(m => item.DistinguishedName.EndsWith(m.DistinguishedName, StringComparison.InvariantCultureIgnoreCase)) as BasePrincipalViewModel;
            }

            if (foundPrincipal is null)
            {
                return;
            }

            if (!foundPrincipal.ItemsLoaded)
            {
                await foundPrincipal.InitializeAsync();
            }

            foundPrincipal.Expanded = true;

            await this.MakeSelectionAsync(foundPrincipal.Items.ToList(), item);
        }

        private void FillUsers(IEnumerable<AdTreeViewModel> users)
        {
            lock (this.syncObject)
            {
                this.UserPrincipals.Clear();
                this.UserPrincipals.AddRange(users);
            }
        }

        private static string GetDomainDistinguishedName(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(domainName));
            }

            IEnumerable<string> domainParts = domainName
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(domainPart => $"DC={domainPart}");

            return string.Join(",", domainParts);
        }
    }
}