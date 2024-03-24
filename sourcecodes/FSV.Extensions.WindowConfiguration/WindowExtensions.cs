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

namespace FSV.Extensions.WindowConfiguration
{
    using System;
    using System.Windows;
    using Abstractions;
    using FsvWindowState = Abstractions.WindowState;
    using WindowState = System.Windows.WindowState;

    public static class WindowExtensions
    {
        public static void SetBoundsFromPosition(this Window window, Position position)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (position.State is null)
            {
                window.WindowState = WindowState.Maximized;
                return;
            }

            window.Top = position.Top <= 0 ? 0 : position.Top;
            window.Left = position.Left <= 0 ? 0 : position.Left;
            window.Width = position.Width;
            window.Height = position.Height;
            window.WindowState = (WindowState)position.State;
        }

        public static Position AsPosition(this Window window)
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            Rect rect = window.RestoreBounds;

            int width = !double.IsInfinity(rect.Width) ? (int)rect.Width : 0;
            int height = !double.IsInfinity(rect.Height) ? (int)rect.Height : 0;
            int top = !double.IsInfinity(rect.Top) ? (int)rect.Top : 0;
            int left = !double.IsInfinity(rect.Left) ? (int)rect.Left : 0;
            var state = (FsvWindowState)window.WindowState;

            return new Position(width, height, left, top, state);
        }
    }
}