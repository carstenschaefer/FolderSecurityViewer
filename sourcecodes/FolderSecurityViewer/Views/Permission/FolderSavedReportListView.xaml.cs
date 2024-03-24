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

namespace FolderSecurityViewer.Views.Permission
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using FSV.ViewModel.Permission;

    /// <summary>
    ///     Interaction logic for FolderSavedReportListView.xaml
    /// </summary>
    public partial class FolderSavedReportListView : UserControl
    {
        private int prevRowIndex = -1;
        private FolderSavedReportListViewModel viewModel;

        public FolderSavedReportListView()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.viewModel = this.DataContext as FolderSavedReportListViewModel;
            if (this.viewModel == null)
            {
                this.ItemsGrid.AllowDrop = false;
            }
        }

        private void ItemsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                return;
            }

            this.prevRowIndex = this.GetDataGridItemCurrentRowIndex(e.GetPosition);

            if (this.prevRowIndex < 0)
            {
                return;
            }

            if (this.ItemsGrid.Items[this.prevRowIndex] is SavedReportItemViewModel listItem)
            {
                //Now Create a Drag Rectangle with Mouse Drag-Effect
                //Here you can select the Effect as per your choice

                var dragdropeffects = DragDropEffects.Move;
                DragDrop.DoDragDrop(this.ItemsGrid, listItem, dragdropeffects);
            }
        }

        private async void ItemsGrid_Drop(object sender, DragEventArgs e)
        {
            if (this.prevRowIndex < 0)
            {
                return;
            }

            int index = this.GetDataGridItemCurrentRowIndex(e.GetPosition);

            //The current Rowindex is -1 (No selected)
            if (index < 0)
            {
                return;
            }

            //If Drag-Drop Location are same
            if (index == this.prevRowIndex)
            {
                return;
            }

            var item1 = this.ItemsGrid.Items[this.prevRowIndex] as SavedReportItemViewModel;
            var item2 = this.ItemsGrid.Items[index] as SavedReportItemViewModel;

            await this.viewModel.CompareAsync(item1, item2);
            this.prevRowIndex = -1;
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

        /// <summary>
        ///     Returns the selected DataGridRow
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private DataGridRow GetDataGridRowItem(int index)
        {
            if (this.ItemsGrid.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }

            return this.ItemsGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        }

        /// <summary>
        ///     Returns the Index of the Current Row.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int GetDataGridItemCurrentRowIndex(GetDragDropPosition pos)
        {
            int curIndex = -1;
            for (var i = 0; i < this.ItemsGrid.Items.Count; i++)
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

        private delegate Point GetDragDropPosition(IInputElement element);
    }
}