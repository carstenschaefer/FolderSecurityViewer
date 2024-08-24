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
    using System.Windows;

    public class CustomWindow : Window
    {
        public static readonly DependencyProperty StartLocationProperty = DependencyProperty.Register(nameof(StartLocation), typeof(WindowStartupLocation), typeof(CustomWindow), new PropertyMetadata(WindowStartupLocation.Manual, OnStartLocationChanged));

        public CustomWindow()
        {
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;

            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
        }

        public WindowStartupLocation StartLocation
        {
            get => (WindowStartupLocation)this.GetValue(StartLocationProperty);
            set => this.SetValue(StartLocationProperty, value);
        }

        private static void OnStartLocationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is CustomWindow window)
            {
                window.WindowStartupLocation = window.StartLocation;
            }
        }
    }
}