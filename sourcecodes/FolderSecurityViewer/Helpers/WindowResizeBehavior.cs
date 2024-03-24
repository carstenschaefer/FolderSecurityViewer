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
    using System.Windows.Controls.Primitives;

    public static class WindowResizeBehavior
    {
        public static readonly DependencyProperty TopLeftResize = DependencyProperty.RegisterAttached("TopLeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopLeftResizeChanged));

        public static readonly DependencyProperty TopRightResize = DependencyProperty.RegisterAttached("TopRightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopRightResizeChanged));

        public static readonly DependencyProperty BottomRightResize = DependencyProperty.RegisterAttached("BottomRightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomRightResizeChanged));

        public static readonly DependencyProperty BottomLeftResize = DependencyProperty.RegisterAttached("BottomLeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomLeftResizeChanged));

        public static readonly DependencyProperty LeftResize = DependencyProperty.RegisterAttached("LeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnLeftResizeChanged));

        public static readonly DependencyProperty RightResize = DependencyProperty.RegisterAttached("RightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnRightResizeChanged));

        public static readonly DependencyProperty TopResize = DependencyProperty.RegisterAttached("TopResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopResizeChanged));

        public static readonly DependencyProperty BottomResize = DependencyProperty.RegisterAttached("BottomResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomResizeChanged));

        public static Window GetTopLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopLeftResize);
        }

        public static void SetTopLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopLeftResize, window);
        }

        private static void OnTopLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTopLeft;
            }
        }

        public static Window GetTopRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopRightResize);
        }

        public static void SetTopRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopRightResize, window);
        }

        private static void OnTopRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTopRight;
            }
        }

        public static Window GetBottomRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomRightResize);
        }

        public static void SetBottomRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomRightResize, window);
        }

        private static void OnBottomRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottomRight;
            }
        }

        public static Window GetBottomLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomLeftResize);
        }

        public static void SetBottomLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomLeftResize, window);
        }

        private static void OnBottomLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottomLeft;
            }
        }

        public static Window GetLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(LeftResize);
        }

        public static void SetLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(LeftResize, window);
        }

        private static void OnLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragLeft;
            }
        }

        public static Window GetRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(RightResize);
        }

        public static void SetRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(RightResize, window);
        }

        private static void OnRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragRight;
            }
        }

        public static Window GetTopResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopResize);
        }

        public static void SetTopResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopResize, window);
        }

        private static void OnTopResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTop;
            }
        }

        public static Window GetBottomResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomResize);
        }

        public static void SetBottomResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomResize, window);
        }

        private static void OnBottomResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottom;
            }
        }

        private static void DragLeft(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(LeftResize) is Window window)
            {
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);
                window.Width -= horizontalChange;
                window.Left += horizontalChange;
            }
        }

        private static void DragRight(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(RightResize) is Window window)
            {
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange);
                window.Width += horizontalChange;
            }
        }

        private static void DragTop(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(TopResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void DragBottom(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(BottomResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange);
                window.Height += verticalChange;
            }
        }

        private static void DragTopLeft(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(TopLeftResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

                window.Width -= horizontalChange;
                window.Left += horizontalChange;
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void DragTopRight(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(TopRightResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange);

                window.Width += horizontalChange;
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void DragBottomRight(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(BottomRightResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange);
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange);

                window.Width += horizontalChange;
                window.Height += verticalChange;
            }
        }

        private static void DragBottomLeft(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb thumb && thumb.GetValue(BottomLeftResize) is Window window)
            {
                double verticalChange = window.SafeHeightChange(e.VerticalChange);
                double horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

                window.Width -= horizontalChange;
                window.Left += horizontalChange;
                window.Height += verticalChange;
            }
        }

        private static double SafeWidthChange(this Window window, double change, bool positive = true)
        {
            double result = positive ? window.Width + change : window.Width - change;

            if (result <= window.MinWidth)
            {
                return 0;
            }

            if (result >= window.MaxWidth)
            {
                return 0;
            }

            if (result < 0)
            {
                return 0;
            }

            return change;
        }

        private static double SafeHeightChange(this Window window, double change, bool positive = true)
        {
            double result = positive ? window.Height + change : window.Height - change;

            if (result <= window.MinHeight)
            {
                return 0;
            }

            if (result >= window.MaxHeight)
            {
                return 0;
            }

            if (result < 0)
            {
                return 0;
            }

            return change;
        }
    }
}