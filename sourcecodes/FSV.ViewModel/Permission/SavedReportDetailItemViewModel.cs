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
    using Core;
    using Database.Models;

    public class SavedReportDetailItemViewModel
    {
        internal SavedReportDetailItemViewModel(PermissionReportDetail detail, bool encrypted)
        {
            this.Detail = detail;

            if (encrypted)
            {
                this.DecryptValues();
            }
            else
            {
                this.SetValues();
            }
        }

        internal PermissionReportDetail Detail { get; }

        [MapAtttribute("sAMAccountName")] public string AccountName { get; private set; }

        [MapAtttribute("givenName")] public string FirstName { get; private set; }

        [MapAtttribute("sn")] public string LastName { get; private set; }

        [MapAtttribute("mail")] public string Email { get; private set; }

        [MapAtttribute("department")] public string Department { get; set; }

        [MapAtttribute("division")] public string Division { get; private set; }

        [MapAtttribute("Domain")] public string Domain { get; private set; }

        [MapAtttribute("OriginatingGroup")] public string OriginatingGroup { get; private set; }

        [MapAtttribute("Rights")] public string Permissions { get; private set; }


        private void SetValues()
        {
            this.AccountName = this.Detail.AccountName;
            this.FirstName = this.Detail.FirstName;
            this.LastName = this.Detail.LastName;
            this.Email = this.Detail.Email;
            this.Department = this.Detail.Department;
            this.Division = this.Detail.Division;
            this.Domain = this.Detail.Domain;
            this.OriginatingGroup = this.Detail.OriginatingGroup;
            this.Permissions = this.Detail.Permissions;
        }

        private void DecryptValues()
        {
            this.AccountName = this.Detail.AccountName?.Decrypt();
            this.FirstName = this.Detail.FirstName?.Decrypt();
            this.LastName = this.Detail.LastName?.Decrypt();
            this.Email = this.Detail.Email?.Decrypt();
            this.Department = this.Detail.Department?.Decrypt();
            this.Division = this.Detail.Division?.Decrypt();
            this.Domain = this.Detail.Domain?.Decrypt();
            this.OriginatingGroup = this.Detail.OriginatingGroup?.Decrypt();
            this.Permissions = this.Detail.Permissions?.Decrypt();
        }
    }
}