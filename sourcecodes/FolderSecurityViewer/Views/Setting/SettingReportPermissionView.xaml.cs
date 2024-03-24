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
    using System.Windows.Input;
    using FSV.ViewModel.Setting;

    /// <summary>
    ///     Interaction logic for SettingReportPermissionView.xaml
    /// </summary>
    public partial class SettingReportPermissionView : UserControl
    {
        private ReportPermissionViewModel _viewModel;

        public SettingReportPermissionView()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.PermissionView_DataContextChanged;
        }

        private void PermissionView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == null)
            {
                this._viewModel = this.DataContext as ReportPermissionViewModel;
            }
        }

        private void ScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}