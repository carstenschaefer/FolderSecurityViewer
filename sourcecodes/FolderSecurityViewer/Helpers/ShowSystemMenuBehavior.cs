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
    using System.Windows.Media;

    public static class ShowSystemMenuBehavior
    {
        public static readonly DependencyProperty TargetWindowProperty = DependencyProperty.RegisterAttached("TargetWindow", typeof(Window), typeof(ShowSystemMenuBehavior), new UIPropertyMetadata(OnTargetWindowChanged));

        private static void HandleMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Window targetWindow)
            {
                return;
            }

            Point systemMenuPosition = GetAdjustedWindowPosition(targetWindow);

            SystemCommands.ShowSystemMenu(targetWindow, systemMenuPosition);
        }

        private static Point GetAdjustedWindowPosition(Window targetWindow)
        {
            Point position = Mouse.GetPosition(targetWindow);
            Point systemMenuPosition = targetWindow.PointToScreen(position);

            PresentationSource source = PresentationSource.FromVisual(targetWindow);
            if (source?.CompositionTarget == null)
            {
                return systemMenuPosition;
            }

            Matrix matrix = source.CompositionTarget.TransformToDevice;
            systemMenuPosition = new Point(systemMenuPosition.X / matrix.M11, systemMenuPosition.Y / matrix.M22);

            return systemMenuPosition;
        }

        public static Window GetTargetWindow(DependencyObject obj)
        {
            return (Window)obj.GetValue(TargetWindowProperty);
        }

        public static void SetTargetWindow(DependencyObject obj, Window window)
        {
            obj.SetValue(TargetWindowProperty, window);
        }

        private static void OnTargetWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is Window oldTarget)
            {
                oldTarget.MouseRightButtonDown -= HandleMouseRightButtonDown;
            }

            if (e.NewValue is Window newTarget)
            {
                newTarget.MouseRightButtonDown += HandleMouseRightButtonDown;
            }
        }
    }
}