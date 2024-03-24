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
    ///     Interaction logic for SettingView.xaml
    /// </summary>
    public partial class SettingView : UserControl
    {
        public SettingView()
        {
            this.InitializeComponent();

            this.Width = SystemParameters.PrimaryScreenWidth / 1.3;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.3;
        }

        private void SettingViewModelChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is SettingViewModel _viewModel)
            {
                _viewModel.PropertyChanged += (s1, e1) =>
                {
                    if (e1.PropertyName.Equals(nameof(_viewModel.CurrentWorkspace)))
                    {
                        this.ButtonSave.Visibility = _viewModel.CurrentWorkspace.UsesSave ? Visibility.Visible : Visibility.Collapsed;
                        this.ButtonCancel.Visibility = _viewModel.CurrentWorkspace.UsesClose ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
            }
        }
    }
}