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

namespace FSV.CsvExporter
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using CsvHelper;
    using FileSystem.Interop.Abstractions;
    using Resources;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
    public class CsvExportManager : IAsyncDisposable, IDisposable
    {
        private readonly CsvWriter csvWriter;
        private bool disposed;

        public CsvExportManager(Stream stream)
        {
            var writer = new StreamWriter(stream, Encoding.UTF8);
            this.csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
        }

        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();

            this.Dispose(false);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this.disposed)
            {
                return;
            }

            this.Flush();

            this.disposed = true;
            this.csvWriter?.Dispose();
        }

        public static CsvExportManager FromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
            }

            FileStream stream = File.OpenWrite(filePath);
            return new CsvExportManager(stream);
        }

        public void Flush()
        {
            this.csvWriter.Flush();
        }

        public async Task FlushAsync()
        {
            await this.csvWriter.FlushAsync();
        }

        public void WriteLargeData(string path, DataTable permissions, IEnumerable<IAclModel> acl, DateTime exportDate)
        {
            this.csvWriter.WriteField(exportDate.ToString("g") + " " + path + " - " + ExportResource.PermissionReportCaption);
            this.csvWriter.NextRecord();
            foreach (DataColumn item in permissions.Columns)
            {
                this.csvWriter.WriteField(item.ColumnName);
            }

            this.csvWriter.NextRecord();

            foreach (DataRow item in permissions.Rows)
            {
                for (var i = 0; i < permissions.Columns.Count; i++)
                {
                    if (item[i] is IEnumerable<string> list)
                    {
                        this.csvWriter.WriteField(string.Join(", ", list));
                    }
                    else
                    {
                        this.csvWriter.WriteField(item[i]);
                    }
                }

                this.csvWriter.NextRecord();
            }

            this.csvWriter.NextRecord();

            this.csvWriter.NextRecord();
        }

        public void WriteLargeData(string path, DataTable folders, string caption, DateTime exportDate)
        {
            if (folders is null)
            {
                throw new ArgumentNullException(nameof(folders));
            }

            if (string.IsNullOrWhiteSpace(caption))
            {
                throw new ArgumentException($"'{nameof(caption)}' cannot be null or whitespace.", nameof(caption));
            }

            string title = string.Concat(path ?? string.Empty, " - ", caption);

            this.csvWriter.WriteField(exportDate.ToString("g") + title);
            this.csvWriter.NextRecord();

            this.csvWriter.NextRecord();

            foreach (DataColumn column in folders.Columns)
            {
                this.csvWriter.WriteField(column.ColumnName);
            }

            this.csvWriter.NextRecord();

            foreach (DataRow row in folders.Rows)
            {
                foreach (DataColumn column in folders.Columns)
                {
                    if (row[column] is IEnumerable<string> list)
                    {
                        this.csvWriter.WriteField(string.Join(", ", list));
                    }
                    else
                    {
                        this.csvWriter.WriteField(row[column].ToString());
                    }
                }

                this.csvWriter.NextRecord();
            }

            this.csvWriter.NextRecord();
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await this.FlushAsync();

            if (this.disposed == false)
            {
                this.disposed = true;
                this.csvWriter?.Dispose();
            }
        }
    }
}