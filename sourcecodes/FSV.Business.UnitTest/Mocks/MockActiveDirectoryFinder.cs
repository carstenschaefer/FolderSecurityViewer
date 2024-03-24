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

namespace FSV.Business.UnitTest.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Threading;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration;
    using Microsoft.Extensions.Logging.Abstractions;

    internal class MockActiveDirectoryFinder : IUserActiveDirectoryFinder
    {
        private readonly ActiveDirectoryScanResult<int> scanResult;

        public MockActiveDirectoryFinder(ActiveDirectoryScanResult<int> scanResult)
        {
            this.ScanOptions = new ActiveDirectoryScanOptions(new NullLogger<ActiveDirectoryScanOptions>())
            {
                BuiltInGroups = new List<string> { "Adminstrators" },
                ExclusionGroups = new List<ConfigItem>(),
                PermissionGridColumns = new List<ConfigItem>(),
                SkipBuiltInGroups = true,
                TranslatedItems = new List<ConfigItem>()
            };

            this.scanResult = scanResult ?? throw new ArgumentNullException(nameof(scanResult));
            this.FillColumns();
        }

        public ActiveDirectoryScanOptions ScanOptions { get; }

        public DirectoryInfo CurrentDirectory { get; set; }

        public bool FindUser(string accountName,
            string aclRight,
            string fileSystemRight,
            string host,
            string localGroupName,
            string originatingGroup,
            CancellationToken cancellationToken)
        {
            DataTable table = this.scanResult.Result;
            table.Rows.Add(this.CurrentDirectory.Name, this.CurrentDirectory.FullName, accountName);
            return true;
        }

        private void FillColumns()
        {
            DataTable table = this.scanResult.Result;
            table.Columns.Add("Folder");
            table.Columns.Add("FullName");
            table.Columns.Add("Originating Group");
        }
    }
}