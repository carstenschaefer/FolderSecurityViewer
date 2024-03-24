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

namespace FSV.ExcelExporter
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using ClosedXML.Excel;
    using FileSystem.Interop.Abstractions;
    using Resources;

    public class ExcelExportManager
    {
        private readonly string _filePath;

        /// <summary>
        ///     The spread sheet
        /// </summary>
        private readonly XLWorkbook _workbook;

        private readonly object syncObject = new();

        private IXLWorksheet _worksheet;

        public ExcelExportManager(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
            }

            this._filePath = filePath;

            this._workbook = new XLWorkbook();
        }

        private void SetHeaderStyle(IXLStyle style)
        {
            style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            style.Fill.BackgroundColor = XLColor.FromHtml("#225797");
            style.Font.FontColor = XLColor.FromHtml("#ffffff");
            style.Font.Bold = true;
        }

        private void SetAclHeaderStyle(IXLStyle style)
        {
            style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            style.Font.FontColor = XLColor.FromHtml("#225797");
            style.Font.Bold = true;
        }

        /// <summary>
        ///     Adds the worksheet.
        /// </summary>
        /// <param name="name">The name.</param>
        public void AddWorksheet(string name)
        {
            this._workbook.Worksheets.Add(name);
        }

        /// <summary>
        ///     Sets the cell value.
        /// </summary>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <param name="value">The value.</param>
        public void SetCellValue(string sheetName, string columnIndex, uint rowIndex, string value)
        {
            this._worksheet = this.GetWorksheetByName(sheetName);

            if (this._worksheet == null)
            {
                return;
            }

            this._worksheet.Cell((int)rowIndex, columnIndex).Value = value;
            this._workbook.SaveAs(this._filePath);
        }

        /// <summary>
        ///     Sets the cell value.
        /// </summary>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <param name="value">The value.</param>
        public void SetCellValue(string sheetName, int columnIndex, uint rowIndex, string value, uint styleIndex)
        {
            this._worksheet = this.GetWorksheetByName(sheetName);
            this._worksheet.Cell((int)rowIndex, columnIndex).Value = value;

            this._workbook.SaveAs(this._filePath);
        }

        /// <summary>
        ///     Saves this instance.
        /// </summary>
        public void Save()
        {
            this._workbook.SaveAs(this._filePath);
        }

        public async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                lock (this.syncObject)
                {
                    this._workbook.SaveAs(this._filePath);
                }
            });
        }

        /// <summary>
        ///     Converts to letter.
        /// </summary>
        /// <param name="col">The col.</param>
        /// <returns></returns>
        public string ConvertToLetter(int col)
        {
            var ret = string.Empty;

            int alpha = col / 27;
            int remainder = col - alpha * 26;
            if (alpha > 0)
            {
                ret = Convert.ToChar(alpha + 64).ToString();
            }

            if (remainder > 0)
            {
                ret += Convert.ToChar(remainder + 64).ToString();
            }

            return ret;
        }


        /// <summary>
        ///     Gets the name of the worksheet part by.
        /// </summary>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <returns></returns>
        private IXLWorksheet GetWorksheetByName(string sheetName)
        {
            IXLWorksheet sheet =
                this._workbook.Worksheets.SingleOrDefault(m => m.Name == sheetName);

            return sheet;
        }

        /// <summary>
        ///     Writes the large data.
        /// </summary>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="dataTable">The data table.</param>
        /// <param name="acl">The ACL view.</param>
        public void WriteLargeData(
            string sheetName, string path, DataTable dataTable, IEnumerable<IAclModel> acl, DateTime exportDate)
        {
            if (acl == null)
            {
                throw new ArgumentNullException(nameof(acl));
            }

            DataTable cloneTable = dataTable; //.Copy();  // No need to copy now as Group Inheritance is also exported in sheet.

            IXLWorksheet sheet = this.GetWorksheetByName(sheetName);
            if (sheet == null)
            {
                sheet = this._workbook.AddWorksheet(sheetName);
            }

            var row = 1;
            var column = 1;

            IXLCell dateCell = sheet.Cell(row, column);

            dateCell.Value = exportDate.ToString("g");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            dateCell.Style.Font.Bold = true;

            IXLCell pathCell = sheet.Cell(row, column + 1);
            pathCell.Value = path + " - " + ExportResource.PermissionReportCaption;
            pathCell.Style.Font.Bold = true;

            sheet.Range(1, 2, 1, 9).Row(1).Merge();

            column = 1;
            row += 2;

            foreach (DataColumn headerColumn in cloneTable.Columns)
            {
                IXLCell cell = sheet.Cell(row, column++);
                cell.Value = headerColumn.ColumnName;
            }

            this.SetHeaderStyle(sheet.Row(row).Style);

            foreach (DataRow dataRow in cloneTable.Rows)
            {
                column = 1;
                row++;
                foreach (DataColumn dataColumn in cloneTable.Columns)
                {
                    IXLCell cell = sheet.Cell(row, column++);
                    object data = dataRow[dataColumn.ColumnName];
                    if (data == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (data is IEnumerable<string> list)
                    {
                        cell.Value = string.Join(", ", list);
                    }
                    else
                    {
                        cell.Value = XLCellValue.FromObject(data);
                    }
                }
            }


            column = 1;
            row += 3;

            if (acl != null)
            {
                Type modelType = typeof(IAclModel);
                foreach (PropertyInfo propertyInfo in modelType.GetProperties())
                {
                    sheet.Cell(row, column).Value = propertyInfo.Name;
                    column++;
                }

                this.SetAclHeaderStyle(sheet.Row(row).Style);

                row++;
                // Acl data
                column = 1;

                IXLCell aclCell = sheet.Cell(row, column);
                aclCell.InsertData(acl);

                // end acl data
                //sheet.Column(5).AdjustToContents();
                //sheet.Column(3).AdjustToContents();
                // sheet.Columns().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Columns("A:I").AdjustToContents();
            }

            //_workbook.SaveAs(this._filePath);
        }

        public void WriteLargeData(
            string sheetName, string path, DataTable userReport, IEnumerable<string> skippedFolders, DateTime exportDate)
        {
            IXLWorksheet sheet = this.GetWorksheetByName(sheetName);
            if (sheet == null)
            {
                sheet = this._workbook.AddWorksheet(sheetName);
            }

            var row = 1;
            var column = 1;

            IXLCell dateCell = sheet.Cell(row, column);

            dateCell.Value = exportDate.ToString("g");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            dateCell.Style.Font.Bold = true;

            IXLCell pathCell = sheet.Cell(row, column + 1);
            pathCell.Value = path + " - " + ExportResource.UserReportCaption;
            pathCell.Style.Font.Bold = true;

            sheet.Range(1, 2, 1, 9).Row(1).Merge();

            column = 1;
            row += 2;

            foreach (DataColumn headerColumn in userReport.Columns)
            {
                IXLCell cell = sheet.Cell(row, column++);
                cell.Value = headerColumn.ColumnName;
            }

            this.SetHeaderStyle(sheet.Row(row).Style);

            foreach (DataRow dataRow in userReport.Rows)
            {
                column = 1;
                row++;
                foreach (DataColumn dataColumn in userReport.Columns)
                {
                    IXLCell cell = sheet.Cell(row, column++);
                    object data = dataRow[dataColumn.ColumnName];

                    if (data == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (data is IEnumerable<string> list)
                    {
                        cell.Value = string.Join(", ", list);
                    }
                    else
                    {
                        cell.Value = XLCellValue.FromObject(data);
                    }
                }
            }


            column = 1;
            row += 3;

            if (skippedFolders != null && skippedFolders.Any())
            {
                sheet.Cell(row, column).Value = ExportResource.UserReportSkippedFoldersCaption;
                this.SetAclHeaderStyle(sheet.Row(row).Style);

                row++;

                IXLCell aclCell = sheet.Cell(row, column);
                aclCell.InsertData(skippedFolders);

                sheet.Columns("A:I").AdjustToContents();
            }
        }

        public void WriteLargeData(string sheetName, string path, DataTable table, string caption, DateTime exportDate)
        {
            IXLWorksheet sheet = this.GetWorksheetByName(sheetName);
            if (sheet == null)
            {
                sheet = this._workbook.AddWorksheet(sheetName);
            }

            var row = 1;
            var column = 1;

            //sheet.Row(row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // sheet.Cell(row, column).Value = path;
            IXLCell dateCell = sheet.Cell(row, column);

            dateCell.Value = exportDate.ToString("g");
            dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            dateCell.Style.Font.Bold = true;

            IXLCell pathCell = sheet.Cell(row, column + 1);
            pathCell.Value = string.IsNullOrEmpty(path) ? caption : string.Concat(path, " - " + caption);
            pathCell.Style.Font.Bold = true;

            sheet.Range(1, 2, 1, 9).Row(1).Merge();

            row += 2;
            foreach (DataColumn headerColumn in table.Columns)
            {
                IXLCell cell = sheet.Cell(row, column++);
                cell.Value = headerColumn.ColumnName;
            }

            this.SetHeaderStyle(sheet.Row(row).Style);

            foreach (DataRow dataRow in table.Rows)
            {
                column = 1;
                row++;
                foreach (DataColumn dataColumn in table.Columns)
                {
                    IXLCell cell = sheet.Cell(row, column++);
                    object data = dataRow[dataColumn.ColumnName];

                    if (data == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (data is IEnumerable<string> list)
                    {
                        cell.Value = string.Join(", ", list);
                    }
                    else
                    {
                        cell.Value = XLCellValue.FromObject(data);
                    }
                }
            }

            // sheet.Tables.First().ShowAutoFilter = false;

            sheet.Columns("A:I").AdjustToContents();
        }

        public void WriteLargeData(DataTable table, string caption, DateTime exportDate)
        {
            // Loop through shares, create sheet for each share
            foreach (DataRow dataRow in table.Rows)
            {
                var sheetName = dataRow["Name"] as string;
                IXLWorksheet sheet = this.GetWorksheetByName(sheetName);
                if (sheet == null)
                {
                    sheet = this._workbook.AddWorksheet(sheetName);
                }

                var row = 1;
                var column = 1;

                //sheet.Row(row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // sheet.Cell(row, column).Value = path;
                IXLCell dateCell = sheet.Cell(row, column);

                dateCell.Value = exportDate.ToString("g");
                dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                dateCell.Style.Font.Bold = true;

                IXLCell pathCell = sheet.Cell(row, column + 1);
                pathCell.Value = caption;
                pathCell.Style.Font.Bold = true;

                sheet.Range(1, 2, 1, 5).Row(1).Merge();

                sheet.Cell(2, 1).Value = SharedServersResource.ShareNameCaption;
                sheet.Cell(2, 2).Value = XLCellValue.FromObject( dataRow["Name"]);
                sheet.Range(2, 2, 2, 5).Merge();

                sheet.Cell(3, 1).Value = SharedServersResource.PathCaption;
                sheet.Cell(3, 2).Value = XLCellValue.FromObject(dataRow["Path"]);
                sheet.Range(3, 2, 3, 5).Merge();

                sheet.Cell(4, 1).Value = SharedServersResource.DescriptionCaption;
                sheet.Cell(4, 2).Value = XLCellValue.FromObject(dataRow["Description"]);
                sheet.Range(4, 2, 4, 5).Merge();

                sheet.Cell(5, 1).Value = SharedServersResource.MaxUsersCaption;
                sheet.Cell(5, 2).Value = XLCellValue.FromObject(dataRow["MaxUsers"]);
                sheet.Cell(5, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Range(5, 2, 5, 5).Merge();

                sheet.Cell(6, 1).Value = SharedServersResource.ClientConnectionsCaption;
                sheet.Cell(6, 2).Value = XLCellValue.FromObject(dataRow["ClientConnections"]);
                sheet.Cell(6, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                sheet.Range(6, 2, 6, 5).Merge();

                sheet.Cell(7, 1).Value = SharedServersResource.TrusteesCaption;
                if (dataRow["Trustees"] is DataTable trusteeTable)
                {
                    var colCount = 2;
                    foreach (DataColumn dataColumn in trusteeTable.Columns)
                    {
                        IXLCell headerCell = sheet.Cell(7, colCount++);
                        headerCell.Value = dataColumn.ColumnName;
                        headerCell.Style.Font.Bold = true;
                    }

                    var rowCount = 8;
                    foreach (DataRow trusteeRow in trusteeTable.Rows)
                    {
                        colCount = 2;
                        foreach (object item in trusteeRow.ItemArray)
                        {
                            sheet.Cell(rowCount, colCount++).Value = XLCellValue.FromObject(item);
                        }

                        rowCount++;
                    }
                }

                sheet.Column("A").AdjustToContents();
            }
        }
    }
}