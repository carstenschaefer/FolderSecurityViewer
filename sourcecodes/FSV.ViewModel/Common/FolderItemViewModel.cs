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

namespace FSV.ViewModel.Common
{
    public class FolderItemViewModel
    {
        private static readonly string[] Sizes = { "Bytes", "KB", "MB", "GB" };
        private string _sizeText;
        private string _sizeWithSubfoldersText;

        public long FileCount { get; internal set; }
        public long FileCountWithSubFolders { get; internal set; }
        public string FullName { get; internal set; }
        public string Name { get; internal set; }
        public string Owner { get; internal set; }

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
                            this._sizeText = $"{size:#.##} {(size != 0 ? Sizes[i] : string.Empty)}";
                            break;
                        }

                        size = size / 1024;
                    }
                }

                return this._sizeText;
            }
        }

        public double Size { get; set; }

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
                            this._sizeWithSubfoldersText = $"{size:#.##} {(size != 0 ? Sizes[i] : string.Empty)}";
                            break;
                        }

                        size = size / 1024;
                    }
                }

                return this._sizeWithSubfoldersText;
            }
        }

        public double SizeWithSubFolders { get; internal set; }
    }
}