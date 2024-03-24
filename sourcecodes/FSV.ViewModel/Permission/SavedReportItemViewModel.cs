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
    using Core;
    using Database.Models;

    /// <summary>
    ///     Represents an item in list of saved reports.
    /// </summary>
    public class SavedReportItemViewModel : ViewModelBase
    {
        private readonly Action<SavedReportItemViewModel> onDescriptionUpdate;

        private bool selected;

        public SavedReportItemViewModel(PermissionReport report, Action<SavedReportItemViewModel> onDescriptionUpdate)
        {
            this.Report = report ?? throw new ArgumentNullException(nameof(report));
            this.onDescriptionUpdate = onDescriptionUpdate;
        }

        public bool IsSelected
        {
            get => this.selected;
            set => this.Set(ref this.selected, value, nameof(this.IsSelected));
        }

        public string SelectedFolderPath => this.Report.Folder;

        public string User => this.Report.User;

        public string Description
        {
            get => this.Report.Description;
            set
            {
                if (this.Report.Description != value)
                {
                    this.Report.Description = value;
                    this.RaisePropertyChanged(nameof(this.Description));

                    this.UpdateDescription();
                }
            }
        }

        public string Date => this.Report.Date.ToString("g");

        public bool Encrypted
        {
            get => this.Report.Encrypted;
            set => this.Report.Encrypted = value;
        }

        public PermissionReport Report { get; }

        private void UpdateDescription()
        {
            this.onDescriptionUpdate?.Invoke(this);
        }
    }
}