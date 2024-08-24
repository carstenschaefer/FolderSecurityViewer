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

namespace FolderSecurityViewer.Helpers
{
    using System.Windows;
    using System.Windows.Input;

    public static class WindowDragBehavior
    {
        public static readonly DependencyProperty LeftMouseButtonDragProperty = DependencyProperty.RegisterAttached("LeftMouseButtonDrag",
            typeof(Window), typeof(WindowDragBehavior),
            new UIPropertyMetadata(null, LeftMouseButtonDragPropertyChangedCallback));

        public static Window GetLeftMouseButtonDrag(DependencyObject obj)
        {
            return (Window)obj.GetValue(LeftMouseButtonDragProperty);
        }

        public static void SetLeftMouseButtonDrag(DependencyObject obj, Window window)
        {
            obj.SetValue(LeftMouseButtonDragProperty, window);
        }

        private static void LeftMouseButtonDragPropertyChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                element.MouseLeftButtonDown += ButtonDown;
            }
        }

        private static void ButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element && element.GetValue(LeftMouseButtonDragProperty) is Window targetWindow)
            {
                targetWindow.DragMove();
            }
        }
    }
}