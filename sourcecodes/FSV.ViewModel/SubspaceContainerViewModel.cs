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

namespace FSV.ViewModel
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using Core;

    public class SubspaceContainerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<SubspaceItemBase> items;

        private RelayCommand closeSubspaceCommand;
        private bool maximizeSubspace;
        private RelayCommand resizeSubspaceCommand;

        private SubspaceItemBase selectedItem;

        public SubspaceContainerViewModel()
        {
            this.items = new ObservableCollection<SubspaceItemBase>();
        }

        public IEnumerable<SubspaceItemBase> Items => this.items;

        public SubspaceItemBase SelectedItem
        {
            get => this.selectedItem;
            set => this.DoSet(ref this.selectedItem, value, nameof(this.SelectedItem));
        }

        public bool MaximizeSubspace
        {
            get => this.maximizeSubspace;
            private set => this.Set(ref this.maximizeSubspace, value, nameof(this.MaximizeSubspace));
        }

        public ICommand CloseItemCommand => this.closeSubspaceCommand ??= new RelayCommand(this.CloseSubspace);

        public ICommand ResizeCommand => this.resizeSubspaceCommand ??= new RelayCommand(this.ResizeSubspace);

        public void AddItem(SubspaceItemBase item)
        {
            this.items.Add(item);
        }

        public void ClearItems()
        {
            this.items.Clear();
        }

        private void CloseSubspace(object p)
        {
            this.SelectedItem = null;
            this.MaximizeSubspace = false;
        }

        private void ResizeSubspace(object p)
        {
            this.MaximizeSubspace = !this.MaximizeSubspace;
        }
    }
}