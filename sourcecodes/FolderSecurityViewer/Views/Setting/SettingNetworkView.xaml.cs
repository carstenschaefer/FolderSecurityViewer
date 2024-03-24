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

namespace FolderSecurityViewer.Views.Setting
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using FSV.ViewModel.Setting;

    /// <summary>
    ///     Interaction logic for NetworkView.xaml
    /// </summary>
    public partial class SettingNetworkView : UserControl
    {
        private NetworkViewModel _viewModel;

        public SettingNetworkView()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.NetworkView_DataContextChanged;

            this.ProxyPassword.LostFocus += this.ProxyPasswordLostFocus;
            this.ProxyPassword.LostKeyboardFocus += this.ProxyPasswordLostKeyboardFocus;

            this.Unloaded += this.SettingNetworkViewUnloaded;
        }

        private void SettingNetworkViewUnloaded(object sender, RoutedEventArgs e)
        {
            this.ProxyPassword.LostFocus -= this.ProxyPasswordLostFocus;
            this.ProxyPassword.LostKeyboardFocus -= this.ProxyPasswordLostKeyboardFocus;
            this.Unloaded -= this.SettingNetworkViewUnloaded;
        }

        private void ProxyPasswordLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.PasswordChanged();
        }

        private void ProxyPasswordLostFocus(object sender, RoutedEventArgs e)
        {
            this.PasswordChanged();
        }

        private void PasswordChanged()
        {
            if (this._viewModel == null)
            {
                return;
            }

            this._viewModel.ProxyPassword = this.ProxyPassword.Password;
        }

        private void NetworkView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null && this._viewModel != null)
            {
                this._viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
            }

            this._viewModel = e.NewValue as NetworkViewModel;
            if (this._viewModel == null)
            {
                return;
            }

            this.ProxyPassword.Password = this._viewModel.ProxyPassword;

            this._viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs ev)
        {
            if (ev.PropertyName.Equals(nameof(this.ProxyPassword)))
            {
                this.ProxyPassword.Password = this._viewModel.ProxyPassword;
            }
        }
    }
}