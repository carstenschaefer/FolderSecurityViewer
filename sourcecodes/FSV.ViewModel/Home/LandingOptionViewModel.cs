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
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Core;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class LandingOptionViewModel
    {
        public LandingOptionViewModel(string name, string icon, Action<object> showAction) : this(name, icon, null, showAction)
        {
        }

        public LandingOptionViewModel(string name, string icon, string text, Action<object> showAction)
        {
            this.Name = name;
            this.Icon = icon;
            this.Text = text;
            this.ShowCommand = new RelayCommand(showAction);
        }

        public LandingOptionViewModel(string name, string icon, string text, Func<object, Task> func)
        {
            this.Name = name;
            this.Icon = icon;
            this.Text = text;
            this.ShowCommand = new AsyncRelayCommand(func);
        }

        public string Icon { get; }

        public string Name { get; }
        public string Text { get; }
        public ICommand ShowCommand { get; }
    }
}