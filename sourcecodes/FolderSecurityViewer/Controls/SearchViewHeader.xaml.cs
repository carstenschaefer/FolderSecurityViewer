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

namespace FolderSecurityViewer.Controls
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;

    /// <summary>
    ///     Interaction logic for ViewHeader.xaml
    /// </summary>
    public partial class SearchViewHeader : UserControl
    {
        public static readonly DependencyProperty RightControlBoxProperty = DependencyProperty.Register("RightControlBox", typeof(object), typeof(SearchViewHeader));
        public static readonly DependencyProperty LeftControlBoxProperty = DependencyProperty.Register("LeftControlBox", typeof(object), typeof(SearchViewHeader));

        public SearchViewHeader()
        {
            this.InitializeComponent();
        }

        public object RightControlBox
        {
            get => this.GetValue(RightControlBoxProperty);
            set => this.SetValue(RightControlBoxProperty, value);
        }

        public object LeftControlBox
        {
            get => this.GetValue(LeftControlBoxProperty);
            set => this.SetValue(LeftControlBoxProperty, value);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }
    }
}