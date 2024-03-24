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
    using System.Linq;
    using Abstractions;
    using AdServices.EnumOU;
    using JetBrains.Annotations;

    public abstract class BasePrincipalViewModel : WorkspaceViewModel, IPrincipalViewModel
    {
        public static readonly string TypeDomain = TreeViewNodeType.Domain.ToString();
        public static readonly string TypeOU = TreeViewNodeType.OU.ToString();
        public static readonly string TypeComputer = TreeViewNodeType.Computer.ToString();
        public static readonly string TypeGroup = TreeViewNodeType.Group.ToString();
        public static readonly string TypeUser = TreeViewNodeType.User.ToString();
        private readonly object syncObject = new();

        private bool _itemsLoaded;

        private bool _selected;

        protected BasePrincipalViewModel(AdTreeViewModel principal, IPrincipalViewModel parent)
        {
            if (principal is null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            this.DisplayName = principal.DisplayName;
            this.Type = principal.Type.ToString();
            this.SamAccountName = principal.SamAccountName;
            this.DistinguishedName = principal.DistinguishedName;
            this.Parent = parent;
        }

        public bool ItemsLoaded
        {
            get => this._itemsLoaded;
            protected set => this.Set(ref this._itemsLoaded, value, nameof(this.ItemsLoaded));
        }

        public IPrincipalViewModel Parent { get; }

        public abstract bool Expanded { get; set; }

        public IEnumerable<IPrincipalViewModel> Items { get; private set; } = new ReadOnlyCollection<IPrincipalViewModel>(new List<IPrincipalViewModel>());

        public string SamAccountName { get; }

        public string DistinguishedName { get; }

        public virtual string AccountName => this.SamAccountName;

        public string Type { get; protected set; }

        public bool Selected
        {
            get => this._selected;
            set => this.Set(ref this._selected, value, nameof(this.Selected));
        }

        protected void SetItems([NotNull] IEnumerable<IPrincipalViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            lock (this.syncObject)
            {
                this.Items = new ReadOnlyCollection<IPrincipalViewModel>(items.ToList());
                this.RaisePropertyChanged(nameof(this.Items));
            }
        }
    }
}