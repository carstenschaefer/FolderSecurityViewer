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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Events;
    using FileSystem.Interop;
    using FolderTree;
    using Interop;
    using Microsoft.Extensions.Logging;
    using Prism.Events;
    using Resources;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class FolderTreeViewModel : WorkspaceViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly IDispatcherService _dispatcherService;
        private readonly FolderWorker _folderWorker;
        private readonly PubSubEvent<DirectoryTreeOpenRequestedData> _treeRequestedEvent;
        private readonly IFileManagement fileManagement;
        private readonly IFolderTreeItemSelector folderTreeItemSelector;

        private readonly ObservableCollection<FolderTreeItemViewModel> items;
        private readonly ILogger<FolderTreeViewModel> logger;
        private readonly object syncObject = new();

        private ICommand _copyPathToClipboardCommand;
        private ICommand _openCommand;
        private ICommand _openConsoleCommand;
        private ICommand _openExplorerCommand;
        private ICommand _propertiesCommand;

        private ICommand _refreshCommand;
        private ICommand _removeUncShareCommand;
        private ICommand _reportCommand;

        private FolderTreeItemViewModel _selectedFolder;

        private string _selectedPath;

        private string[] _selectionParts;
        private bool _standalone;

        public FolderTreeViewModel(
            FolderWorker folderWorker,
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            IFileManagement fileManagement,
            ILogger<FolderTreeViewModel> logger,
            Func<Func<IEnumerable<FolderTreeItemViewModel>>, IFolderTreeItemSelector> folderTreeItemSelectorFactory)
        {
            this._folderWorker = folderWorker ?? throw new ArgumentNullException(nameof(folderWorker));
            this._dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this._dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.fileManagement = fileManagement ?? throw new ArgumentNullException(nameof(fileManagement));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (folderTreeItemSelectorFactory is null)
            {
                throw new ArgumentNullException(nameof(folderTreeItemSelectorFactory));
            }

            this._treeRequestedEvent = eventAggregator.GetEvent<DirectoryTreeOpenRequested>();

            this._folderWorker.ProgressChanged += this.OnFolderWorkerProgressChanged;
            this._folderWorker.RunWorkerCompleted += this.OnFolderWorkerRunCompleted;

            this.DisplayName = HomeResource.DirectoryCaption;

            this.items = new ObservableCollection<FolderTreeItemViewModel>();

            IEnumerable<FolderTreeItemViewModel> GetItems()
            {
                return this.items;
            }

            this.folderTreeItemSelector = folderTreeItemSelectorFactory.Invoke(GetItems);
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

        public string SelectedPath
        {
            get => this._selectedPath;
            set => this.Set(ref this._selectedPath, value, nameof(this.SelectedPath));
        }

        public bool Refreshing { get; private set; }

        public FolderTreeItemViewModel SelectedFolder
        {
            get => this._selectedFolder;
            set
            {
                this.Set(ref this._selectedFolder, value, nameof(this.SelectedFolder));
                if (this._selectedFolder != null && !this.Refreshing)
                {
                    this.SelectedPath = this._selectedFolder.Path;
                }
            }
        }

        public IEnumerable<FolderTreeItemViewModel> Items
        {
            get
            {
                lock (this.syncObject)
                {
                    return this.items;
                }
            }
        }

        public ICommand RefreshCommand => this._refreshCommand ??= new AsyncRelayCommand(this.RefreshAsync);

        public ICommand OpenCommand => this._openCommand ??= new AsyncRelayCommand(this.OpenAsync, this.CanOpenOrStartReport);
        public ICommand ReportCommand => this._reportCommand ??= new AsyncRelayCommand(this.StartReportAsync, this.CanOpenOrStartReport);

        public ICommand PropertiesCommand => this._propertiesCommand ??= new RelayCommand(this.ShowPropertiesDialog);

        public ICommand OpenExplorerCommand => this._openExplorerCommand ??= new RelayCommand(this.ShowExplorerWindow);

        public ICommand OpenConsoleCommand => this._openConsoleCommand ??= new RelayCommand(this.ShowConsoleWindow);

        public ICommand CopyPathToClipboardCommand => this._copyPathToClipboardCommand ??= new RelayCommand(this.CopySelectedPathToClipboard);

        public ICommand RemoveUncShareCommand => this._removeUncShareCommand ??= new RelayCommand(this.RemoveUncShare, this.CanRemoveUncShare);

        public void Expand()
        {
            if (this.SelectedFolder.Empty)
            {
                this.LoadSubFolders(this.SelectedFolder);
            }
            else
            {
                this.folderTreeItemSelector.Reset();
            }
        }

        public void Collapse()
        {
            this.folderTreeItemSelector.Reset();
        }

        public void Clear()
        {
            this.SelectedFolder = null;
            this.SelectedPath = null;

            this.ClearFolderItems();
        }

        public async Task SelectFolderNextAsync(string typingText)
        {
            if (string.IsNullOrEmpty(typingText))
            {
                throw new ArgumentException($"'{nameof(typingText)}' cannot be null or empty.", nameof(typingText));
            }

            try
            {
                FolderTreeItemViewModel foundItem = await this.folderTreeItemSelector.GetNextAsync(typingText, this._selectedFolder);

                if (foundItem is null)
                {
                    return;
                }

                await this._dispatcherService.InvokeAsync(() => foundItem.Selected = true);
            }
            catch (FolderTreeItemSelectorException ex)
            {
                this.logger.LogError(ex, "Failed to synchronize worker-results with the tree-view due to an unhandled error.");
            }
        }

        public override Task InitializeAsync()
        {
            this.RefreshAndLoadTree();
            return Task.CompletedTask;
        }


        private async void OnFolderWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.UserState is FolderModel folderModel)
                {
                    await this._dispatcherService.InvokeAsync(() => this.UpdateTreeView(folderModel));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to synchronize worker-results with the tree-view due to an unhandled error.");
            }
        }

        private async void OnFolderWorkerRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                var synchronizationDelegate = new Action(async () =>
                {
                    this.StopProgress();

                    if (this.Refreshing)
                    {
                        this.Refreshing = false;
                        await this.InternalRefreshAsync();

                        return;
                    }

                    if (this.SelectedFolder != null)
                    {
                        string pathPart = this.GetNextPathPart();
                        if (pathPart != null)
                        {
                            this.StartExpansion(pathPart, this.SelectedFolder.Items);
                        }
                        else
                        {
                            this.folderTreeItemSelector.Reset();
                        }
                    }
                });

                await this._dispatcherService.InvokeAsync(synchronizationDelegate);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to synchronize worker results due to an unhandled error.");
            }
        }

        private void LoadTree()
        {
            try
            {
                this.ClearFolderItems();

                if (!this._folderWorker.IsBusy)
                {
                    this.DoProgress();
                    this._folderWorker.RunWorkerAsync(string.Empty);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load the tree due to an unhandled error.");
            }
        }

        private void LoadSubFolders(FolderTreeItemViewModel node)
        {
            try
            {
                if (!this._folderWorker.IsBusy && node.Empty)
                {
                    this.DoProgress();
                    this._folderWorker.RunWorkerAsync(node.Path);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load sub-folders due to an unhandled error.");
            }
        }

        private void UpdateTreeView(FolderModel folderModel)
        {
            try
            {
                bool isDriveModel = string.IsNullOrEmpty(folderModel.ParentPath);
                var item = new FolderTreeItemViewModel(folderModel);

                if (isDriveModel)
                {
                    this.items.Add(item);
                }
                else
                {
                    this.SelectedFolder.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to update the tree-view due to an unhandled error.");
            }
        }

        private bool StartExpansion(string pathPart, IList<FolderTreeItemViewModel> items)
        {
            var found = false;
            foreach (FolderTreeItemViewModel item in items)
            {
                if (!(item is FolderTreeItemViewModel folderModel))
                {
                    return false;
                }

                if (folderModel.DisplayName.ToLower().Equals(pathPart.ToLower()))
                {
                    found = true;
                    if (!folderModel.HasItems)
                    {
                        folderModel.Selected = true;
                        this.StopProgress();
                    }
                    else if (folderModel.Empty)
                    {
                        folderModel.Expanded = true; // Triggers Expanded event when LoadSubFolders is invoked.
                    }
                    else
                    {
                        folderModel.Expanded = true;
                        string nextPathPart = this.GetNextPathPart();
                        if (nextPathPart == null)
                        {
                            this.StopProgress();
                            folderModel.Selected = true;
                            return found;
                        }

                        if (this.StartExpansion(nextPathPart, folderModel.Items))
                        {
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                this.StopProgress();
            }

            return found;
        }

        private async Task InternalRefreshAsync()
        {
            if (string.IsNullOrEmpty(this.SelectedPath))
            {
                return;
            }

            this.SelectedFolder = null;
            this._selectionParts = this.fileManagement.GetPathParts(this.SelectedPath);

            await this._dispatcherService.InvokeAsync(() =>
            {
                this.DoProgress();
                this.StartExpansion(this.GetNextPathPart(), this.items);
            });
        }

        private string GetNextPathPart()
        {
            if (this._selectionParts == null)
            {
                return null;
            }

            string part = this._selectionParts.FirstOrDefault();
            this._selectionParts = this._selectionParts.Skip(1).ToArray();
            return part;
        }

        private async Task OpenAsync(object obj)
        {
            // Add path root in Folder worker's container if SelectedPath is UNC path.
            // This is correct place to add path because SelectedPath is cleared once DirectoryTreeOpenRequested is complete.
            AddUncResultStatus status = await this.AddUncPathToWorkerAsync(this.SelectedPath);

            // Now the Unc path is added in container. If the status is:
            switch (status)
            {
                case AddUncResultStatus.NoUnc: // SelectedPath is not UNC path. Start the permission report.
                case AddUncResultStatus.Available: // Path is already available in container. Start the report.
                    this._treeRequestedEvent.Publish(new DirectoryTreeOpenRequestedData(ReportType.Permission, this.SelectedPath));
                    break;
                case AddUncResultStatus.Added:
                    // Path is added successfully. Just refresh, and don't start report.
                    this.RefreshAndLoadTree();
                    break;
            }
        }

        private async Task StartReportAsync(object obj)
        {
            this._treeRequestedEvent.Publish(new DirectoryTreeOpenRequestedData((ReportType?)obj ?? ReportType.Permission, this.SelectedPath));
            await Task.CompletedTask;
        }

        private async Task RefreshAsync(object obj)
        {
            if (!string.IsNullOrEmpty(this.SelectedPath))
            {
                // Add path root in Folder worker's container if SelectedPath is UNC path.
                AddUncResultStatus pathAdded = await this.AddUncPathToWorkerAsync(this.SelectedPath);
                if (pathAdded == AddUncResultStatus.Error)
                {
                    return;
                }
            }

            this.RefreshAndLoadTree();
        }

        private async Task<bool> CanOpenOrStartReport(object p)
        {
            return await Task.FromResult(!string.IsNullOrEmpty(this.SelectedPath));
        }

        private void ShowPropertiesDialog(object obj)
        {
            DirectoryCommands.ShowProperties(this.SelectedPath);
        }

        private void CopySelectedPathToClipboard(object obj)
        {
            Clipboard.Clear();
            Clipboard.SetText(this.SelectedPath);
        }

        private void ShowConsoleWindow(object obj)
        {
            DirectoryCommands.ShowInConsole(this.SelectedPath);
        }

        private void ShowExplorerWindow(object obj)
        {
            DirectoryCommands.ShowInExplorer(this.SelectedPath);
        }

        private void RemoveUncShare(object __)
        {
            if (this._folderWorker.RemoveUncPath(this.SelectedPath))
            {
                lock (this.syncObject)
                {
                    FolderTreeItemViewModel root = this.items.FirstOrDefault(m => this.SelectedPath.ToLower() == m.Path.ToLower());
                    if (root != null)
                    {
                        this.items.Remove(root);
                    }
                }
            }
        }

        private bool CanRemoveUncShare(object __)
        {
            return this.SelectedFolder != null && this.SelectedFolder.IsUncPath && this.SelectedFolder.IsRoot;
        }

        /// <summary>
        ///     Adds unc path, if exists, in FolderWorker.
        /// </summary>
        /// <param name="path">Path to add in Unc list.</param>
        /// <returns>System.Threading.Tasks.Task</returns>
        /// <remarks>
        ///     Calling AddUncPath makes sure that given UncPath exists, however it may take a little time to check.
        ///     Async will make sure that window doesn't get into Not Responding state.
        /// </remarks>
        private async Task<AddUncResultStatus> AddUncPathToWorkerAsync(string path)
        {
            try
            {
                this.DoProgress();
                bool? status = await Task.Run(() => this._folderWorker.AddUncPath(path));

                return status switch
                {
                    true => AddUncResultStatus.Added,
                    false => AddUncResultStatus.Available,
                    _ => AddUncResultStatus.NoUnc
                };
            }
            catch (DirectoryNotFoundException ex)
            {
                string message = string.Format(ErrorResource.DirectoryNotExist, path);
                this.logger.LogError(ex, message);
                await this._dispatcherService.InvokeAsync(() => this._dialogService.ShowMessage(message));
                return AddUncResultStatus.Error;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to add the given UNC-path {Path} to the worker due to an unhandled error.", path);
                await this._dispatcherService.InvokeAsync(() => this._dialogService.ShowMessage($"Failed to add the given UNC-path {path} to the worker due to an unhandled error."));
                return AddUncResultStatus.Error;
            }
            finally
            {
                this.StopProgress();
            }
        }

        private void RefreshAndLoadTree()
        {
            this.Refreshing = true;
            this.LoadTree();
        }

        private void ClearFolderItems()
        {
            lock (this.syncObject)
            {
                this.items.Clear();
            }
        }

        private enum AddUncResultStatus
        {
            NoUnc,
            Added,
            Available,
            Error
        }
    }
}