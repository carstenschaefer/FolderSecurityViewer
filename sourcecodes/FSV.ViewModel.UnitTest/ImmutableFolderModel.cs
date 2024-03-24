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

namespace FSV.ViewModel.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class ImmutableFolderModel
    {
        public ImmutableFolderModel(
            IList<ImmutableFolderModel> items,
            string name,
            string path,
            bool selected,
            bool expanded,
            bool hasSubFolders,
            bool isUncPath)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            this.Items = items ?? throw new ArgumentNullException(nameof(items));
            this.Name = name;
            this.Path = path;
            this.HasSubFolders = hasSubFolders;
            this.IsUncPath = isUncPath;

            var directoryInfo = new DirectoryInfo(path);
            this.ParentPath = directoryInfo.Parent?.FullName;

            this.Selected = selected;
            this.Expanded = expanded;
        }

        public string ParentPath { get; }
        public bool Selected { get; }
        public bool Expanded { get; }
        public IList<ImmutableFolderModel> Items { get; }
        public string Name { get; }
        public string Path { get; }
        public bool HasSubFolders { get; }
        public bool IsUncPath { get; }
    }
}