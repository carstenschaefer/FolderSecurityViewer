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

namespace FolderSecurityViewer
{
    using System.ComponentModel;
    using System.Windows;
    using Controls;
    using FSV.ViewModel;

    /// <summary>
    ///     Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : CustomWindow
    {
        public DialogWindow()
        {
            this.InitializeComponent();

            this.Loaded += this.WindowLoaded;
            this.Unloaded += this.WindowUnloaded;
        }

        public bool CloseInitiated { get; private set; }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is WorkspaceViewModel viewModel)
            {
                viewModel.Closing -= this.ViewModelClosing;
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is WorkspaceViewModel viewModel)
            {
                viewModel.Closing += this.ViewModelClosing;
            }
        }

        private void ViewModelClosing(object sender, CloseCommandEventArgs e)
        {
            if (!this.CloseInitiated)
            {
                this.DialogResult = e.IsOK;
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.CloseInitiated = true;
            base.OnClosing(e);
        }
    }
}