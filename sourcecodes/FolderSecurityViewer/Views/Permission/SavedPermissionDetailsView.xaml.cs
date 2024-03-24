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
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Controls;
    using FSV.ViewModel.Permission;

    /// <summary>
    ///     Interaction logic for SavedPermissionDetailsView.xaml
    /// </summary>
    public partial class SavedPermissionDetailsView : BaseGridViewControl<SavedReportDetailListViewModel>
    {
        public SavedPermissionDetailsView()
        {
            this.InitializeComponent();

            this.DataGrid = this.DetailGrid;
            this.DataContextChanged += this.HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (this.DataContext is SavedReportDetailListViewModel model)
            {
                this.FillColumns(model.GridMetadata);
            }
        }

        private void FillColumns(GridMetadataModel view)
        {
            this.DetailGrid.Columns.Clear();
            IReadOnlyDictionary<string, string> columns = view.GetColumns<SavedReportDetailItemViewModel>();
            foreach (KeyValuePair<string, string> item in columns)
            {
                this.DetailGrid.Columns.Add(new DataGridTextColumn
                {
                    IsReadOnly = true,
                    Header = item.Value,
                    Binding = new Binding(item.Key)
                });
            }
        }
    }
}