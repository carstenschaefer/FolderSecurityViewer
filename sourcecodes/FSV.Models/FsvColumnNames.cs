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

namespace FSV.Models
{
    public class FsvColumnNames
    {
        public string Domain { get; set; }

        public string OriginatingGroup { get; set; }
        public string Sid { get; set; }
        public string Rigths { get; set; }

        /// <summary>
        ///     Gets or sets column name for complete path of folder.
        /// </summary>
        /// <remarks>Used in User Permission Report</remarks>
        public string CompleteName { get; set; }

        /// <summary>
        ///     Gets or sets column name for only name of folder.
        /// </summary>
        /// <remarks>Used in User Permission Report</remarks>
        public string Folder { get; set; }
    }
}