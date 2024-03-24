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
    using System.Linq;
    using Abstractions;
    using Core;
    using Core.Abstractions;
    using Microsoft.Extensions.Logging;
    using Types;

    public sealed class DirectoryFolderEnumerator : IDirectoryFolderEnumerator
    {
        private readonly IDirectorySizeService directorySizeService;
        private readonly IFileManagementService fileManagementService;
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;
        private readonly ILogger<DirectoryFolderEnumerator> logger;
        private readonly IOwnerService ownerService;

        public DirectoryFolderEnumerator(
            IFileManagementService fileManagementService,
            IDirectorySizeService directorySizeService,
            IOwnerService ownerService,
            IKernel32 kernel32,
            IKernel32FindFile kernel32FindFile,
            ILogger<DirectoryFolderEnumerator> logger)
        {
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.directorySizeService = directorySizeService ?? throw new ArgumentNullException(nameof(directorySizeService));
            this.ownerService = ownerService ?? throw new ArgumentNullException(nameof(ownerService));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<IFolderReport> GetStructure(
            LongPath directoryPath,
            FolderEnumeratorOptions options,
            Action incrementProgressCallback,
            Func<bool> isCancellationPending)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (incrementProgressCallback == null)
            {
                throw new ArgumentNullException(nameof(incrementProgressCallback));
            }

            if (isCancellationPending == null)
            {
                throw new ArgumentNullException(nameof(isCancellationPending));
            }

            var paths = new Stack<(int, FolderTraversalItem)>();
            paths.Push((0, new FolderTraversalItem(directoryPath, options)));

            while (paths.Any() && isCancellationPending() == false)
            {
                (int level, FolderTraversalItem item) = paths.Pop();

                string path = item.Path + @"\*";

                using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, path);
                while (fileEnumerator.MoveNext() && isCancellationPending() == false)
                {
                    Win32FindDataWrapper next = fileEnumerator.Current;
                    if (next.IsValid == false || next.IsAccessDenied())
                    {
                        string errorMessage = next.GetErrorMessage();
                        yield return new InaccessibleFolderReport(next.Path, errorMessage);
                        break;
                    }

                    Win32FindData findData = next.GetWin32FindData();
                    if (!findData.WorkOnFolder(item.Options.IncludeHiddenFolder, item.Options.IncludeCurrentFolder))
                    {
                        continue;
                    }

                    bool isCurrentFolder = findData.IsCurrentDirectory();
                    string currentItem = isCurrentFolder && item.Options.IncludeCurrentFolder ? findData.cFileName.Replace(".", string.Empty) : findData.cFileName;
                    string newPath = Path.Combine(item.Path, currentItem);

                    FolderReport folder = this.GetFolderReport(newPath, currentItem);
                    if (folder is null)
                    {
                        continue;
                    }

                    try
                    {
                        if (item.Options.GetSizeAndFileCount)
                        {
                            FolderSizeInfo fi = this.directorySizeService.AggregateSizeInfo(newPath, false, item.Options.IncludeHiddenFolder);
                            folder.Size = fi.Size;
                            folder.FileCount = fi.FileCount;
                        }

                        if (item.Options.GetSizeAndFileCountForSubTree)
                        {
                            FolderSizeInfo fi = this.directorySizeService.AggregateSizeInfo(newPath, true, item.Options.IncludeHiddenFolder);
                            folder.SizeInclSub = fi.Size;
                            folder.FileCountInclSub = fi.FileCount;
                        }
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e, "Failed to aggregate directory size information due to an error.");
                        folder.AddException(e);
                    }

                    var owner = string.Empty;

                    try
                    {
                        if (item.Options.IncludeOwner || !string.IsNullOrEmpty(item.Options.OwnerFilter))
                        {
                            owner = this.ownerService.GetNative(newPath);
                            folder.Owner = owner;
                        }
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e, "Failed to obtain directory owner information due to an error.");
                        owner = string.Empty;
                        folder.AddException(e);
                    }

                    if (!string.IsNullOrEmpty(item.Options.OwnerFilter)
                        && (owner.Equals(item.Options.OwnerFilter, StringComparison.OrdinalIgnoreCase)
                            || folder.Exception != null))
                    {
                        incrementProgressCallback?.Invoke();
                        yield return folder;
                    }
                    else if (string.IsNullOrEmpty(item.Options.OwnerFilter))
                    {
                        incrementProgressCallback?.Invoke();
                        yield return folder;
                    }

                    const int infiniteScanDepth = 0;
                    if (item.Options.Subtree && !isCurrentFolder && (options.ScanDepth == infiniteScanDepth || level < options.ScanDepth))
                    {
                        if (folder is InaccessibleFolderReport)
                        {
                            continue;
                        }

                        var subTreeFolderEnumeratorOptions = new FolderEnumeratorOptions(true, item.Options.IncludeHiddenFolder, false, item.Options.GetSizeAndFileCount, item.Options.GetSizeAndFileCountForSubTree, item.Options.IncludeOwner, item.Options.OwnerFilter);
                        var traversalItem = new FolderTraversalItem(newPath, subTreeFolderEnumeratorOptions);
                        paths.Push((level + 1, traversalItem));
                    }
                }
            }
        }

        private FolderReport GetFolderReport(LongPath path, string currentItem)
        {
            FolderReport folder = null;
            try
            {
                bool isAccessDenied = this.fileManagementService.IsAccessDenied(path);
                folder = isAccessDenied
                    ? new InaccessibleFolderReport(path, $"Access denied: {path}")
                    : new CompletedFolderReport(path, currentItem);

                if (isAccessDenied)
                {
                    throw new UnauthorizedAccessException($"Access denied to folder {path}.");
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to continue enumerating directory due to an error.");
                folder?.AddException(e);
            }

            return folder;
        }

        private class FolderTraversalItem
        {
            public FolderTraversalItem(LongPath path, FolderEnumeratorOptions options)
            {
                this.Path = path;
                this.Options = options ?? throw new ArgumentNullException(nameof(options));
            }

            public LongPath Path { get; }

            public FolderEnumeratorOptions Options { get; }

            public override string ToString()
            {
                return $"{this.Path}";
            }
        }
    }
}