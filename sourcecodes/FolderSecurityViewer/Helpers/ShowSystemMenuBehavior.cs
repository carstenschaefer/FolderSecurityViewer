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
#pragma warning disable 618
    using System.Windows;
    using System.Windows.Input;
    using SystemCommands = ControlzEx.Windows.Shell.SystemCommands;

#pragma warning restore 618

    public static class ShowSystemMenuBehavior
    {
        #region RightButtonShow handlers

        private static void RightButtonDownShow(object sender, MouseButtonEventArgs e)
        {
            Window targetWindow = GetTargetWindow((UIElement)sender);

            // var showMenuAt = targetWindow.PointToScreen(Mouse.GetPosition((targetWindow)));

            // SystemMenuManager.ShowMenu(targetWindow, showMenuAt);
#pragma warning disable CS0618 // Type or member is obsolete
            SystemCommands.ShowSystemMenu(targetWindow, e);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        #endregion

        #region TargetWindow

        public static Window GetTargetWindow(DependencyObject obj)
        {
            return (Window)obj.GetValue(TargetWindow);
        }

        public static void SetTargetWindow(DependencyObject obj, Window window)
        {
            obj.SetValue(TargetWindow, window);
        }

        public static readonly DependencyProperty TargetWindow = DependencyProperty.RegisterAttached("TargetWindow", typeof(Window), typeof(ShowSystemMenuBehavior), new UIPropertyMetadata(OnTargetWindowChanged));

        private static void OnTargetWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.MouseRightButtonDown += RightButtonDownShow;
            }
        }

        #endregion
    }
}