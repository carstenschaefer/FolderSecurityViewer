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

namespace FSV.FolderTree
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Interaction logic for FolderAccessTree
    /// </summary>
    [Obsolete]
    public partial class FolderAccessTree : UserControl
    {
        public static readonly DependencyProperty SelectedDrivePathProperty = DependencyProperty.Register("SelectedDrivePath", typeof(object), typeof(FolderAccessTree),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        ///     The folder worker
        /// </summary>
        private readonly FolderWorker folderWorker;

        private readonly ILogger log;

        /// <summary>
        ///     The path recorder
        /// </summary>
        private readonly Queue<string> pathRecorder = new();

        private readonly ConcurrentDictionary<string, TreeViewItem> treeNodeCollection = new();

        private TreeViewItem nodeToExpand;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderAccessTree" /> class.
        /// </summary>
        public FolderAccessTree(FolderWorker folderWorker, ILogger log)
        {
            this.folderWorker = folderWorker ?? throw new ArgumentNullException(nameof(folderWorker));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            this.InitializeComponent();

            this.folderWorker.ProgressChanged += this.OnFolderWorkerProgressChanged;
            this.folderWorker.RunWorkerCompleted += this.OnFolderWorkerRunWorkerCompleted;

            this.NaviLeftTree.SelectedItemChanged += this.OnSelectedItemChanged;
            this.NaviLeftTree.MouseDoubleClick += this.OnMouseDoubleClick;
            this.NaviLeftTree.KeyDown += this.OnKeyDown;

            this.NaviLeftTree.PreviewMouseDown += this.OnBeforeMouseDown;

            // Init Tree
            this.LoadTree();
        }

        /// <summary>
        ///     Gets or sets an item selected by user.
        /// </summary>
        /// <remarks>Currently a </remarks>
        public object SelectedDrivePath
        {
            get => this.GetValue(SelectedDrivePathProperty);
            set => this.SetValue(SelectedDrivePathProperty, value);
        }

        public object SelectedItem => this.NaviLeftTree.SelectedItem;

        public static RoutedCommand RemoveCommand => new();

        /// <summary>
        ///     Occurs when [node selected].
        /// </summary>
        public event EventHandler NodeSelected;

        public void LoadTree()
        {
            try
            {
                if (!this.folderWorker.IsBusy)
                {
                    this.NaviLeftTree.Items.Clear();

                    this.folderWorker.RunWorkerAsync(string.Empty);

                    this.NaviLeftTree.IsEnabled = false;

                    this.Progress.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to load the folder-access-tree due to an unhandled error.");
            }
        }

        /// <summary>
        ///     Loads the tree and highlight the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void LoadTree(string path)
        {
            try
            {
                this.pathRecorder.Clear();
                var isUnc = false;
                string root = Path.GetPathRoot(path);

                if (Uri.TryCreate(root, UriKind.RelativeOrAbsolute, out Uri rootUri))
                {
                    string[] pathComponents = path.Replace(root, string.Empty).Split('\\');
                    string newPath = root.Replace("\\", string.Empty);

                    if (rootUri.IsUnc)
                    {
                        newPath = root;
                        isUnc = true;
                    }

                    // handle already loaded roots
                    if (this.treeNodeCollection.ContainsKey(root.ToLower()))
                    {
                        if (rootUri.IsUnc)
                        {
                            if (path.ToLower().Equals(root.ToLower())) // Added in version 1.11.1
                            {
                                this.nodeToExpand = this.treeNodeCollection[root.ToLower()];
                                this.nodeToExpand.IsSelected = true;
                            }
                        }
                        else
                        {
                            // Following code is commented in version 1.11.1
                            this.nodeToExpand = this.treeNodeCollection[root.ToLower()];

                            this.nodeToExpand.Items.Clear();
                            this.nodeToExpand.IsExpanded = false;

                            foreach (string item in pathComponents)
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    newPath += "\\" + item;

                                    // add path component
                                    this.pathRecorder.Enqueue(newPath);
                                }
                            }

                            this.nodeToExpand.IsExpanded = true;
                        }
                    }
                    else
                    {
                        if (isUnc)
                        {
                            // handle new unc etc path
                            var folder = new FolderShim(root, Path.GetDirectoryName(root), true);

                            FolderModel folderModel = FolderModelBuilder.GetFolderItem(folder, string.Empty);
                            folderModel.Name = root;
                            folderModel.IsUncPath = true;
                            TreeViewItem uncTreeItem = this.CreateTreeViewItemFrom(folderModel);
                            // uncTreeItem.Items.Add(new TreeViewItem() { Header = "#dummy#" });
                            this.treeNodeCollection[root.ToLower()] = uncTreeItem;
                            this.NaviLeftTree.Items.Add(uncTreeItem);
                            this.nodeToExpand = uncTreeItem;
                            // this.nodeToExpand.IsSelected = true;

                            // lets go
                            this.nodeToExpand.Items.Clear();
                            this.nodeToExpand.IsExpanded = false;

                            foreach (string item in pathComponents)
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    newPath += "\\" + item;

                                    // add path component
                                    this.pathRecorder.Enqueue(newPath);
                                }
                            }

                            this.nodeToExpand.IsExpanded = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to load the folder-access-tree due to an unhandled error.");
            }
        }

        public void RemoveSelected()
        {
            if (this.NaviLeftTree.SelectedItem is TreeViewItem selectedItem)
            {
                this.NaviLeftTree.Items.Remove(selectedItem);

                // treeNodeCollection.TryRemove(selectedItem.Tag.ToString().ToLower(), out selectedItem);

                var folderModel = selectedItem.Tag as FolderModel;
                this.treeNodeCollection.TryRemove(folderModel?.Path.ToLower(), out selectedItem);

                this.nodeToExpand = null;
            }
        }

        /// <summary>
        ///     NavigationTree the left tree_ selected item changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.HandleActive(e.NewValue, true);
            this.HandleActive(e.OldValue, false);

            this.InvokeNodeSelected(e.NewValue, EventArgs.Empty);

            // Sets a path of folder selected from the tree.
            if (e.NewValue is TreeViewItem selectedTreeViewItem)
                // this.SelectedDrivePath = selectedTreeViewItem.Tag.ToString();
            {
                if (selectedTreeViewItem.Tag is FolderModel folderModel)
                {
                    this.SelectedDrivePath = folderModel.Path;
                }
            }
        }

        /// <summary>
        ///     Handles the MouseDoubleClick event of the Tree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="MouseButtonEventArgs" /> instance containing the event data.
        /// </param>
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.InvokeNodeSelected(sender, e);
        }

        /// <summary>
        ///     Invokes the node selected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void InvokeNodeSelected(object sender, EventArgs e)
        {
            this.NodeSelected?.Invoke(sender, e);
        }

        /// <summary>
        ///     Handles the KeyDown event of the NaviLeftTree control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="KeyEventArgs" /> instance containing the event data.
        /// </param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.InvokeNodeSelected(sender, e);
            }
        }

        /// <summary>
        ///     Handles the RunWorkerCompleted event of the folderWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs" /> instance containing the event data.
        /// </param>
        private async void OnFolderWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Delegate synchronizationDelegate = new Action(() =>
                {
                    this.NaviLeftTree.IsEnabled = true;

                    this.HideProgressBar();

                    if (this.nodeToExpand != null)
                    {
                        this.nodeToExpand.IsSelected = true;
                    }

                    this.NaviLeftTree.Focus();

                    if (this.pathRecorder.Any())
                    {
                        string nextPath = this.pathRecorder.Dequeue();

                        string itemKey = nextPath.ToLower();
                        if (this.treeNodeCollection.ContainsKey(itemKey))
                        {
                            TreeViewItem nextItem = this.treeNodeCollection[itemKey];
                            this.nodeToExpand = nextItem;
                            this.nodeToExpand.IsExpanded = true;

                            this.LoadSubFolders(nextItem);
                        }
                    }
                });

                await this.Dispatcher.BeginInvoke(synchronizationDelegate);
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "The {WorkerName} complete-event handler could not complete the synchronization due to an unhandled error.", nameof(FolderWorker));
            }
        }

        /// <summary>
        ///     Handles the ProgressChanged event of the folderWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="System.ComponentModel.ProgressChangedEventArgs" /> instance containing the event data.
        /// </param>
        private async void OnFolderWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.UserState is FolderModel folderModel)
                {
                    Delegate synchronizationDelegate = new Action(() => this.UpdateTreeView(folderModel));
                    await this.Dispatcher.BeginInvoke(synchronizationDelegate);
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "The {WorkerName} progress-changed handler could not properly update the operation progress information due to an unhandled error.", nameof(FolderWorker));
            }
        }

        /// <summary>
        ///     Updates the progress bar.
        /// </summary>
        /// <param name="e">The <see cref="ProgressChangedEventArgs" /> instance containing the event data.</param>
        private void UpdateProgressBar(ProgressChangedEventArgs e)
        {
            // Progress.Value = e.ProgressPercentage;
        }

        /// <summary>
        ///     Updates the tree view.
        /// </summary>
        /// <param name="folderModel">The folder model.</param>
        private void UpdateTreeView(FolderModel folderModel)
        {
            try
            {
                TreeViewItem newItem = this.CreateTreeViewItemFrom(folderModel);

                // add or update treeviewitem
                string path = folderModel.Path.ToLower();
                this.treeNodeCollection[path] = newItem;

                bool isDriveModel = string.IsNullOrEmpty(folderModel.ParentPath);

                if (isDriveModel)
                {
                    this.AddDriveItem(newItem);
                }
                else
                {
                    this.AddFolderItem(newItem);
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to update the tree-view due to an unhandled error.");
            }
        }

        private void AddFolderItem(TreeViewItem newItem)
        {
            this.nodeToExpand.Items.Add(newItem);
        }

        private void AddDriveItem(TreeViewItem newItem)
        {
            this.NaviLeftTree.Items.Add(newItem);
        }

        /// <summary>
        ///     Handles the Expanded event of the newNode control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void OnNewNodeExpanded(object sender, RoutedEventArgs e)
        {
            this.LoadSubFolders((TreeViewItem)e.Source);
        }

        /// <summary>
        ///     Handles the active node icon.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="active">
        ///     if set to <c>true</c> [active].
        /// </param>
        private void HandleActive(object item, bool active)
        {
            if (item != null)
            {
                var tvi = (TreeViewItem)item;
                var header = (CustomTreeViewHeader)tvi.Header;

                if (active)
                {
                    header.SetActive();
                }
                else
                {
                    header.SetInActive();
                }
            }
        }

        /// <summary>
        ///     Loads the sub folders.
        /// </summary>
        /// <param name="treeViewItem">The treeViewItem.</param>
        private void LoadSubFolders(TreeViewItem treeViewItem)
        {
            try
            {
                if (!this.folderWorker.IsBusy)
                {
                    this.nodeToExpand = treeViewItem;

                    if (this.nodeToExpand.Items.Count < 2)
                    {
                        this.nodeToExpand.Items.Clear();
                        this.NaviLeftTree.IsEnabled = false;

                        this.ShowProgressBar();

                        var folderModel = this.nodeToExpand.Tag as FolderModel;

                        this.folderWorker.RunWorkerAsync(folderModel?.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to load sub-folders due to an unhandled error.");
            }
        }

        private void ShowProgressBar()
        {
            this.Progress.IsEnabled = true;
            this.Progress.Visibility = Visibility.Visible;
        }

        /// <summary>
        ///     Hides the progress bar.
        /// </summary>
        private void HideProgressBar()
        {
            this.Progress.IsEnabled = false;
            this.Progress.Visibility = Visibility.Collapsed;
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
                        item.Items.Add("#dummyNode#");
                        item.Expanded += this.OnNewNodeExpanded;
                    }
                }

                // item.Tag = folderModel.Path;
                item.Tag = folderModel;
                item.Header = new CustomTreeViewHeader(folderModel.Name, folderModel.Image, folderModel.HasSubFolders);
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to create a new tree-view item due to an unhandled error.");
            }

            return item;
        }

        /// <summary>
        ///     Handles ContextMenuOpening event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                this.InvokeNodeSelected(this, e);
            }

            base.OnContextMenuOpening(e);
        }

        /// <summary>
        ///     Handles PreviewMouseDown event of TreeView control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeforeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                InputBinding binding = this.InputBindings.OfType<InputBinding>().FirstOrDefault(m => m is MouseBinding);
                if (binding != null)
                {
                    binding.Command?.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeViewItem;
        }
    }

    public class FolderShim : IFolder
    {
        public FolderShim(string fullName, string name, bool hasSubFolders)
        {
            this.FullName = fullName;
            this.Name = name;
            this.HasSubFolders = hasSubFolders;
        }

        public string FullName { get; }
        public string Name { get; }
        public bool HasSubFolders { get; set; }
        public bool AccessDenied { get; set; }
    }
}