// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Sch�fer, Matthias Friedrich, and Ritesh Gite
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
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdServices.EnumOU;
    using Core;
    using Events;
    using JetBrains.Annotations;
    using Prism.Events;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ComputerPrincipalViewModel : BasePrincipalViewModel
    {
        private readonly IAdBrowserService adBrowserService;

        private readonly IDialogService dialogService;
        private readonly IEventAggregator eventAggregator;
        private readonly AdTreeViewModel principal;
        private readonly object syncObject = new();

        private ICommand addServersCommand;

        private bool expanded;
        private bool loadTreeInitiated;

        public ComputerPrincipalViewModel(
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            IAdBrowserService adBrowserService,
            AdTreeViewModel principal,
            IPrincipalViewModel parent)
            : base(principal, parent)
        {
            if (parent is null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));
            this.principal = principal ?? throw new ArgumentNullException(nameof(principal));
        }

        public override bool Expanded
        {
            get => this.expanded;
            set => this.Set(ref this.expanded, value, nameof(this.Expanded));
        }

        public ICommand AddServersCommand => this.addServersCommand ??= new AsyncRelayCommand(this.AddServersAsync);

        protected override void OnPropertyChange(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Expanded) && this.principal.Type == TreeViewNodeType.OU && !this.loadTreeInitiated)
            {
                this.LoadTreeAsync().FireAndForgetSafeAsync();
            }

            base.OnPropertyChange(e);
        }

        public override async Task InitializeAsync()
        {
            await this.LoadTreeAsync();
        }

        private async Task AddServersAsync(object obj)
        {
            IEnumerable<string> computerNames = null;

            if (this.Type.Equals(TypeOU))
            {
                if (!this.loadTreeInitiated)
                {
                    this.expanded = true;
                    this.RaisePropertyChanged(nameof(this.Expanded));
                    await this.LoadTreeAsync();
                }

                lock (this.syncObject)
                {
                    computerNames = this.Items.Where(m => m.Type.Equals(TypeComputer)).Select(m => m.DisplayName);
                }
            }
            else if (this.Type.Equals(TypeComputer))
            {
                computerNames = new[] { this.DisplayName };
            }

            this.eventAggregator.GetEvent<AddServersEvent>().Publish(computerNames);
        }

        private async Task LoadTreeAsync()
        {
            this.loadTreeInitiated = true;
            this.DoProgress();

            try
            {
                IEnumerable<IPrincipalViewModel> items = await this.adBrowserService.GetComputerPrincipalsAsync(this.DistinguishedName, this);

                this.SetItems(items);

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
    }
}