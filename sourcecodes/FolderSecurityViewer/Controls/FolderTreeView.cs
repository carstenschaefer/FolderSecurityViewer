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

namespace FolderSecurityViewer.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using FSV.FileSystem.Interop;
    using FSV.FolderTree;
    using Microsoft.Extensions.Logging;

    [Obsolete]
    public class FolderTreeView : TreeView
    {
        public static readonly DependencyProperty SelectedPathProperty = DependencyProperty.Register(nameof(SelectedPath), typeof(string), typeof(FolderTreeView),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private static readonly string Loading = "Loading";

        private readonly FolderWorker _folderWorker;
        private readonly IFileManagement fileManagement;
        private readonly ILogger<FolderTreeView> logger;
        private ProgressBar _progress;

        private bool _refreshInitiated;

        private string[] _selectionParts;

        public FolderTreeView(FolderWorker folderWorker, IFileManagement fileManagement, ILogger<FolderTreeView> logger)
        {
            this._folderWorker = folderWorker ?? throw new ArgumentNullException(nameof(folderWorker));
            this.fileManagement = fileManagement ?? throw new ArgumentNullException(nameof(fileManagement));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Loaded += this.OnLoaded;
            this.SelectedItemChanged += this.OnSelectedItemChanged;

            this._folderWorker.ProgressChanged += this.OnFolderWorkerProgressChanged;
            this._folderWorker.RunWorkerCompleted += this.OnFolderWorkerRunWorkerCompleted;
        }

        public TreeViewItem SelectedNode { get; set; }

        /// <summary>
        ///     Gets or sets an item selected by user.
        /// </summary>
        public string SelectedPath
        {
            get => (string)this.GetValue(SelectedPathProperty);
            set => this.SetValue(SelectedPathProperty, value);
        }

        public void Refresh()
        {
            this._refreshInitiated = true;
            this.LoadTree();
        }

        public async Task CollapseAllAsync()
        {
            await this.Dispatcher.InvokeAsync(() => this.InternalCollapseAll(this.Items));
        }

        private void InternalCollapseAll(ItemCollection items)
        {
            var stack = new Stack<TreeViewItem>();

            static void PushItems(Stack<TreeViewItem> target, ItemCollection items)
            {
                foreach (TreeViewItem item in items.OfType<TreeViewItem>())
                {
                    target.Push(item);
                }
            }

            while (stack.Any())
            {
                TreeViewItem next = stack.Pop();
                if (next.IsExpanded)
                {
                    next.IsExpanded = false;
                    PushItems(stack, next.Items);
                }
            }
        }

        private bool StartExpansion(string pathPart, ItemCollection items)
        {
            var found = false;
            foreach (object item in items)
            {
                if (!(item is TreeViewItem node) || !(node.Tag is FolderModel folderModel))
                {
                    return false;
                }

                if (folderModel.Name.ToLower().Equals(pathPart.ToLower()))
                {
                    found = true;
                    if (!node.HasItems)
                    {
                        node.IsSelected = true;
                        this.HideProgressBar();
                    }
                    else if (node.Items[0] as string == Loading)
                    {
                        node.IsExpanded = true; // Triggers Expanded event when LoadSubFolders is invoked.
                    }
                    else
                    {
                        node.IsExpanded = true;
                        string nextPathPart = this.GetNextPathPart();
                        if (nextPathPart == null)
                        {
                            this.HideProgressBar();
                            node.IsSelected = true;
                            return found;
                        }

                        if (this.StartExpansion(nextPathPart, node.Items))
                        {
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                this.HideProgressBar();
            }

            return found;
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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._progress = this.GetTemplateChild("PART_Progress") as ProgressBar;
            this.LoadTree();
        }

        /// <summary>
        ///     NavigationTree the left tree_ selected item changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Sets a path of folder selected from the tree.
            if (e.NewValue is TreeViewItem selectedTreeViewItem && selectedTreeViewItem.Tag is FolderModel folderModel)
            {
                this.SelectedPath = folderModel.Path;
            }
        }

        private async void OnFolderWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                var synchronizationDelegate = new Func<Task>(async () =>
                {
                    this.IsEnabled = true;
                    this.HideProgressBar();

                    if (this._refreshInitiated)
                    {
                        this._refreshInitiated = false;
                        await this.InternalRefreshAsync();
                        return;
                    }

                    if (this.SelectedNode != null)
                    {
                        this.SelectedNode.Items.RemoveAt(0);
                        string pathPart = this.GetNextPathPart();
                        if (pathPart != null)
                        {
                            this.StartExpansion(pathPart, this.SelectedNode.Items);
                        }
                    }
                });

                await this.Dispatcher.InvokeAsync(synchronizationDelegate);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to synchronize folder worker results.");
            }
        }

        private void OnFolderWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.UserState is FolderModel folderModel)
                {
                    this.Dispatcher.InvokeAsync(() => this.UpdateTreeView(folderModel));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to handle the folder-worker progress-change event due to an unhandled error.");
            }
        }

        private void LoadTree()
        {
            try
            {
                this.Items.Clear();

                if (!this._folderWorker.IsBusy)
                {
                    this._folderWorker.RunWorkerAsync(string.Empty);
                    this.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load the tree-view due to an unhandled error.");
            }
        }

        private void UpdateTreeView(FolderModel folderModel)
        {
            try
            {
                TreeViewItem newItem = this.CreateTreeViewItemFrom(folderModel);

                bool isDriveModel = string.IsNullOrEmpty(folderModel.ParentPath);

                if (isDriveModel)
                {
                    this.Items.Add(newItem);
                }
                else
                {
                    this.SelectedNode.Items.Add(newItem);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to update the tree-view due to an unhandled error.");
            }
        }

        /// <summary>
        ///     Gets the new node.
        /// </summary>
        /// <param name="folderModel">The folder model.</param>
        /// <returns>
        ///     a new treeItem for the given model
        /// </returns>
        private TreeViewItem CreateTreeViewItemFrom(FolderModel folderModel)
        {
            var item = new TreeViewItem();

            try
            {
                if (!folderModel.AccessDenied)
                {
                    if (folderModel.HasSubFolders)
                    {
                        item.Items.Add(Loading);
                        if (string.IsNullOrEmpty(folderModel.ParentPath))
                            // Due to RoutingStrategy as Bubbling, the event handler on root treeview item is also propagated to children items on invariable depth.
                            // So add handler only for root treeviewitem.
                        {
                            item.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(this.FolderExpanded));
                        }
                    }
                }

                item.Tag = folderModel;
                item.Header = new CustomTreeViewHeader(folderModel.Name, folderModel.Image, folderModel.HasSubFolders);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to a new tree-view item due to an unhandled error.");
            }

            return item;
        }

        private void FolderExpanded(object sender, RoutedEventArgs e)
        {
            this.LoadSubFolders(e.Source as TreeViewItem);
        }

        private void LoadSubFolders(TreeViewItem node)
        {
            try
            {
                this.SelectedNode = node;
                node.IsSelected = true;
                if (!this._folderWorker.IsBusy && node.Items[0] as string == Loading)
                {
                    // node.Items.Clear();
                    this.IsEnabled = false;

                    this.ShowProgressBar();

                    var model = node.Tag as FolderModel;
                    this._folderWorker.RunWorkerAsync(model?.Path);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to load sub-folders due to an unhandled error.");
            }
        }

        private void ShowProgressBar()
        {
            this._progress.IsEnabled = true;
            this._progress.Visibility = Visibility.Visible;
        }

        /// <summary>
        ///     Hides the progress bar.
        /// </summary>
        private void HideProgressBar()
        {
            this._progress.IsEnabled = false;
            this._progress.Visibility = Visibility.Hidden;
        }

        private async Task InternalRefreshAsync()
        {
            if (string.IsNullOrEmpty(this.SelectedPath))
            {
                return;
            }

            this.SelectedNode = null;
            this._selectionParts = this.fileManagement.GetPathParts(this.SelectedPath);

            await this.Dispatcher.InvokeAsync(() =>
            {
                this.ShowProgressBar();
                this.StartExpansion(this.GetNextPathPart(), this.Items);
            });
        }
    }
}