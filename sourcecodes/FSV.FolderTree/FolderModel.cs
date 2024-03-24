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

namespace FSV.FolderTree
{
    public class FolderModel
    {
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the path.
        /// </summary>
        /// <value>
        ///     The path.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        ///     Gets or sets the parent path.
        /// </summary>
        /// <value>
        ///     The parent path.
        /// </value>
        public string ParentPath { get; set; }

        /// <summary>
        ///     Gets or sets the image.
        /// </summary>
        /// <value>
        ///     The image.
        /// </value>
        public EnumTreeNodeImage Image { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [access denied].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [access denied]; otherwise, <c>false</c>.
        /// </value>
        public bool AccessDenied { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance has sub folders.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has sub folders; otherwise, <c>false</c>.
        /// </value>
        public bool HasSubFolders { get; set; }

        public bool IsUncPath { get; set; }
    }
}