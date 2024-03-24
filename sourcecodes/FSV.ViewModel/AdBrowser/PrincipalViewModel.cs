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
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdMembers;
    using AdServices.Abstractions;
    using AdServices.EnumOU;
    using Core;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class PrincipalViewModel : BasePrincipalViewModel
    {
        private readonly IAdBrowserService adBrowserService;
        private readonly IDialogService dialogService;
        private readonly IDomainInformationService domainInformationService;
        private readonly IFlyOutService flyOutService;
        private readonly AdTreeViewModel principal;

        private bool expanded;
        private bool loadTreeInitiated;
        private string rootDomainNetBiosName;

        private ICommand showGroupMembersCommand;
        private ICommand showMembershipsCommand;

        public PrincipalViewModel(
            IDialogService dialogService,
            IFlyOutService flyOutService,
            IAdBrowserService adBrowserService,
            IDomainInformationService domainInformationService,
            AdTreeViewModel principal,
            IPrincipalViewModel parent)
            : base(principal, parent)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.principal = principal ?? throw new ArgumentNullException(nameof(principal));
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));
            this.domainInformationService = domainInformationService ?? throw new ArgumentNullException(nameof(domainInformationService));
        }

        public override bool Expanded
        {
            get => this.expanded;
            set => this.Set(ref this.expanded, value, nameof(this.Expanded));
        }

        public override string AccountName
        {
            get
            {
                this.rootDomainNetBiosName = this.rootDomainNetBiosName ??= this.domainInformationService.GetRootDomainNetBiosName();
                return this.principal.Type == TreeViewNodeType.User ? string.Concat(this.rootDomainNetBiosName, '\\', this.SamAccountName) : this.SamAccountName;
            }
        }

        public ICommand ShowGroupMembersCommand => this.showGroupMembersCommand ??= new RelayCommand(this.ShowGroupMembers, p => this.Type == TreeViewNodeType.Group.ToString());
        public ICommand ShowMembershipsCommand => this.showMembershipsCommand ??= new AsyncRelayCommand(this.ShowMembershipsAsync, this.CanShowMembershipsAsync);

        public override async Task InitializeAsync()
        {
            await this.LoadTreeAsync();
        }

        protected override void OnPropertyChange(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Expanded) && this.principal.Type == TreeViewNodeType.OU && !this.loadTreeInitiated)
            {
                this.LoadTreeAsync().FireAndForgetSafeAsync();
            }

            base.OnPropertyChange(e);
        }

        private async Task LoadTreeAsync()
        {
            this.loadTreeInitiated = true;
            this.DoProgress();

            try
            {
                IEnumerable<IPrincipalViewModel> principals = await this.adBrowserService.GetPrincipalsAsync(this.DistinguishedName, this);

                this.SetItems(principals);

                this.ItemsLoaded = true;
            }
            catch (Exception ex)
            {
                this.dialogService.ShowMessage(ex.ToString());
            }
            finally
            {
                this.StopProgress();
            }
        }

        private void ShowGroupMembers(object p)
        {
            this.flyOutService.Show<GroupMembersViewModel>(this.SamAccountName);
        }

        private async Task ShowMembershipsAsync(object _)
        {
            await this.flyOutService.ShowAsync<PrincipalMembershipViewModel>(this.SamAccountName);
        }

        private async Task<bool> CanShowMembershipsAsync(object _)
        {
            return await Task.FromResult(this.Type == TreeViewNodeType.Group.ToString() || this.Type == TreeViewNodeType.User.ToString());
        }
    }
}