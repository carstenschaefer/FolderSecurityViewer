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
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class PathSelector : UserControl
    {
        public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(nameof(ButtonCommand), typeof(ICommand), typeof(PathSelector));
        public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(nameof(TextStyle), typeof(Style), typeof(PathSelector));
        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(PathSelector));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<PathSelectorItem>), typeof(PathSelector));

        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
            nameof(Path),
            typeof(string),
            typeof(PathSelector),
            new FrameworkPropertyMetadata(
                string.Empty,
                OnPathPropertyChanged));

        public PathSelector()
        {
            this.InitializeComponent();
        }

        public ICommand ButtonCommand
        {
            get => this.GetValue(ButtonCommandProperty) as ICommand;
            set => this.SetValue(ButtonCommandProperty, value);
        }

        public string Path
        {
            get => this.GetValue(PathProperty) as string;
            set => this.SetValue(PathProperty, value);
        }

        public string DisplayText
        {
            get => this.GetValue(DisplayTextProperty) as string;
            set => this.SetValue(DisplayTextProperty, value);
        }

        public Style TextStyle
        {
            get => this.GetValue(TextStyleProperty) as Style;
            set => this.SetValue(TextStyleProperty, value);
        }

        public IEnumerable<PathSelectorItem> ItemsSource
        {
            get => this.GetValue(ItemsSourceProperty) as IEnumerable<PathSelectorItem>;
            private set => this.SetValue(ItemsSourceProperty, value);
        }

        private static void OnPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PathSelector pathSelector)
            {
                return;
            }

            if (e.NewValue is string value && !string.IsNullOrWhiteSpace(value))
            {
                pathSelector.ItemsSource = PathToEnumerableConverter.Convert(value);
            }
            else
            {
                pathSelector.ItemsSource = Enumerable.Empty<PathSelectorItem>();
            }
        }

        private void OnCopyButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetDataObject(this.Path, true);
                Clipboard.Flush();
            }
            catch (COMException)
            {
                // Do nothing. This is a bug in WPF.
                // https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
            }
        }
    }
}