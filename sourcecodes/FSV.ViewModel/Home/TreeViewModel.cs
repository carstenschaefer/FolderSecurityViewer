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
    using System.Windows;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Events;
    using Interop;
    using Prism.Events;
    using Resources;

    [Obsolete("This class is deprecated in favor of FSV.ViewModel.Home.FolderTreeViewModel.")]
    public class TreeViewModel : WorkspaceViewModel
    {
        private readonly IDialogService _dialogService;

        private readonly PubSubEvent<DirectoryTreeOpenRequestedData> _treeRequestedEvent;
        private ICommand _copyPathToClipboardCommand;
        private ICommand _openCommand;
        private ICommand _openConsoleCommand;
        private ICommand _openExplorerCommand;
        private ICommand _propertiesCommand;

        private ICommand _refreshCommand;

        private string _selectedFolderPath = string.Empty;
        private bool _standalone;

        public TreeViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            if (eventAggregator == null)
            {
                throw new ArgumentNullException(nameof(eventAggregator));
            }

            this.DisplayName = HomeResource.DirectoryCaption;

            this._dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this._treeRequestedEvent = eventAggregator.GetEvent<DirectoryTreeOpenRequested>();
        }

        public string SelectedFolderPath
        {
            get => this._selectedFolderPath;
            set => this.Set(ref this._selectedFolderPath, value, nameof(this.SelectedFolderPath));
        }

        public bool Standalone
        {
            get => this._standalone;
            internal set
            {
                this.Set(ref this._standalone, value, nameof(this.Standalone));
                this.DisplayName = this._standalone ? HomeResource.DirectoryTreeCaption : HomeResource.DirectoryCaption;
            }
        }

        public ICommand RefreshCommand => this._refreshCommand ?? (this._refreshCommand = new RelayCommand(this.Refresh));

        public ICommand OpenCommand => this._openCommand ?? (this._openCommand = new RelayCommand(this.Open, this.CanDo));

        public ICommand PropertiesCommand => this._propertiesCommand ?? (this._propertiesCommand = new RelayCommand(this.ShowPropertiesDialog));

        public ICommand OpenExplorerCommand => this._openExplorerCommand ?? (this._openExplorerCommand = new RelayCommand(this.ShowExplorerWindow));

        public ICommand OpenConsoleCommand => this._openConsoleCommand ?? (this._openConsoleCommand = new RelayCommand(this.ShowConsoleWindow));

        public ICommand CopyPathToClipboardCommand => this._copyPathToClipboardCommand ?? (this._copyPathToClipboardCommand = new RelayCommand(this.CopySelectedPathToClipboard));

        public event EventHandler FolderTreeRefreshRequested;
        public event EventHandler CollapseAllRequested;

        private void Open(object obj)
        {
            try
            {
                this.FolderTreeRefreshRequested?.Invoke(this, new EventArgs());
                this._treeRequestedEvent.Publish(new DirectoryTreeOpenRequestedData(obj == null ? ReportType.Permission : (ReportType)obj, this.SelectedFolderPath));
            }
            catch (Exception ex)
            {
                this._dialogService.ShowMessage(ex.Message);
            }
        }

        internal void Clear()
        {
            this.SelectedFolderPath = string.Empty;
            this.CollapseAllRequested?.Invoke(this, new EventArgs());
        }

        private void Refresh(object obj)
        {
            try
            {
                this.FolderTreeRefreshRequested?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                this._dialogService.ShowMessage(ex.Message);
            }
        }

        private bool CanDo(object p)
        {
            return !string.IsNullOrEmpty(this._selectedFolderPath);
        }

        private void ShowPropertiesDialog(object obj)
        {
            DirectoryCommands.ShowProperties(this._selectedFolderPath);
        }

        private void CopySelectedPathToClipboard(object obj)
        {
            Clipboard.Clear();
            Clipboard.SetText(this._selectedFolderPath);
        }

        private void ShowConsoleWindow(object obj)
        {
            DirectoryCommands.ShowInConsole(this._selectedFolderPath);
        }

        private void ShowExplorerWindow(object obj)
        {
            DirectoryCommands.ShowInExplorer(this._selectedFolderPath);
        }

        public class OpenEventArgs : EventArgs
        {
            public OpenEventArgs(ReportType reportType, string path)
            {
                this.ReportType = reportType;
                this.Path = path;
            }

            public ReportType ReportType { get; }
            public string Path { get; }
        }
    }
}