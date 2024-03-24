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

namespace FSV.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Input;
    using Core;
    using Resources;

    public class MessageViewModel : WorkspaceViewModel
    {
        private string _cancelText = CommonResource.CancelButtonCaption;
        private string _okText = CommonResource.OKButtonCaption;
        private bool _showCancel;

        public MessageViewModel(string message)
        {
            this.DisplayName = HomeResource.MessageCaption;

            string[] lines = message?.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            this.Messages = lines?.Select(m => m.TrimStart()).ToArray();
        }

        public string[] Messages { get; }

        public bool ShowCancel
        {
            get => this._showCancel;
            set => this.Set(ref this._showCancel, value, nameof(this.ShowCancel));
        }

        public string OkText
        {
            get => this._okText;
            set => this.Set(ref this._okText, value, nameof(this.OkText));
        }

        public string CancelText
        {
            get => this._cancelText;
            set => this.Set(ref this._cancelText, value, nameof(this.CancelText));
        }

        public ICommand CloseCommand { get; } = new RelayCommand(o => { Debugger.Log(0, null, "Close command invoked."); });
    }
}