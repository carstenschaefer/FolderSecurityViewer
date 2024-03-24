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
    using System.Windows;
    using System.Windows.Controls;
    using FSV.ViewModel.Setting;

    /// <summary>
    ///     Interaction logic for SettingDatabaseSqlServer.xaml
    /// </summary>
    public partial class SettingDatabaseSqlServer : UserControl
    {
        private bool _passwordChanged;
        private DatabaseSqlServerViewModel _viewModel;

        public SettingDatabaseSqlServer()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.DatabaseView_DataContextChanged;
            this.SqlServerPassword.LostFocus += (s, e1) => this.PasswordChanged();
            this.SqlServerPassword.LostKeyboardFocus += (s, e1) => this.PasswordChanged();
        }

        private void DatabaseView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._viewModel = this.DataContext as DatabaseSqlServerViewModel;
            if (this._viewModel == null)
            {
                return;
            }

            if (this._viewModel.Password != null && this._viewModel.Password.Length > 0)
            {
                this.SqlServerPassword.Password = "TempPassword";
            }

            this.SqlServerPassword.PasswordChanged += (_, d) => this._passwordChanged = true;
        }

        private void PasswordChanged()
        {
            if (this._viewModel != null && this._passwordChanged)
            {
                this._viewModel.Password = this.SqlServerPassword.SecurePassword;
                this._passwordChanged = false;
            }
        }
    }
}