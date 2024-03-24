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

namespace FSV.HtmlExporter
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using FileSystem.Interop.Abstractions;
    using FSV.Resources;
    using HtmlTags;
    using Resources;

    public class HtmlExportManager
    {
        private readonly HtmlDocument _document = new();

        private readonly string _filePath;
        private readonly HtmlTag _mainDiv = new("div");
        private readonly object syncObject = new();

        public HtmlExportManager(string filePath)
        {
            this._filePath = filePath;

            this._document.AddStyle(MainResource.Style);

            this._document.Body.Append(this._mainDiv.AddClass("main"));
        }

        public void WriteLargeData(string path, DataTable permissions, IEnumerable<IAclModel> acl, DateTime exportDate)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace", nameof(path));
            }

            if (permissions is null)
            {
                throw new ArgumentNullException(nameof(permissions));
            }

            if (acl == null)
            {
                throw new ArgumentNullException(nameof(acl));
            }

            HtmlTag divContainer = new HtmlTag("div", this._mainDiv).AddClass("container");
            HtmlTag divPath = new HtmlTag("div", divContainer).AddClass("path");

            var spanDate = new HtmlTag("span", divPath);
            spanDate.Text(exportDate.ToString("g"));

            var spanPath = new HtmlTag("span", divPath);
            spanPath.Text(string.Concat(path, " - " + ExportResource.PermissionReportCaption));

            HtmlTag h1Permission = new HtmlTag("h1", divContainer).Text(ExportResource.PermissionsCaption);
            var permissionTable = new HtmlTag("table", divContainer);
            var permissionHeader = new HtmlTag("tr", permissionTable);

            foreach (DataColumn item in permissions.Columns)
            {
                var th = new HtmlTag("th", permissionHeader);
                th.Text(item.ColumnName);
            }

            foreach (DataRow item in permissions.Rows)
            {
                var tr = new HtmlTag("tr", permissionTable);

                for (var i = 0; i < permissions.Columns.Count; i++)
                {
                    var td = new HtmlTag("td", tr);
                    if (item[i] is IEnumerable<string> list)
                    {
                        td.Text(string.Join(", ", list));
                    }
                    else
                    {
                        td.Text(item[i].ToString());
                    }
                }
            }

            if (!acl.Any())
            {
                return;
            }

            Type modelType = typeof(IAclModel);
            Type aclType = modelType;
            PropertyInfo[] aclProperties = modelType.GetProperties();

            HtmlTag h1Acl = new HtmlTag("h1", divContainer).Text(ExportResource.ACLCaption);
            HtmlTag aclTable = new HtmlTag("table", divContainer).AddClass("acl");
            var aclHeader = new HtmlTag("tr", aclTable);

            foreach (PropertyInfo propertyInfo in aclProperties)
            {
                var td = new HtmlTag("th", aclHeader);
                td.Text(propertyInfo.Name);
            }

            aclHeader.Children.Last().Text(ExportResource.DiffFromParentCaption);

            foreach (IAclModel item in acl)
            {
                var tr = new HtmlTag("tr", aclTable);
                foreach (PropertyInfo property in aclProperties)
                {
                    var td = new HtmlTag("td", tr);
                    td.Text(aclType.GetProperty(property.Name).GetValue(item).ToString());
                }
            }
        }

        public void WriteLargeData(string path, DataTable userReports, IEnumerable<string> skippedFolders, DateTime exportDate)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace", nameof(path));
            }

            if (userReports is null)
            {
                throw new ArgumentNullException(nameof(userReports));
            }

            HtmlTag divContainer = new HtmlTag("div", this._mainDiv).AddClass("container");
            HtmlTag divPath = new HtmlTag("div", divContainer).AddClass("path");

            var spanDate = new HtmlTag("span", divPath);
            spanDate.Text(exportDate.ToString("g"));

            var spanPath = new HtmlTag("span", divPath);
            spanPath.Text(string.Concat(path, " - " + ExportResource.UserReportCaption));

            HtmlTag h1Permission = new HtmlTag("h1", divContainer).Text(ExportResource.UserReportCaption);
            var permissionTable = new HtmlTag("table", divContainer);
            var permissionHeader = new HtmlTag("tr", permissionTable);

            foreach (DataColumn item in userReports.Columns)
            {
                var th = new HtmlTag("th", permissionHeader);
                th.Text(item.ColumnName);
            }

            foreach (DataRow item in userReports.Rows)
            {
                var tr = new HtmlTag("tr", permissionTable);

                for (var i = 0; i < userReports.Columns.Count; i++)
                {
                    var td = new HtmlTag("td", tr);
                    if (item[i] is IEnumerable<string> list)
                    {
                        td.Text(string.Join(", ", list));
                    }
                    else
                    {
                        td.Text(item[i].ToString());
                    }
                }
            }

            if (skippedFolders == null || !skippedFolders.Any())
            {
                return;
            }

            HtmlTag skippedHeading = new HtmlTag("h1", divContainer).Text(ExportResource.UserReportSkippedFoldersCaption);
            HtmlTag skippedTable = new HtmlTag("table", divContainer).AddClass("acl");
            var skippedHeaderRow = new HtmlTag("tr", skippedTable);

            var headerCell = new HtmlTag("th", skippedHeaderRow);
            headerCell.Text(ExportResource.UserReportSkippedFoldersCaption);

            foreach (string item in skippedFolders)
            {
                var tr = new HtmlTag("tr", skippedTable);
                var cell = new HtmlTag("td", tr);
                cell.Text(item);
            }
        }

        public void WriteLargeData(string path, DataTable folders, string caption, DateTime exportDate)
        {
            if (folders is null)
            {
                throw new ArgumentNullException(nameof(folders));
            }

            if (string.IsNullOrWhiteSpace(caption))
            {
                throw new ArgumentException($"'{nameof(caption)}' cannot be null or whitespace", nameof(caption));
            }

            HtmlTag divContainer = new HtmlTag("div", this._mainDiv).AddClass("container");
            HtmlTag divPath = new HtmlTag("div", divContainer).AddClass("path"); //.Text(string.Concat(path, " - ", caption));

            var spanDate = new HtmlTag("span", divPath);
            spanDate.Text(exportDate.ToString("g"));

            string title = string.IsNullOrEmpty(path) ? caption : string.Concat(path, " - " + caption);

            var spanPath = new HtmlTag("span", divPath);
            spanPath.Text(title);

            var folderTable = new HtmlTag("table", divContainer);
            var folderHeader = new HtmlTag("tr", folderTable);

            HtmlTag table = new HtmlTag("table", divContainer).AddClass("acl");
            var header = new HtmlTag("tr", table);

            foreach (DataColumn column in folders.Columns)
            {
                var td = new HtmlTag("th", header);
                td.Text(column.ColumnName);
            }

            //header.Children.Last().Text("Diff. from Parent");

            foreach (DataRow row in folders.Rows)
            {
                var tr = new HtmlTag("tr", table);
                foreach (DataColumn column in folders.Columns)
                {
                    var td = new HtmlTag("td", tr);
                    if (row[column] is IEnumerable<string> list)
                    {
                        td.Text(string.Join(", ", list));
                    }
                    else
                    {
                        td.Text(row[column].ToString());
                    }
                }
            }
        }

        public void WriteLargeData(DataTable dataTable, string caption, DateTime exportDate)
        {
            if (dataTable is null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (string.IsNullOrWhiteSpace(caption))
            {
                throw new ArgumentException($"'{nameof(caption)}' cannot be null or whitespace", nameof(caption));
            }

            HtmlTag divContainer = new HtmlTag("div", this._mainDiv).AddClass("container");
            HtmlTag divPath = new HtmlTag("div", divContainer).AddClass("path"); //.Text(string.Concat(path, " - ", caption));

            var spanDate = new HtmlTag("span", divPath);
            spanDate.Text(exportDate.ToString("g"));

            var spanPath = new HtmlTag("span", divPath);
            spanPath.Text(string.Concat(caption));

            foreach (DataRow dataRow in dataTable.Rows)
            {
                new HtmlTag("h1", divContainer).Text(dataRow["Name"].ToString());

                HtmlTag htmlTable = new HtmlTag("table", divContainer).AddClass("light");

                var pathRow = new HtmlTag("tr", htmlTable);

                HtmlTag pathCaption = new HtmlTag("td", pathRow).Style("width", "15%").Style("text-align", "left");
                HtmlTag pathValue = new HtmlTag("td", pathRow).Style("text-align", "left");
                ;

                pathCaption.Text(SharedServersResource.PathCaption);
                pathValue.Text(dataRow["Path"].ToString());

                var descriptionRow = new HtmlTag("tr", htmlTable);
                new HtmlTag("td", descriptionRow).Text(SharedServersResource.DescriptionCaption);
                new HtmlTag("td", descriptionRow).Text(dataRow["Description"].ToString());

                var maxUsersRow = new HtmlTag("tr", htmlTable);
                new HtmlTag("td", maxUsersRow).Text(SharedServersResource.MaxUsersCaption);
                new HtmlTag("td", maxUsersRow).Text(dataRow["MaxUsers"].ToString());

                var clientConnectionsRow = new HtmlTag("tr", htmlTable);
                new HtmlTag("td", clientConnectionsRow).Text(SharedServersResource.ClientConnectionsCaption);
                new HtmlTag("td", clientConnectionsRow).Text(dataRow["ClientConnections"].ToString());

                var trusteesRow = new HtmlTag("tr", htmlTable);
                new HtmlTag("td", trusteesRow).Text(SharedServersResource.TrusteesCaption);

                var trusteesCell = new HtmlTag("td", trusteesRow);

                if (dataRow["Trustees"] is DataTable trusteesDataTable && trusteesDataTable.Rows.Count > 0)
                {
                    var trusteesHtmlTable = new HtmlTag("table", trusteesCell);
                    var trusteeHtmlHeaderRow = new HtmlTag("tr", trusteesHtmlTable);
                    foreach (DataColumn dataColumn in trusteesDataTable.Columns)
                    {
                        new HtmlTag("th", trusteeHtmlHeaderRow).Text(dataColumn.ColumnName);
                    }

                    foreach (DataRow trusteeDataRow in trusteesDataTable.Rows)
                    {
                        var trusteeHtmlRow = new HtmlTag("tr", trusteesHtmlTable);
                        foreach (DataColumn dataColumn in trusteesDataTable.Columns)
                        {
                            new HtmlTag("td", trusteeHtmlRow).Text(trusteeDataRow[dataColumn.ColumnName]?.ToString() ?? string.Empty);
                        }
                    }
                }
                else
                {
                    trusteesCell.Text(ExportResource.NoTrusteesCaption).AddClass("not-found");
                }
            }
        }

        public void Save()
        {
            this._document.WriteToFile(this._filePath);
        }

        public async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                lock (this.syncObject)
                {
                    this._document.WriteToFile(this._filePath);
                }
            });
        }
    }
}