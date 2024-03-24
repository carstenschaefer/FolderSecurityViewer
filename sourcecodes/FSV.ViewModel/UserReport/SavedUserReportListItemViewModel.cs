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

namespace FSV.ViewModel.UserReport
{
    using System;
    using Core;
    using Database.Models;

    /// <summary>
    ///     Represents a save report as a list item.
    /// </summary>
    public class SavedUserReportListItemViewModel : ViewModelBase, IEquatable<SavedUserReportListItemViewModel>
    {
        private bool _isSelected;

        public SavedUserReportListItemViewModel(UserPermissionReport report, Action<SavedUserReportListItemViewModel> onUpdate)
        {
            this.Report = report ?? throw new ArgumentNullException(nameof(report));
            this.OnUpdate = onUpdate;

            this.Id = report.Id;
            this.FolderPath = report.Folder;
            this.UserName = report.ReportUser;
        }

        private Action<SavedUserReportListItemViewModel> OnUpdate { get; }

        internal UserPermissionReport Report { get; }

        public bool IsSelected
        {
            get => this._isSelected;
            set => this.Set(ref this._isSelected, value, nameof(this.IsSelected));
        }

        public int Id { get; }

        public string FolderPath { get; }
        public string UserName { get; }

        public string Folder => this.Report.Folder;

        public string ReportUser => this.Report.ReportUser;

        public string Date => this.Report.Date.ToString("g");

        public string Description
        {
            get => this.Report.Description;
            set
            {
                if (this.Report.Description != value)
                {
                    this.Report.Description = value;
                    this.RaisePropertyChanged(() => this.Description);

                    this.UpdateDescription();
                }
            }
        }

        public string User => this.Report.User;

        public bool Equals(SavedUserReportListItemViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Compare(this.FolderPath, other.FolderPath, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                   string.Compare(this.UserName, other.UserName, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private void UpdateDescription()
        {
            this.OnUpdate?.Invoke(this);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((SavedUserReportListItemViewModel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.Id;
                hashCode = (hashCode * 397) ^ (this.FolderPath != null ? this.FolderPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.UserName != null ? this.UserName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}