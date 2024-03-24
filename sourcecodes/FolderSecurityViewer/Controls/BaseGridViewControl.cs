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
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using FSV.ViewModel.Abstractions;

    public class BaseGridViewControl<T> : UserControl where T : class, ISortable
    {
        private ListSortDirection _sortDirection;
        private string _sortMemberPath;

        public BaseGridViewControl()
        {
            this.Loaded += this.ViewLoaded;
            this.DataContextChanged += this.HandleDataContextChanged;
        }

        protected T ModelContext { get; private set; }

        protected DataGrid DataGrid { get; set; }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine($@"Setting {nameof(this.ModelContext)} property of {this.GetType().FullName} control to instance of {typeof(T).FullName}.");

            this.ModelContext = this.DataContext as T;
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (this.DataGrid == null)
            {
                return;
            }

            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
                .AddValueChanged(this.DataGrid, this.DataGridItemContainerStatusChanged);

            this.DataGrid.ItemContainerGenerator.StatusChanged += this.DataGridItemContainerStatusChanged;
            this.DataGrid.Sorting += this.DataGrid_Sorting;
        }

        private void DataGridItemContainerStatusChanged(object sender, EventArgs e)
        {
            this.SetSortDirection();
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (this.ModelContext != null)
            {
                e.Handled = true;

                this._sortMemberPath = e.Column.SortMemberPath;
                this._sortDirection = e.Column.SortDirection != ListSortDirection.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;

                e.Column.SortDirection = this._sortDirection;

                this.ModelContext.SortColumn = this._sortMemberPath;
                this.ModelContext.SortDirection = SortOrder.From(this._sortDirection);

                this.ModelContext.SortCommand?.Execute(null);
            }
        }

        private void SetSortDirection()
        {
            if (this.DataGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && this.ModelContext != null && !string.IsNullOrEmpty(this.ModelContext.SortColumn))
            {
                DataGridColumn sortColumn = this.DataGrid.Columns.FirstOrDefault(m => m.SortMemberPath == this.ModelContext.SortColumn);
                if (sortColumn != null)
                {
                    sortColumn.SortDirection = this.ModelContext.SortDirection.ToSortDirection();
                }

                this.DataGrid.ItemContainerGenerator.StatusChanged -= this.DataGridItemContainerStatusChanged;
            }
        }

        /// <summary>
        ///     Returns the selected DataGridRow
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private DataGridRow GetDataGridRowItem(int index)
        {
            if (this.DataGrid.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }

            return this.DataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        }

        /// <summary>
        ///     Returns the Index of the Current Row.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected int GetDataGridItemCurrentRowIndex(GetDragDropPosition pos)
        {
            int curIndex = -1;
            for (var i = 0; i < this.DataGrid.Items.Count; i++)
            {
                DataGridRow itm = this.GetDataGridRowItem(i);
                if (this.IsTheMouseOnTargetRow(itm, pos))
                {
                    curIndex = i;
                    break;
                }
            }

            return curIndex;
        }

        private bool IsTheMouseOnTargetRow(Visual theTarget, GetDragDropPosition pos)
        {
            if (theTarget == null)
            {
                return false;
            }

            Rect posBounds = VisualTreeHelper.GetDescendantBounds(theTarget);
            Point theMousePos = pos((IInputElement)theTarget);
            return posBounds.Contains(theMousePos);
        }

        protected delegate Point GetDragDropPosition(IInputElement element);
    }
}