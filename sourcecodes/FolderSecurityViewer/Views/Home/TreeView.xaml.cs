namespace FolderSecurityViewer.Views.Home
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using FSV.ViewModel;
    using FSV.ViewModel.Home;

    /// <summary>
    ///     Interaction logic for TreeView.xaml
    /// </summary>
    [Obsolete("This class is deprecated in favor of FolderSecurityViewer.Views.Home.FolderTreeView.")]
    public partial class TreeView : UserControl
    {
        private TreeViewModel _dataContext;

        public TreeView()
        {
            this.InitializeComponent();

            this.Unloaded += this.TreeView_Unloaded;
            this.DataContextChanged += this.OnDataContextChanged;
        }

        private void TreeView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.DataContextChanged -= this.OnDataContextChanged;
            if (this._dataContext == null)
            {
                return;
            }

            this._dataContext.FolderTreeRefreshRequested -= this.OnFolderTreeRefreshRequested;
            this._dataContext.CollapseAllRequested -= this.OnCollapseAllRequested;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                // Before UserControl is unloaded, DataContext property is set to null. Remove event handlers from local _dataContext reference and return.
                this._dataContext.FolderTreeRefreshRequested -= this.OnFolderTreeRefreshRequested;
                this._dataContext.CollapseAllRequested -= this.OnCollapseAllRequested;

                this._dataContext = null;

                return;
            }

            this._dataContext = this.DataContext as TreeViewModel;

            if (this._dataContext != null && string.IsNullOrEmpty(this._dataContext.SelectedFolderPath))
            {
                if (this._dataContext.Standalone)
                {
                    this.InputBindings.Add(new KeyBinding(this._dataContext.OpenCommand, new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift))
                    {
                        CommandParameter = ReportType.Folder
                    });
                }

                this._dataContext.FolderTreeRefreshRequested += this.OnFolderTreeRefreshRequested;
                this._dataContext.CollapseAllRequested += this.OnCollapseAllRequested;
            }
        }

        private async void OnCollapseAllRequested(object sender, EventArgs e) => await this.FolderTree.CollapseAllAsync();

        private void OnFolderTreeRefreshRequested(object o_s, EventArgs o_e) => this.FolderTree.Refresh();
    }
}