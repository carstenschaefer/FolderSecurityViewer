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
    using System;
    using System.IO;
    using FileSystem.Interop.Abstractions;

    public sealed class FolderModelBuilder
    {
        private readonly IFileManagementService fileManagementService;

        public FolderModelBuilder(IFileManagementService fileManagementService)
        {
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        }

        /// <summary>
        ///     Gets the folder item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>filled Folder Model</returns>
        public static FolderModel GetFolderItem(IFolder item, string parentPath)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            bool accessDenied = item.AccessDenied;
            var hasSubFolders = false;
            var img = EnumTreeNodeImage.Folder;

            if (!accessDenied)
            {
                hasSubFolders = item.HasSubFolders;
            }
            else
            {
                img = EnumTreeNodeImage.AccessDenied;
            }

            return new FolderModel
            {
                Name = item.Name,
                Path = item.FullName,
                ParentPath = parentPath,
                AccessDenied = accessDenied,
                Image = img,
                HasSubFolders = hasSubFolders,
                IsUncPath = item.FullName.StartsWith(@"\\")
            };
        }

        /// <summary>
        ///     Gets the drive item.
        /// </summary>
        /// <param name="drive">The drive.</param>
        /// <returns>filled Drive Item</returns>
        public FolderModel GetDriveItem(DriveInfo drive)
        {
            if (drive == null)
            {
                throw new ArgumentNullException(nameof(drive));
            }

            DirectoryInfo rootDirectory = drive.RootDirectory;

            bool isReady = drive.IsReady;
            var hasSubFolders = false;
            var img = EnumTreeNodeImage.Drive;

            if (!isReady)
            {
                img = EnumTreeNodeImage.DriveNotReady;
            }
            else
            {
                hasSubFolders = this.fileManagementService.HasSubFolders(rootDirectory.FullName);
            }

            return new FolderModel
            {
                Name = rootDirectory.Name,
                Path = rootDirectory.FullName,
                ParentPath = string.Empty,
                AccessDenied = !isReady,
                Image = img,
                HasSubFolders = hasSubFolders
            };
        }

        public FolderModel GetUncPathItem(string uncPath)
        {
            if (string.IsNullOrWhiteSpace(uncPath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(uncPath));
            }

            var rootDirectory = new DirectoryInfo(uncPath);
            const bool isReady = true;
            var hasSubFolders = false;
            const EnumTreeNodeImage img = EnumTreeNodeImage.Drive;

            hasSubFolders = this.fileManagementService.HasSubFolders(rootDirectory.FullName);

            return new FolderModel
            {
                Name = rootDirectory.FullName,
                Path = rootDirectory.FullName,
                ParentPath = string.Empty,
                AccessDenied = !isReady,
                Image = img,
                HasSubFolders = hasSubFolders,
                IsUncPath = true
            };
        }
    }
}