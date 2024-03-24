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

    public class UserReportDetailListItemViewModel : IEquatable<UserReportDetailListItemViewModel>
    {
        public UserReportDetailListItemViewModel(UserPermissionReportDetail reportDetail, bool encrypted)
        {
            this.Detail = reportDetail ?? throw new ArgumentNullException(nameof(reportDetail));

            if (encrypted)
            {
                this.DecryptValues();
            }
            else
            {
                this.SetValues();
            }
        }

        internal UserPermissionReportDetail Detail { get; }

        public string Folder { get; private set; }
        public string CompleteName { get; private set; }
        public string OriginatingGroup { get; private set; }
        public string Permissions { get; private set; }
        public string Domain { get; private set; }

        public bool Equals(UserReportDetailListItemViewModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Folder == other.Folder && this.CompleteName == other.CompleteName && this.OriginatingGroup == other.OriginatingGroup && this.Permissions == other.Permissions && this.Domain == other.Domain;
        }

        private void SetValues()
        {
            this.Folder = this.Detail.SubFolder;
            this.CompleteName = this.Detail.CompleteName;
            this.OriginatingGroup = this.Detail.OriginatingGroup;
            this.Permissions = this.Detail.Permissions;
            this.Domain = this.Detail.Domain;
        }

        private void DecryptValues()
        {
            this.Folder = this.Detail.SubFolder.Decrypt();
            this.CompleteName = this.Detail.CompleteName.Decrypt();
            this.OriginatingGroup = this.Detail.OriginatingGroup.Decrypt();
            this.Permissions = this.Detail.Permissions.Decrypt();
            this.Domain = this.Detail.Domain.Decrypt();
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

            return this.Equals((UserReportDetailListItemViewModel)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Folder, this.CompleteName, this.OriginatingGroup, this.Permissions, this.Domain);
        }
    }
}