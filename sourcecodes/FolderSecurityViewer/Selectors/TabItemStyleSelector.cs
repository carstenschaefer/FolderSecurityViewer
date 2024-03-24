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

namespace FolderSecurityViewer.Selectors
{
    using System.Windows;
    using System.Windows.Controls;

    public class TabItemStyleSelector : StyleSelector
    {
        public Style FirstStyle { get; set; }
        public Style DefaultStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var tab = ItemsControl.ItemsControlFromItemContainer(container) as TabControl;
            int index = tab.ItemContainerGenerator.IndexFromContainer(container);

            if (index == 0)
            {
                return this.FirstStyle;
            }

            return this.DefaultStyle;
        }
    }
}