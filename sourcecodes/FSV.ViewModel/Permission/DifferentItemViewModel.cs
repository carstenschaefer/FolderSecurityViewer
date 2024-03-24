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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Data;
    using System.Windows.Input;
    using Core;

    public class DifferentItemViewModel : ViewModelBase
    {
        private int? _exportCount;
        private string _path;

        private DifferentItemState _state;

        public DifferentItemViewModel(string path, ICommand openInTabCommand)
        {
            this.Path = path;
            this.OpenInTabCommand = openInTabCommand ?? throw new ArgumentNullException(nameof(openInTabCommand));
        }

        public string Path
        {
            get => this._path;
            set => this.Set(ref this._path, value, nameof(this.Path));
        }

        public DifferentItemState State
        {
            get => this._state;
            internal set => this.Set(ref this._state, value, nameof(this.State));
        }

        public int? ExportCount
        {
            get => this._exportCount;
            internal set => this.Set(ref this._exportCount, value, nameof(this.ExportCount));
        }

        public DataTable ExportItems { get; set; }

        public ICommand OpenInTabCommand { get; }
    }
}