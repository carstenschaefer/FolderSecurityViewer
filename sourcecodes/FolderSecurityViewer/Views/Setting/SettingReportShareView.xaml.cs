﻿// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
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
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using FSV.ViewModel.Setting;

    /// <summary>
    ///     Interaction logic for SetttingReportShareView.xaml
    /// </summary>
    public partial class SettingReportShareView : UserControl
    {
        private bool _passwordChanged;
        private ReportShareViewModel _viewModel;

        public SettingReportShareView()
        {
            this.InitializeComponent();

            this.Loaded += this.ViewLoaded;
            this.Unloaded += this.ViewUnloaded;
        }

        private void ViewUnloaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.ViewLoaded;
            this.Unloaded -= this.ViewUnloaded;

            this._viewModel?.SetPassword(null);
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = this.DataContext as ReportShareViewModel;

            if (this._viewModel?.IsPasswordSet() ?? false)
            {
                this.SharePasswordBox.SetPasswordForReveal(this._viewModel.GetPassword());
            }
        }

        private void PasswordLostFocus(object sender, RoutedEventArgs e)
        {
            this.PasswordChanged();
        }

        private void PasswordLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.PasswordChanged();
        }

        private void PasswordChanged()
        {
            if (this._viewModel != null && this._passwordChanged)
            {
                this._viewModel.SetPassword(this.SharePasswordBox.Password);
                this._passwordChanged = false;
            }
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            this._passwordChanged = true;
        }
    }
}