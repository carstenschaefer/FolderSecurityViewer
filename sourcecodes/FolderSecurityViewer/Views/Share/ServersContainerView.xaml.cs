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

namespace FolderSecurityViewer.Views.Share
{
    using System.Windows;
    using System.Windows.Controls;
    using Extensions;
    using FSV.ViewModel.ShareReport;

    public partial class ServersContainerView : UserControl
    {
        private ServersContainerViewModel _dataContext;

        public ServersContainerView()
        {
            this.InitializeComponent();

            this.DataContextChanged += (s, e) => this._dataContext = this.DataContext as ServersContainerViewModel;
        }

        private void ServersTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var item = (e.OriginalSource as DependencyObject).VisualUpwardSearch<TreeViewItem>();
            if (item != null)
            {
                item.IsSelected = true;
            }
            //if(!(item.DataContext is SharedServerViewModel))
            //{
            //    e.Handled = true;
            //}
        }

        private void ServersTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this._dataContext == null)
            {
                return;
            }

            if (e.NewValue is SharedServerViewModel sharedServer)
            {
                this._dataContext.SelectedServer = sharedServer;
            }
            else if (e.NewValue is ShareViewModel share)
            {
                this._dataContext.SelectedShare = share;
            }
        }
    }
}