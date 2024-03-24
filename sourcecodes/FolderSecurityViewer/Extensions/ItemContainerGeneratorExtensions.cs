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

namespace FolderSecurityViewer.Extensions
{
    using System;
    using System.Windows.Controls;

    public static class ItemContainerGeneratorExtensionMethods
    {
        public static ItemsControl ContainerFromItem<T>(
            this ItemContainerGenerator rootContainerGenerator,
            T item,
            Func<T, T> itemParentSelector)
        {
            if (item == null)
            {
                return null;
            }

            if (itemParentSelector == null)
            {
                throw new ArgumentNullException(nameof(itemParentSelector));
            }

            T parentItem = itemParentSelector(item);

            //  When we run out of parents, we're a root level node so we query the 
            //  rootContainerGenerator itself for the top level child container, and 
            //  start unwinding back to the item the caller gave us. 
            if (parentItem == null)
                //  Our item is the parent of our caller's item. 
                //  This is the parent of our caller's container. 
            {
                return rootContainerGenerator.ContainerFromItem(item) as ItemsControl;
            }

            //  This gets parents by unwinding the stack back down from root
            ItemsControl parentContainer =
                ContainerFromItem(rootContainerGenerator, parentItem, itemParentSelector);

            return parentContainer.ItemContainerGenerator.ContainerFromItem(item)
                as ItemsControl;
        }
    }
}