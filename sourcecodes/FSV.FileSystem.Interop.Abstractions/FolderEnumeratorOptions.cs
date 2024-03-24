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

namespace FSV.FileSystem.Interop.Abstractions
{
    using System.Diagnostics.CodeAnalysis;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FolderEnumeratorOptions
    {
        private int scanDepth;

        public FolderEnumeratorOptions(bool subtree, bool includeHiddenFolder, bool includeCurrentFolder, bool getSizeAndFileCount, bool getSizeAndFileCountForSubTree, bool includeOwner, string ownerFilter)
        {
            this.Subtree = subtree;
            this.IncludeHiddenFolder = includeHiddenFolder;
            this.IncludeCurrentFolder = includeCurrentFolder;
            this.GetSizeAndFileCount = getSizeAndFileCount;
            this.GetSizeAndFileCountForSubTree = getSizeAndFileCountForSubTree;
            this.IncludeOwner = includeOwner;
            this.OwnerFilter = ownerFilter;
        }

        /// <summary>
        ///     Gets or sets a value indicating the folder-enumerator´s scan-depth.
        /// </summary>
        public int ScanDepth
        {
            get => this.scanDepth;
            set
            {
                const int infinite = 0;
                if (value >= infinite)
                {
                    this.scanDepth = value;
                }
            }
        }

        public bool IncludeHiddenFolder { get; }
        public bool IncludeCurrentFolder { get; }
        public bool GetSizeAndFileCount { get; }
        public bool GetSizeAndFileCountForSubTree { get; }
        public bool IncludeOwner { get; }
        public string OwnerFilter { get; }
        public bool Subtree { get; }
    }
}