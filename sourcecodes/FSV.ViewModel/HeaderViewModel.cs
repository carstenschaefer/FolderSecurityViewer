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
    using System.Windows.Input;
    using Core;

    public class HeaderViewModel : ViewModelBase
    {
        private bool _useRefreshCache;
        private bool hasError;
        private string internalSearchText;
        private bool isWorking;

        private ICommand searchCommand;
        private bool searchDisabled;
        private string searchText;
        private bool showCancel;

        private string text;

        public HeaderViewModel() : this(null, null)
        {
        }

        internal HeaderViewModel(ICommand cancelCommand) : this(null, cancelCommand)
        {
        }

        internal HeaderViewModel(PaginationViewModel pagination) : this(pagination, null)
        {
        }

        internal HeaderViewModel(PaginationViewModel pagination, ICommand cancelCommand)
        {
            this.Pagination = pagination;
            this.CancelCommand = cancelCommand;

            this.InitPagination();
            this.InitCancelCommand();
        }

        public ICommand CancelCommand { get; }

        public ICommand SearchCommand
        {
            get => this.searchCommand;
            set
            {
                this.searchCommand = value;

                if (this.searchCommand != null && this.searchCommand is IRelayCommand command)
                {
                    command.Executed += (sender, e) => { this.SearchTriggered(); };
                }
            }
        }

        public ICommand SearchClearCommand => new RelayCommand(this.ClearSearch, p => !string.IsNullOrEmpty(this.SearchText));

        public ICommand RefreshCommand { get; set; }

        public PaginationViewModel Pagination { get; }

        /// <summary>
        ///     Gets or sets boolean value indicating Refresh Cache menu option is used with Refresh Command.
        /// </summary>
        public bool UseRefreshCache
        {
            get => this._useRefreshCache;
            internal set => this.Set(ref this._useRefreshCache, value, nameof(this.UseRefreshCache));
        }

        public bool ShowCancel
        {
            get => this.showCancel;
            set
            {
                if (this.showCancel == value)
                {
                    return;
                }

                this.showCancel = value;
                this.RaisePropertyChanged(nameof(this.ShowCancel));
            }
        }

        public bool IsWorking
        {
            get => this.isWorking;
            private set
            {
                if (value == this.isWorking)
                {
                    return;
                }

                this.isWorking = value;
                this.RaisePropertyChanged(nameof(this.IsWorking));
            }
        }

        public bool SearchDisabled
        {
            get => this.searchDisabled;
            internal set
            {
                if (value == this.searchDisabled)
                {
                    return;
                }

                this.searchDisabled = value;
                this.RaisePropertyChanged(nameof(this.SearchDisabled));
            }
        }

        public string SearchText
        {
            get => this.searchText;
            set
            {
                this.searchText = value;
                this.RaisePropertyChanged(nameof(this.SearchText));
            }
        }

        public string Text
        {
            get => this.text;
            private set
            {
                this.text = value;
                this.RaisePropertyChanged(nameof(this.Text));
            }
        }

        public bool HasError
        {
            get => this.hasError;
            private set
            {
                if (value == this.hasError)
                {
                    return;
                }

                this.hasError = value;
                this.RaisePropertyChanged(nameof(this.HasError));
            }
        }

        public void ShowProgress()
        {
            this.IsWorking = true;
        }

        public void EndProgress()
        {
            this.IsWorking = false;
        }

        public void SetText(string text, bool hasError = false)
        {
            this.Text = text;
            this.HasError = hasError;
        }

        private void ClearSearch(object p)
        {
            this.SearchTriggered();
            this.SearchText = string.Empty;
            this.SearchCommand?.Execute(null);
        }

        private void SearchTriggered()
        {
            if (this.Pagination != null)
            {
                this.Pagination.CurrentPage = 1;
            }

            this.internalSearchText = this.SearchText;
        }

        private void InitPagination()
        {
            if (this.Pagination != null)
            {
                this.Pagination.PageChange += this.OnPageChange;
            }
        }

        private void InitCancelCommand()
        {
            if (this.CancelCommand != null)
            {
                this.showCancel = true;
            }

            if (this.CancelCommand is IRelayCommand relayCommand)
            {
                relayCommand.Executed += (sender, e) => { this.SearchDisabled = true; };
            }
        }

        private void OnPageChange(object sender, PageChangeEventArgs e)
        {
            this.SearchText = this.internalSearchText;
        }
    }
}