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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.Security;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices.Abstractions;
    using Configuration.Sections.ShareConfigXml;
    using Resources;
    using Services.Setting;

    public class ReportShareViewModel : ReportWorkspaceViewModel
    {
        private readonly IAdAuthentication _adAuthentication;
        private readonly ShareCredentials _credentials;
        private readonly ISettingShareService _shareService;
        private bool _changed;

        private SecureString _password;
        private string _userName;

        [DesignTimeCtor]
        public ReportShareViewModel()
        {
        }

        public ReportShareViewModel(ISettingShareService shareService, IAdAuthentication adAuthentication)
        {
            this.DisplayName = SettingReportShareResource.ShareReportCaption;

            this._shareService = shareService;
            this._adAuthentication = adAuthentication;

            this._credentials = this._shareService.GetCredentials();

            this._userName = this._credentials.UserName;
            this._password = this._credentials.GetPassword();
        }

        public string UserName
        {
            get => this._userName;
            set
            {
                this.Set(ref this._userName, value, nameof(this.UserName));
                this._changed = true;
            }
        }

        public void SetPassword(SecureString password)
        {
            if (password?.Length > 0)
            {
                this._password = password;
                password.MakeReadOnly();
                this._changed = true;
            }
        }

        public SecureString GetPassword()
        {
            return this._password;
        }

        public bool IsPasswordSet()
        {
            return this._password?.Length > 0;
        }

        internal async Task Save()
        {
            if (!this._changed)
            {
                return;
            }

            if (string.IsNullOrEmpty(this._userName) || this._password?.Length == 0)
                // Save empty.
            {
                await this._shareService.SaveAsync(string.Empty, null);
            }
            else if (!this.ValidateUserName())
            {
                throw new ApplicationException(SettingReportShareResource.UserNameValidationText);
            }
            else if (this._adAuthentication.ValidateUser(this.UserName, this._password))
            {
                await this._shareService.SaveAsync(this._userName, this._password);
            }
            else
            {
                throw new ApplicationException(SettingReportShareResource.InvalidCredentialsText);
            }

            this._changed = false;
        }

        private bool ValidateUserName()
        {
            string[] splits = this._userName?.Split('\\');
            return splits.Length == 2;
        }
    }
}