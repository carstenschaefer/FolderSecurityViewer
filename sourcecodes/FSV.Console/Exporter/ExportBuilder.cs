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

namespace FSV.Console.Exporter
{
    using System;
    using Abstractions;
    using Configuration.Abstractions;
    using Managers;

    internal class ExportBuilder : IExportBuilder
    {
        internal const string Excel = "excel";
        internal const string Csv = "csv";
        internal const string Html = "html";
        private readonly IConfigurationPaths configurationPaths;

        private readonly IExportTableGenerator exportTableGenerator;

        public ExportBuilder(
            IExportTableGenerator exportTableGenerator,
            IConfigurationPaths configurationPaths)
        {
            this.exportTableGenerator = exportTableGenerator ?? throw new ArgumentNullException(nameof(exportTableGenerator));
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
        }

        public IExport Build(string exportFormat, string scannedDirectory, string baseFilePath)
        {
            return this.Build(exportFormat, scannedDirectory, baseFilePath, null);
        }

        public IExport Build(string exportFormat, string scannedDirectory, string baseFilePath, string fileNamePrefix)
        {
            exportFormat = this.GetFormat(exportFormat);
            var exportFilePath = new ExportFilePath(this.configurationPaths, baseFilePath, fileNamePrefix);
            return this.GetExporter(exportFormat, exportFilePath, scannedDirectory);
        }

        private IExport GetExporter(string type, ExportFilePath exportFilePath, string scannedDirectory)
        {
            return type switch
            {
                Excel => new Excel(this.exportTableGenerator, exportFilePath, scannedDirectory),
                Csv => new Csv(this.exportTableGenerator, exportFilePath, scannedDirectory),
                Html => new Html(this.exportTableGenerator, exportFilePath, scannedDirectory),
                _ => throw new InvalidExportTypeException()
            };
        }

        private string GetFormat(string format)
        {
            return string.IsNullOrEmpty(format) ? Html : format.ToLower();
        }
    }
}