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
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    public class CommandTextBox : TextBox
    {
        public static DependencyProperty ButtonsProperty = DependencyProperty.Register("Buttons", typeof(List<ButtonBase>), typeof(CommandTextBox));
        public static DependencyProperty PlaceholderProperty = DependencyProperty.Register("Placeholder", typeof(string), typeof(CommandTextBox));

        public CommandTextBox()
        {
            this.Buttons = new List<ButtonBase>();
        }

        public List<ButtonBase> Buttons
        {
            get => this.GetValue(ButtonsProperty) as List<ButtonBase>;
            set => this.SetValue(ButtonsProperty, value);
        }

        public string Placeholder
        {
            get => this.GetValue(PlaceholderProperty) as string;
            set => this.SetValue(PlaceholderProperty, value);
        }
    }
}