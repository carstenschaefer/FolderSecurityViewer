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
    using System.Collections.Generic;
    using System.Windows;

    public static class TabCollectorHelper
    {
        public static readonly DependencyProperty DisableOnTourStartProperty =
            DependencyProperty.RegisterAttached("DisableOnTourStart", typeof(bool), typeof(TabCollectorHelper), new FrameworkPropertyMetadata(OnDisableOnTourStartChanged));

        internal static IList<UIElement> AttachedElements { get; } = new List<UIElement>(10);

        internal static bool IsTourRunning { get; } = false;

        public static void SetDisableOnTourStart(UIElement dp, bool value)
        {
            dp.SetValue(DisableOnTourStartProperty, value);
        }

        public static bool GetDisableOnTourStart(UIElement dp)
        {
            return (bool)dp.GetValue(DisableOnTourStartProperty);
        }

        private static void OnDisableOnTourStartChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var tc = dependencyObject as UIElement;

            if (tc == null || !e.Property.Name.Equals(DisableOnTourStartProperty.Name))
            {
                return;
            }

            bool contains = AttachedElements.Contains(tc);
            var value = (bool)e.NewValue;

            if (!contains && value)
            {
                if (IsTourRunning)
                {
                    tc.IsEnabled = false;
                }

                AttachedElements.Add(tc);
            }

            if (contains && !value)
            {
                AttachedElements.Remove(tc);
            }
        }
    }
}