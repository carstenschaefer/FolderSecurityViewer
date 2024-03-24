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

namespace FSV.ViewModel.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using Abstractions;
    using FolderSecurityViewer;
    using Microsoft.Extensions.DependencyInjection;
    using Resources;

    public class DialogService : IDialogService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly object syncObject = new();
        private Window owner;

        public DialogService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IList<WorkspaceViewModel> ModalHistory { get; } = new List<WorkspaceViewModel>(2);

        public bool ShowDialog<T>() where T : WorkspaceViewModel
        {
            return this.ShowDialog(this.serviceProvider.GetRequiredService<T>());
        }

        public bool ShowDialog<T>(params object[] values) where T : WorkspaceViewModel
        {
            var workspace = this.serviceProvider.GetRequiredService<T>((type, message, exception) => Debugger.Log(0, null, message), values);
            return this.ShowDialog(workspace);
        }

        public bool ShowDialog(WorkspaceViewModel workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            lock (this.syncObject)
            {
                this.ModalHistory.Add(workspace);
            }

            void OnWorkspaceOnClosing(object s, CloseCommandEventArgs e)
            {
                lock (this.syncObject)
                {
                    if (s is WorkspaceViewModel model && this.ModalHistory.Contains(model))
                    {
                        model.Closing -= OnWorkspaceOnClosing;
                        this.ModalHistory.Remove(model);
                    }
                }
            }

            workspace.Closing += OnWorkspaceOnClosing;

            this.ShowDialog(workspace, out bool dialogResult);

            return dialogResult;
        }

        public void ShowMessage(string message)
        {
            this.ShowDialog(new MessageViewModel(message));
        }

        public void SetOwner(Window window)
        {
            this.owner = window ?? throw new ArgumentNullException(nameof(window));
        }

        public bool Ask(string message)
        {
            return this.ShowDialog(new MessageViewModel(message) { ShowCancel = true });
        }

        public bool AskRemove()
        {
            return this.ShowDialog(new MessageViewModel(CommonResource.AskRemove) { ShowCancel = true });
        }

        public void CloseAll()
        {
            for (int index = this.ModalHistory.Count - 1; index >= 0; index--)
            {
                WorkspaceViewModel model = this.ModalHistory[index];
                model.CancelCommand.Execute(null);
            }
        }

        public void Pop()
        {
            this.ModalHistory.LastOrDefault()?.CancelCommand.Execute(null);
        }


        private void ShowDialog(WorkspaceViewModel workspace, out bool dialogResult)
        {
            var window = new DialogWindow
            {
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                Title = workspace.DisplayName,
                DataContext = workspace,
                Owner = this.owner,
                UseLayoutRounding = true // Keep this, otherwise WPF adds weird border around window.
            };

            try
            {
                dialogResult = window.ShowDialog() ?? false;
            }
            catch (NullReferenceException)
            {
                dialogResult = false;
            }
        }
    }
}