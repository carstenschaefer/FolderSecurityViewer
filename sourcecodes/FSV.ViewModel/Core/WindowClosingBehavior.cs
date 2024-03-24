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

namespace FSV.ViewModel.Core
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;

    public class WindowClosingBehavior
    {
        public static readonly DependencyProperty ClosedProperty = DependencyProperty.RegisterAttached(
            "Closed", typeof(ICommand), typeof(WindowClosingBehavior),
            new UIPropertyMetadata(ClosedChanged));

        public static readonly DependencyProperty ClosingProperty = DependencyProperty.RegisterAttached(
            "Closing", typeof(ICommand), typeof(WindowClosingBehavior),
            new UIPropertyMetadata(ClosingChanged));

        public static readonly DependencyProperty CancelClosingProperty = DependencyProperty.RegisterAttached(
            "CancelClosing", typeof(ICommand), typeof(WindowClosingBehavior));

        public static ICommand GetClosed(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ClosedProperty);
        }

        public static void SetClosed(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ClosedProperty, value);
        }

        private static void ClosedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var window = target as Window;

            if (window != null)
            {
                if (e.NewValue != null)
                {
                    window.Closed += Window_Closed;
                }
                else
                {
                    window.Closed -= Window_Closed;
                }
            }
        }

        public static ICommand GetClosing(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ClosingProperty);
        }

        public static void SetClosing(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ClosingProperty, value);
        }

        private static void ClosingChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var window = target as Window;

            if (window != null)
            {
                if (e.NewValue != null)
                {
                    window.Closing += Window_Closing;
                }
                else
                {
                    window.Closing -= Window_Closing;
                }
            }
        }

        public static ICommand GetCancelClosing(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CancelClosingProperty);
        }

        public static void SetCancelClosing(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CancelClosingProperty, value);
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            ICommand closed = GetClosed(sender as Window);
            if (closed != null)
            {
                closed.Execute(null);
            }
        }

        private static void Window_Closing(object sender, CancelEventArgs e)
        {
            ICommand closing = GetClosing(sender as Window);
            if (closing != null)
            {
                if (closing.CanExecute(null))
                {
                    closing.Execute(null);
                }
                else
                {
                    ICommand cancelClosing = GetCancelClosing(sender as Window);
                    if (cancelClosing != null)
                    {
                        cancelClosing.Execute(null);
                    }

                    e.Cancel = true;
                }
            }
        }
    }
}