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
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    public class IconButton : Button
    {
        public static readonly DependencyProperty IsProgressProperty = DependencyProperty.Register("IsProgress", typeof(bool), typeof(IconButton), new UIPropertyMetadata(false, OnIsProgressChanged));
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(IconButton));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(IconButton));
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register("IconSize", typeof(IconButtonSize), typeof(IconButton), new FrameworkPropertyMetadata(IconButtonSize.Medium));
        public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu", typeof(ContextMenu), typeof(IconButton), new UIPropertyMetadata(null, OnMenuChanged));

        public IconButton()
        {
            this.Loaded += this.OnLoaded;
        }

        public ContextMenu Menu
        {
            get => (ContextMenu)this.GetValue(MenuProperty);
            set => this.SetValue(MenuProperty, value);
        }

        public Geometry Icon
        {
            get => this.GetValue(IconProperty) as Geometry;
            set => this.SetValue(IconProperty, value);
        }

        public string Text
        {
            get => this.GetValue(TextProperty) as string;
            set => this.SetValue(TextProperty, value);
        }

        public IconButtonSize IconSize
        {
            get => (IconButtonSize)this.GetValue(IconSizeProperty);
            set => this.SetValue(IconSizeProperty, value);
        }

        public bool IsProgress
        {
            get => (bool)this.GetValue(IsProgressProperty);
            set => this.SetValue(IsProgressProperty, value);
        }

        private ICommand PlaceholderCommand { get; set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Command == null && this.Menu != null)
            {
                this.Click += this.Clicking;
            }
        }

        private void Clicking(object sender, RoutedEventArgs e)
        {
            if (this.Menu != null)
            {
                this.Menu.IsOpen = true;
            }
        }

        private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IconButton iconButton && e.NewValue is ContextMenu contextMenu)
            {
                contextMenu.DataContext = iconButton.DataContext;
                contextMenu.PlacementTarget = iconButton;
                contextMenu.Placement = PlacementMode.Bottom;
            }
        }

        private static void OnIsProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IconButton iconButton)
            {
                var newValue = (bool)e.NewValue;

                if (newValue)
                {
                    iconButton.PlaceholderCommand = iconButton.Command;
                    iconButton.Command = null;
                }
                else
                {
                    iconButton.Command = iconButton.PlaceholderCommand;
                    iconButton.PlaceholderCommand = null;
                }
            }
        }
    }
}