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

namespace FSV.Extensions.WindowConfiguration.Test
{
    using Abstractions;

    using System.Windows;

    using Xunit;

    using WindowState = System.Windows.WindowState;

    /// <summary>
    ///     See https://github.com/AArnott/Xunit.StaFact
    /// </summary>
    public class WindowExtensionsTest
    {
        [WpfFact]
        public void WindowExtensions_AsPosition_Test()
        {
            // Arrange
            const int width = 640;
            const int height = 480;
            const int left = 1;
            const int top = 1;

            // Info: we cannot test the AsPosition()-method since it uses the RestoreBounds-property of the Window-class that requires an actual window (with a handle)
            /* const Abstractions.WindowState windowState = Abstractions.WindowState.Minimized;
            var expectedPosition = new Position(width, height, left, top, windowState); */

            var window = new Window
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                WindowState = WindowState.Minimized
            };

            // Act
            Position actual = window.AsPosition();

            // Assert
            Assert.NotNull(actual);

            // Assert.Equal(expectedPosition, actual);
        }

        [WpfFact]
        public void WindowExtensions_SetBoundsFromPosition_Test()
        {
            // Arrange
            const int width = 640;
            const int height = 480;
            const int left = 1;
            const int top = 1;

            const Abstractions.WindowState windowState = Abstractions.WindowState.Normal;
            var position = new Position(width, height, left, top, windowState);

            var window = new Window();

            // Act
            window.SetBoundsFromPosition(position);

            // Assert
            Assert.Equal(left, window.Left);
            Assert.Equal(top, window.Top);
            Assert.Equal(width, window.Width);
            Assert.Equal(height, window.Height);

            Assert.Equal(WindowState.Normal, window.WindowState);
        }

        [WpfFact]
        public void WindowExtensions_SetBoundsFromPosition_state_null_restores_maximized_window_Test()
        {
            // Arrange
            const int width = 640;
            const int height = 480;
            const int left = 1;
            const int top = 1;

            Abstractions.WindowState? unset = null;
            var position = new Position(width, height, left, top, unset);

            var window = new Window();

            // Act
            window.SetBoundsFromPosition(position);

            // Assert
            Assert.Equal(double.NaN, window.Left);
            Assert.Equal(double.NaN, window.Top);
            Assert.Equal(double.NaN, window.Width);
            Assert.Equal(double.NaN, window.Height);

            Assert.Equal(WindowState.Maximized, window.WindowState);
        }
    }
}