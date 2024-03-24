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

namespace FSV.ViewModel.Folder
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using Common;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FoldersTableViewModel : IDisposable
    {
        private const string tableName = "FoldersTable";
        private readonly IConfigurationManager configurationManager;

        public FoldersTableViewModel(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            this.DataColumns = this.GetDataColumns();
            this.DataTable = this.GetDataTable();
        }

        public DataTable DataTable { get; }

        public DataColumnsModel DataColumns { get; }

        public void Dispose()
        {
            this.DataTable?.Dispose();
        }

        private DataColumnsModel GetDataColumns()
        {
            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report report = configRoot.Report;
            ReportFolder reportFolder = report.Folder;

            return new DataColumnsModel
            {
                Name = new DataColumnModel(ColumnNames.NameColumnName, Visibility.Visible),
                FullName = new DataColumnModel(ColumnNames.FullNameColumnName, Visibility.Visible),
                Owner = new DataColumnModel(ColumnNames.OwnerColumnName, reportFolder.Owner ? Visibility.Visible : Visibility.Hidden),
                FileCount = new DataColumnModel(ColumnNames.FileCountColumnName, reportFolder.IncludeFileCount ? Visibility.Visible : Visibility.Collapsed),
                FileCountWithSubFolders = new DataColumnModel(ColumnNames.FileCountWithSubFoldersColumnName, reportFolder.IncludeFileCount ? Visibility.Visible : Visibility.Collapsed),
                SizeText = new DataColumnModel(ColumnNames.SizeTextColumnName, reportFolder.IncludeSubFolderFileCount ? Visibility.Visible : Visibility.Collapsed),
                SizeTextWithSubFolders = new DataColumnModel(ColumnNames.SizeWithSubFoldersTextColumnName, reportFolder.IncludeSubFolderFileCount ? Visibility.Visible : Visibility.Collapsed)
            };
        }

        private DataTable GetDataTable()
        {
            DataColumnsModel columns = this.GetDataColumns();
            var table = new DataTable(tableName);
            foreach (DataColumnModel column in columns)
            {
                table.Columns.Add(new DataColumn(column.Header));
            }

            return table;
        }

        public string GetColumnName(int ordinal)
        {
            DataColumnCollection columnCollection = this.DataTable.Columns;
            if (ordinal < 0 || ordinal >= columnCollection.Count)
            {
                return null;
            }

            DataColumn column = columnCollection[ordinal];
            return column.ColumnName;
        }

        public void Clear()
        {
            this.DataTable.Rows.Clear();
        }

        public void AddRowFrom(FolderItemViewModel item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            DataRow row = this.DataTable.NewRow();

            row[ColumnNames.FileCountColumnName] = item.FileCount;
            row[ColumnNames.FileCountWithSubFoldersColumnName] = item.FileCountWithSubFolders;
            row[ColumnNames.FullNameColumnName] = item.FullName;
            row[ColumnNames.NameColumnName] = item.Name;
            row[ColumnNames.OwnerColumnName] = item.Owner;
            row[ColumnNames.SizeTextColumnName] = item.SizeText;
            row[ColumnNames.SizeWithSubFoldersTextColumnName] = item.SizeWithSubFoldersText;

            this.DataTable.Rows.Add(row);
        }

        public static class ColumnNames
        {
            public const string FileCountColumnName = "FileCount";
            public const string OwnerColumnName = "Owner";
            public const string FullNameColumnName = "FullName";
            public const string NameColumnName = "Name";
            public const string FileCountWithSubFoldersColumnName = "FileCountWithSubFolders";
            public const string SizeTextColumnName = "SizeText";
            public const string SizeWithSubFoldersTextColumnName = "SizeWithSubFoldersText";
        }
    }
}