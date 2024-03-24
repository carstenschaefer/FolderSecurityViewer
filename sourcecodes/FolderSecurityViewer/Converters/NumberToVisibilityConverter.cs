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

namespace FolderSecurityViewer.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class NumberToVisibilityConverter : IValueConverter
    {
        public OperatorOption Operator { get; set; }
        public object CompareWith { get; set; }

        public Visibility TrueVisibility { get; set; }

        public Visibility FalseVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return this.FalseVisibility;
            }

            // Here value is data passsed by binding. 
            return this.Operator switch
            {
                OperatorOption.Equal => value.Equals(this.CompareWith) ? this.TrueVisibility : this.FalseVisibility,
                OperatorOption.NotEqual => !value.Equals(this.CompareWith) ? this.TrueVisibility : this.FalseVisibility,
                _ => this.FalseVisibility
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}