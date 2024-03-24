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

namespace FolderSecurityViewer.Views.Home
{
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Extensions;
    using FSV.ViewModel.Home;

    public partial class FolderTreeView
    {
        private readonly Timer textInputWaitTimer;

        private FolderTreeViewModel dataContext;
        private string typingText = string.Empty;

        public FolderTreeView()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.OnDataContextChanged;
            this.Unloaded += this.OnUnload;

            this.textInputWaitTimer = new Timer(this.OnFolderTreeTextUpdateTimeElapsed);
        }


        private void OnUnload(object sender, RoutedEventArgs e)
        {
            this.DataContextChanged -= this.OnDataContextChanged;
            this.Unloaded -= this.OnUnload;

            this.textInputWaitTimer.Dispose();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                this.dataContext = this.DataContext as FolderTreeViewModel;
            }
        }

        private void FolderTreeExpanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not TreeViewItem treeViewItem)
            {
                return;
            }

            if (!this.dataContext.Refreshing)
            {
                treeViewItem.SetValue(TreeViewItem.IsSelectedProperty, true);
            }

            if (treeViewItem.IsExpanded)
            {
                this.dataContext.Expand();
            }
            else
            {
                this.dataContext.Collapse();
            }
        }

        private void FolderTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not FolderTreeItemViewModel vm)
            {
                return;
            }

            this.dataContext.SelectedFolder = vm;

            if (e.OriginalSource is not TreeView tv)
            {
                return;
            }

            DependencyObject testItem = tv.ItemContainerGenerator.ContainerFromItem(vm);

            if (tv.ItemContainerGenerator.ContainerFromItem(vm) is TreeViewItem treeViewItem)
            {
                treeViewItem.BringIntoView();
            }
        }

        private void FolderTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = (e.OriginalSource as DependencyObject).VisualUpwardSearch<TreeViewItem>();
            if (treeViewItem is not null)
            {
                treeViewItem.IsSelected = true;
            }
        }

        private void FolderTreeTextInput(object _, TextCompositionEventArgs e)
        {
            this.typingText += e.Text;
            this.textInputWaitTimer.Change(300, Timeout.Infinite);
        }

        private async void OnFolderTreeTextUpdateTimeElapsed(object _)
        {
            if (string.IsNullOrWhiteSpace(this.typingText))
            {
                return;
            }

            // At this point, TextChanged timer is stopped. Search the text in view model.
            await this.dataContext.SelectFolderNextAsync(this.typingText);

            this.typingText = string.Empty;
        }
    }
}