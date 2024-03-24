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

namespace FSV.FileSystem.Interop
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Abstractions;
    using Core;
    using Core.Abstractions;

    public sealed class FileManagementService : IFileManagementService
    {
        private readonly IFileManagement fileManagement;

        public FileManagementService(IFileManagement fileManagement)
        {
            this.fileManagement = fileManagement ?? throw new ArgumentNullException(nameof(fileManagement));
        }

        public IEnumerable<IFolder> GetDirectories(LongPath dirName)
        {
            if (dirName == null)
            {
                throw new ArgumentNullException(nameof(dirName));
            }

            try
            {
                return this.fileManagement.GetDirectories(dirName);
            }
            catch (FindFileEnumeratorException e)
            {
                throw new FileManagementServiceException($"Failed to get directories for path {dirName} due to an error. See inner exception for further details.", e);
            }
        }

        public bool GetDirectoryExist(LongPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return this.fileManagement.DirectoryExist(path);
        }

        public bool IsAccessDenied(LongPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return this.fileManagement.IsAccessDenied(path);
        }

        public DirectoryInfo GetDirectoryInfo(LongPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return this.fileManagement.GetDirectoryInfo(path);
        }

        public bool HasSubFolders(IFolder folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }

            return this.fileManagement.HasSubFolders(folder.FullName);
        }

        public bool HasSubFolders(LongPath folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }

            return this.fileManagement.HasSubFolders(folder);
        }
    }
}