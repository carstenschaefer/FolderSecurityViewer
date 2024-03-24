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

namespace FolderSecurityViewer.Views.User
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Controls;
    using FSV.ViewModel.UserReport;

    /// <summary>
    ///     Interaction logic for SavedAllUserReportView.xaml
    /// </summary>
    public partial class SavedAllUserReportView : BaseGridViewControl<SavedUserReportListViewModel>
    {
        private int prevRowIndex = -1;

        public SavedAllUserReportView()
        {
            this.InitializeComponent();

            this.DataGrid = this.ItemsGrid;

            this.DataContextChanged += this.HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.ModelContext == null)
            {
                this.ItemsGrid.AllowDrop = false;
                this.ItemsGrid.CanUserSortColumns = false;
            }
        }

        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (SavedUserReportListItemViewModel item in e.AddedItems.Cast<SavedUserReportListItemViewModel>())
            {
                item.IsSelected = true;
            }

            foreach (SavedUserReportListItemViewModel item in e.RemovedItems.Cast<SavedUserReportListItemViewModel>())
            {
                item.IsSelected = false;
            }
        }

        private void ItemsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                return;
            }

            if (!this.ItemsGrid.AllowDrop)
            {
                return;
            }

            this.prevRowIndex = this.GetDataGridItemCurrentRowIndex(e.GetPosition);

            if (this.prevRowIndex < 0)
            {
                return;
            }

            //ItemsGrid.SelectedIndex = prevRowIndex;

            var listItem = this.ItemsGrid.Items[this.prevRowIndex] as SavedUserReportListItemViewModel;

            if (listItem == null)
            {
                return;
            }

            //Now Create a Drag Rectangle with Mouse Drag-Effect
            //Here you can select the Effect as per your choice

            var dragdropeffects = DragDropEffects.Move;

            if (DragDrop.DoDragDrop(this.ItemsGrid, listItem, dragdropeffects) != DragDropEffects.None)
            {
                //Now This Item will be dropped at new location and so the new Selected Item
                //ItemsGrid.SelectedItem = listItem;
            }
        }

        private void ItemsGrid_Drop(object sender, DragEventArgs e)
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

            var item1 = this.ItemsGrid.Items[this.prevRowIndex] as SavedUserReportListItemViewModel;
            var item2 = this.ItemsGrid.Items[index] as SavedUserReportListItemViewModel;

            this.ModelContext.Compare(item1, item2);
            this.prevRowIndex = -1;
        }
    }
}