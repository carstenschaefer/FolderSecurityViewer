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

namespace FSV.ViewModel.Home
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Core;
    using FolderTree;

    public sealed class FolderTreeItemViewModel : ViewModelBase, IEquatable<FolderTreeItemViewModel>
    {
        private readonly FolderModel folderModel;

        private bool _isExpanded;
        private bool _isSelected;

        public FolderTreeItemViewModel(FolderModel folderModel)
        {
            this.folderModel = folderModel ?? throw new ArgumentNullException(nameof(folderModel));

            this.DisplayName = this.folderModel.Name;

            this.Path = this.folderModel.Path;
            this.HasItems = this.folderModel.HasSubFolders;
            this.IsUncPath = this.folderModel.IsUncPath;
            this.IsRoot = string.IsNullOrEmpty(this.folderModel.ParentPath);
            this.Image = this.GetImage();

            this.Items = new ObservableCollection<FolderTreeItemViewModel>();
        }

        public bool Selected
        {
            get => this._isSelected;
            set => this.Set(ref this._isSelected, value, nameof(this.Selected));
        }

        public bool Expanded
        {
            get => this._isExpanded;
            set => this.Set(ref this._isExpanded, value, nameof(this.Expanded));
        }

        public string Path { get; }
        public bool HasItems { get; }
        public bool IsUncPath { get; }
        public bool IsRoot { get; }
        public IList<FolderTreeItemViewModel> Items { get; }

        public string Image { get; }

        /// <summary>
        ///     Gets whether current folder has sub-folders and they haven't been filled yet.
        /// </summary>
        public bool Empty => this.HasItems && this.Items.Count == 0;

        public bool Equals(FolderTreeItemViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Path.Equals(other.Path, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FolderTreeItemViewModel);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Path);
        }

        private string GetImage()
        {
            var hasChildren = string.Empty;

            if (!this.HasItems)
            {
                hasChildren = "nochilds";
            }

            string image = this.folderModel.Image switch
            {
                EnumTreeNodeImage.Drive => "drive.png",
                EnumTreeNodeImage.Folder => hasChildren + "folder.png",
                EnumTreeNodeImage.DriveNotReady => "driveNotReady.png",
                EnumTreeNodeImage.AccessDenied => "accessDenied.png",
                _ => "drive.png"
            };

            return $"/FSV.FolderTree;component/Images/WinRT/{image}";
        }
    }
}