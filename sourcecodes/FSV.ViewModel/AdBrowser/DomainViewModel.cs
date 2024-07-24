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
    using Abstractions;
    using AdServices.EnumOU;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Logging;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class DomainViewModel : BasePrincipalViewModel
    {
        private readonly IAdBrowserService adBrowserService;
        private readonly ADBrowserType adBrowserType;
        private readonly IDialogService dialogService;
        private readonly ILogger<DomainViewModel> logger;

        private bool expanded;

        public DomainViewModel(
            ILogger<DomainViewModel> logger,
            IDialogService dialogService,
            IAdBrowserService adBrowserService,
            AdTreeViewModel adTreeItem,
            ADBrowserType adBrowserType) : base(adTreeItem, null)
        {
            if (!Enum.IsDefined(typeof(ADBrowserType), adBrowserType))
            {
                throw new InvalidEnumArgumentException(nameof(adBrowserType), (int)adBrowserType, typeof(ADBrowserType));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.adBrowserService = adBrowserService ?? throw new ArgumentNullException(nameof(adBrowserService));

            this.adBrowserType = adBrowserType;
        }

        public override bool Expanded
        {
            get => this.expanded;
            set => this.Set(ref this.expanded, value, nameof(this.Expanded));
        }

        public override async Task InitializeAsync()
        {
            try
            {
                string domain = this.GetOuName();

                IEnumerable<IPrincipalViewModel> items = this.adBrowserType == ADBrowserType.Principals
                    ? await this.adBrowserService.GetPrincipalsAsync(domain, this)
                    : await this.adBrowserService.GetComputerPrincipalsAsync(domain, this);

                this.SetItems(items);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to load principals of domain {domain}", this.DisplayName);
                this.dialogService.ShowMessage($"Failed to load principals of domain {this.DisplayName}. Please check log for more details.");
            }
        }

        private string GetOuName()
        {
            string text = string.IsNullOrEmpty(this.DistinguishedName) ? this.DisplayName : this.DistinguishedName;
            if (text.EndsWith(".local"))
            {
                text = text.Substring(0, text.LastIndexOf(".", StringComparison.Ordinal));
            }

            return text;
        }
    }
}