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

namespace FolderSecurityViewer.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Extensions;
    using FSV.ViewModel.Abstractions;
    using FSV.ViewModel.AdBrowser;

    /// <summary>
    ///     Interaction logic for ADBrowserView.xaml
    /// </summary>
    public partial class ADBrowserView : UserControl
    {
        private ADBrowserViewModel _dataContext;

        public ADBrowserView()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.HandleViewDataContextChanged;
            this.Loaded += this.HandleViewLoaded;
            this.Unloaded += this.HandleViewUnload;
        }

        private void HandleViewUnload(object sender, RoutedEventArgs e)
        {
            this.DataContextChanged -= this.HandleViewDataContextChanged;
            this.Loaded -= this.HandleViewLoaded;
        }

        private void HandleViewLoaded(object sender, RoutedEventArgs e)
        {
            var window = this.VisualUpwardSearch<DialogWindow>();
            if (window == null)
            {
                return;
            }

            this.Width = SystemParameters.PrimaryScreenWidth / 1.7;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.7;
            // MainGrid.Margin = new Thickness(20, 10, 20, 20);
        }

        private void HandleViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._dataContext = this.DataContext as ADBrowserViewModel;
        }

        private void HandlePrincipalsTreeContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var item = (e.OriginalSource as DependencyObject).VisualUpwardSearch<TreeViewItem>();
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

        private void HandlePrincipalsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this._dataContext is null)
            {
                return;
            }

            this._dataContext.SelectedPrincipal = e.NewValue as IPrincipalViewModel;

            if (e.OriginalSource is not TreeView adTree || this._dataContext.SelectedPrincipal is null)
            {
                return;
            }

            ItemsControl testItem = adTree.ItemContainerGenerator.ContainerFromItem(this._dataContext.SelectedPrincipal, p => p.Parent);

            if (testItem is TreeViewItem item)
            {
                item.BringIntoView();
            }
        }

        private void HandleDomainListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this._dataContext?.DomainChangeCommand.Execute(null);
        }

        private void HandleUserListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.PrincipalName.Focus();
            this.PrincipalName.CaretIndex = this.PrincipalName.Text.Length;
        }
    }
}