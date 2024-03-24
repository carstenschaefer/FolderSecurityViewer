// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Sch�fer, Matthias Friedrich, and Ritesh Gite
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

    public class PathSeparatorDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FirstTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
            if (itemsControl == null)
            {
                return this.DefaultTemplate;
            }

            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(container);

            return index == 0 ? this.FirstTemplate : this.DefaultTemplate;
        }
    }
}