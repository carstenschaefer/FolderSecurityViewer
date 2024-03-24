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

namespace FSV.Console.Models
{
    using System;
    using FileSystem.Interop.Abstractions;

    public class FolderItem
    {
        private static readonly string[] Sizes = { "Bytes", "KB", "MB", "GB" };

        private readonly IFolderReport _report;

        private string _sizeText;
        private string _sizeWithSubfoldersText;

        internal FolderItem(IFolderReport report)
        {
            this._report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public long FileCount => this._report.FileCount;
        public long FileCountWithSubFolders => this._report.FileCountInclSub;
        public string FullName => this._report.FullName;
        public string Name => this._report.Name;
        public string Owner => this._report.Owner;

        public string SizeText
        {
            get
            {
                if (string.IsNullOrEmpty(this._sizeText))
                {
                    double size = this.Size;
                    for (var i = 0; i < Sizes.Length; i++)
                    {
                        if (size < 1024)
                        {
                            this._sizeText = string.Format("{0:#.##} {1}", size, size != 0 ? Sizes[i] : string.Empty);
                            break;
                        }

                        size /= 1024;
                    }
                }

                return this._sizeText;
            }
        }

        public double Size => this._report.Size;

        public string SizeWithSubFoldersText
        {
            get
            {
                if (string.IsNullOrEmpty(this._sizeWithSubfoldersText))
                {
                    double size = this.SizeWithSubFolders;
                    for (var i = 0; i < Sizes.Length; i++)
                    {
                        if (size < 1024)
                        {
                            this._sizeWithSubfoldersText = string.Format("{0:#.##} {1}", size, size != 0 ? Sizes[i] : string.Empty);
                            break;
                        }

                        size /= 1024;
                    }
                }

                return this._sizeWithSubfoldersText;
            }
        }

        public double SizeWithSubFolders => this._report.SizeInclSub;
    }
}