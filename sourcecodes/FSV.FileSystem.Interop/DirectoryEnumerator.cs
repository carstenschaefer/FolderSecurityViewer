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
    using System.Linq;
    using System.Threading;
    using Abstractions;
    using Core.Abstractions;
    using Microsoft.Extensions.Logging;

    public class DirectoryEnumerator : IDirectoryEnumerator
    {
        private readonly IDirectorySizeService directorySizeService;
        private readonly IFileManagementService fileManagementService;
        private readonly bool getSizeAndFileCount;
        private readonly bool getSizeAndFileCountForSubTree;
        private readonly bool includeCurrentFolder;
        private readonly bool includeHiddenFolder;
        private readonly bool includeOwner;
        private readonly bool includeSubFolder;
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOwnerService ownerService;

        public DirectoryEnumerator(
            IFileManagementService fileManagementService,
            IDirectorySizeService directorySizeService,
            IOwnerService ownerService,
            IKernel32 kernel32,
            IKernel32FindFile kernel32FindFile,
            ILoggerFactory loggerFactory,
            bool includeSubFolder,
            bool includeHiddenFolder,
            bool includeCurrentFolder,
            bool getSizeAndFileCount,
            bool getSizeAndFileCountForSubTree,
            bool includeOwner)
        {
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.directorySizeService = directorySizeService ?? throw new ArgumentNullException(nameof(directorySizeService));
            this.ownerService = ownerService ?? throw new ArgumentNullException(nameof(ownerService));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            this.includeSubFolder = includeSubFolder;
            this.includeHiddenFolder = includeHiddenFolder;
            this.includeCurrentFolder = includeCurrentFolder;
            this.getSizeAndFileCount = getSizeAndFileCount;
            this.getSizeAndFileCountForSubTree = getSizeAndFileCountForSubTree;
            this.includeOwner = includeOwner;
        }

        public IEnumerable<IFolderReport> GetFolders(string path, Action progressCallback, CancellationToken cancellationToken)
        {
            return this.GetFolders(
                path,
                null,
                progressCallback,
                cancellationToken);
        }

        public IEnumerable<IFolderReport> GetFolders(string path, string userName, Action progressCallback, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            string ownerFilter = userName;
            var options = new FolderEnumeratorOptions(this.includeSubFolder, this.includeHiddenFolder, this.includeCurrentFolder, this.getSizeAndFileCount, this.getSizeAndFileCountForSubTree, this.includeOwner, ownerFilter);

            ILogger<DirectoryFolderEnumerator> enumeratorLogger = this.loggerFactory.CreateLogger<DirectoryFolderEnumerator>();
            var enumerator = new DirectoryFolderEnumerator(this.fileManagementService, this.directorySizeService, this.ownerService, this.kernel32, this.kernel32FindFile, enumeratorLogger);
            return enumerator.GetStructure(path, options, progressCallback, cancellationToken)
                .ToList();
        }
    }
}