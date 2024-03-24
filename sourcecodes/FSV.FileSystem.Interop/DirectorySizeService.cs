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

    public sealed class DirectorySizeService : IDirectorySizeService
    {
        private const long MaxDword = 0xffffffff;
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;

        public DirectorySizeService(IKernel32 kernel32, IKernel32FindFile kernel32FindFile)
        {
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
        }

        public FolderSizeInfo AggregateSizeInfo(LongPath directoryPath, bool includeSubtrees, bool includeHiddenFolders)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            var stack = new Stack<AggregateSizeInfoTraversalItem>();
            stack.Push(new AggregateSizeInfoTraversalItem(directoryPath, includeSubtrees, includeHiddenFolders));

            var fileCount = 0L;
            var size = 0D;

            while (stack.Any())
            {
                AggregateSizeInfoTraversalItem traversalItem = stack.Pop();

                using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, $@"{traversalItem.Path}\*");
                while (fileEnumerator.MoveNext())
                {
                    Win32FindDataWrapper next = fileEnumerator.Current;
                    if (next == null || next.IsValid == false || next.IsAccessDenied())
                    {
                        break;
                    }

                    Win32FindData findData = next.GetWin32FindData();
                    if (findData.IsDirectory())
                    {
                        if (findData.IsSystem() || findData.IsTemporary() || (!includeHiddenFolders && findData.IsHidden()))
                        {
                            continue;
                        }

                        if (findData.IsCurrentDirectory() || findData.IsParentDirectory())
                        {
                            continue;
                        }

                        if (!traversalItem.SubTree)
                        {
                            continue;
                        }

                        string subTreePath = Path.Combine(traversalItem.Path, findData.cFileName);
                        var item = new AggregateSizeInfoTraversalItem(subTreePath, true, traversalItem.IncludeHiddenFolders);
                        stack.Push(item);
                    }
                    else
                    {
                        fileCount += 1;
                        size += findData.nFileSizeHigh * (MaxDword + 1) + findData.nFileSizeLow;
                    }
                }
            }

            return new FolderSizeInfo(fileCount, size);
        }

        private class AggregateSizeInfoTraversalItem
        {
            public AggregateSizeInfoTraversalItem(LongPath path, bool subTree, bool includeHiddenFolders)
            {
                this.Path = path ?? throw new ArgumentNullException(nameof(path));
                this.SubTree = subTree;
                this.IncludeHiddenFolders = includeHiddenFolders;
            }

            public LongPath Path { get; }
            public bool SubTree { get; }
            public bool IncludeHiddenFolders { get; }
        }
    }
}