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

namespace FSV.ViewModel.Services.Home
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using ViewModel.Home;

    internal sealed class FolderTreeItemSelector : IFolderTreeItemSelector
    {
        private readonly Func<IEnumerable<FolderTreeItemViewModel>> _foldersFactory;

        private int _cycleEndsAt;
        private int _cycleStartsFrom;

        private IList<FolderTreeItemViewModel> _folders;
        private int _foldersCount;
        private int _lastFoundIndex = -1;
        private string _previousText = string.Empty;

        public FolderTreeItemSelector(Func<IEnumerable<FolderTreeItemViewModel>> foldersFactory)
        {
            this._foldersFactory = foldersFactory ?? throw new ArgumentNullException(nameof(foldersFactory));

            this.Reset();
        }

        public async Task<FolderTreeItemViewModel> GetNextAsync(string text, FolderTreeItemViewModel selectedFolder)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));
            }

            if (selectedFolder is null)
            {
                throw new ArgumentNullException(nameof(selectedFolder));
            }

            if (!this._previousText.Equals(text, StringComparison.InvariantCultureIgnoreCase))
            {
                this._lastFoundIndex = -1;
                this._previousText = text;
            }

            try
            {
                return await Task.Run(() => this.InternalGetNext(text, selectedFolder));
            }
            catch (Exception ex)
            {
                throw new FolderTreeItemSelectorException("Failed to fetch next folder item from list of folder due to an error. See inner exception for more details.", ex);
            }
        }

        public void Reset()
        {
            this._folders = this.GetFlatList();
            this._foldersCount = this._folders.Count;
            this._lastFoundIndex = -1;
        }

        private FolderTreeItemViewModel InternalGetNext(string text, FolderTreeItemViewModel selectedFolder)
        {
            if (!this._folders.Any())
            {
                return selectedFolder;
            }

            int index = this.GetIndex(selectedFolder);
            while (true)
            {
                FolderTreeItemViewModel folderItem = this._folders.ElementAt(index);
                if (folderItem.DisplayName.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    this._lastFoundIndex = index;
                    return folderItem;
                }

                if (index == this._cycleEndsAt && this._lastFoundIndex == -1)
                {
                    return null;
                }

                if (index == this._foldersCount - 1)
                {
                    index = 0;
                    continue;
                }

                index++;
            }
        }

        private int GetIndex(FolderTreeItemViewModel selectedFolder)
        {
            int selectedIndex = this._folders.IndexOf(selectedFolder);

            this._cycleStartsFrom = selectedIndex == this._foldersCount - 1 ? 0 : selectedIndex + 1;
            this._cycleEndsAt = this._cycleStartsFrom == 0 ? this._foldersCount - 1 : this._cycleStartsFrom - 1;

            if (this._lastFoundIndex == -1)
            {
                return this._cycleStartsFrom;
            }

            if (this._lastFoundIndex == this._foldersCount - 1)
            {
                return 0;
            }

            return this._lastFoundIndex + 1;
        }

        private IList<FolderTreeItemViewModel> GetFlatList()
        {
            static void FillFlatList(IEnumerable<FolderTreeItemViewModel> items, IList<FolderTreeItemViewModel> targetList)
            {
                foreach (FolderTreeItemViewModel item in items)
                {
                    targetList.Add(item);
                    if (item.HasItems && item.Expanded)
                    {
                        FillFlatList(item.Items, targetList);
                    }
                }
            }

            List<FolderTreeItemViewModel> targetList = new();
            IEnumerable<FolderTreeItemViewModel> items = this._foldersFactory.Invoke();
            if (items is null)
            {
                return targetList;
            }

            FillFlatList(items, targetList);

            return targetList;
        }
    }
}