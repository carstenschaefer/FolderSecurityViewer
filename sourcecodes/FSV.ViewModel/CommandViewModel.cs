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
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Core;

    public class CommandViewModel : NotifyChangeObject
    {
        private string _icon;
        private string _tip;

        internal CommandViewModel(ICommand command, string text, string icon, string tip = null)
        {
            this.Command = command ?? throw new ArgumentNullException(nameof(command));
            this.Text = text;
            this.Icon = icon;
            this.Tip = tip;
        }

        internal CommandViewModel(Func<object, Task> action, string text, string icon, string tip = null)
            : this(new AsyncRelayCommand(action), text, icon, tip)
        {
        }

        internal CommandViewModel(Action<object> action, string text, string icon, string tip = null)
            : this(new RelayCommand(action), text, icon, tip)
        {
        }

        public ICommand Command { get; }
        public string Text { get; set; }

        public string Icon
        {
            get => this._icon;
            set => this.Set(ref this._icon, value, nameof(this.Icon));
        }

        public string Tip
        {
            get => this._tip;
            set => this.Set(ref this._tip, value, nameof(this.Tip));
        }
    }
}