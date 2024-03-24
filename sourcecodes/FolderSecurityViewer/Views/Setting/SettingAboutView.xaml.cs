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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Navigation;

    /// <summary>
    ///     Interaction logic for AboutView.xaml
    /// </summary>
    public partial class SettingAboutView : UserControl
    {
        public SettingAboutView()
        {
            this.InitializeComponent();

            this.Loaded += this.AboutView_Loaded;
        }

        private void AboutView_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<Hyperlink> links = GetVisuals(this.viewer.Document).OfType<Hyperlink>();
            foreach (Hyperlink link in links)
            {
                link.RequestNavigate += this.Link_RequestNavigate;
            }
        }

        private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public static IEnumerable<DependencyObject> GetVisuals(DependencyObject root)
        {
            foreach (DependencyObject child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (DependencyObject descendants in GetVisuals(child))
                {
                    yield return descendants;
                }
            }
        }
    }
}