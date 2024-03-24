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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;

    public class PaginationViewModel : ViewModelBase
    {
        private static readonly object PageChangeEvent = new();
        private readonly IConfigurationManager configurationManager;

        private readonly EventHandlerList events = new();
        private int _currentPage;
        private int _pageSize;
        private string _showText;
        private int _totalPages;
        private int _totalRows;
        private bool disposed;

        private Func<PageChangeMode, Task> pageChangeAsyncCallback;

        internal PaginationViewModel(
            IConfigurationManager configurationManager,
            Func<PageChangeMode, Task> pageChangeAsyncCallback) : this(configurationManager, pageChangeAsyncCallback, "Rows")
        {
        }

        internal PaginationViewModel(
            IConfigurationManager configurationManager,
            Func<PageChangeMode, Task> pageChangeAsyncCallback, string showText)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.ShowText = showText;
            this.ChangePageCommand = new RelayCommand(this.ChangePage);

            this.InitPageSizes();
            this.InitOnPageChange(pageChangeAsyncCallback);
        }

        public int CurrentPage
        {
            get => this._currentPage;
            internal set
            {
                this._currentPage = value;
                this.RaisePropertyChanged(nameof(this.CurrentPage));
            }
        }

        public int TotalPages
        {
            get => this._totalPages;
            set
            {
                this._totalPages = value;
                this.RaisePropertyChanged(nameof(this.TotalPages));

                if (this._totalPages == 0)
                {
                    this.CurrentPage = 0;
                }
            }
        }

        public string ShowText
        {
            get => this._showText;
            set
            {
                this._showText = value;
                this.RaisePropertyChanged(nameof(this.ShowText));
            }
        }

        public ICommand ChangePageCommand { get; }

        public Dictionary<string, int> PageSizes { get; private set; }

        public int PageSize
        {
            get => this._pageSize;
            set
            {
                this._pageSize = value;
                this.RaisePropertyChanged(nameof(this.PageSize));

                this.MakeTotalPages(); // Recalculates the number of pages according to page size.
                this.ChangePage(PageChangeMode.First, true);
            }
        }

        public int TotalRows
        {
            get => this._totalRows;
            internal set
            {
                this._totalRows = value;
                this.RaisePropertyChanged(nameof(this.TotalRows));

                this.MakeTotalPages();
                if (this._totalRows > 0)
                {
                    this.CurrentPage = 1;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && this.disposed == false)
            {
                this.events.Dispose();
                this.disposed = true;
            }
        }

        public event EventHandler<PageChangeEventArgs> PageChange
        {
            add => this.events.AddHandler(PageChangeEvent, value);
            remove => this.events.RemoveHandler(PageChangeEvent, value);
        }

        private void ChangePage(object mode)
        {
            this.ChangePage(mode, false);
        }

        private void ChangePage(object mode, bool fromPageSize) // fromPageSize makes sure that page change is invoked when page size is changed.
        {
            PageChangeMode changeMode = mode is PageChangeMode pageChangeMode ? pageChangeMode : PageChangeMode.First;

            if (this.TotalPages >= 1)
            {
                var didPageChange = false;

                if (changeMode == PageChangeMode.First)
                {
                    didPageChange = this.CurrentPage != 1;
                    this.CurrentPage = 1;
                }
                else if (changeMode == PageChangeMode.Previous) // Move previous
                {
                    if (this.CurrentPage > 1)
                    {
                        didPageChange = true;
                        this.CurrentPage = this.CurrentPage - 1;
                    }
                }
                else if (changeMode == PageChangeMode.Next) // Move next
                {
                    if (this.CurrentPage < this.TotalPages)
                    {
                        didPageChange = true;
                        this.CurrentPage = this.CurrentPage + 1;
                    }
                }
                else if (changeMode == PageChangeMode.Last)
                {
                    didPageChange = this.CurrentPage != this.TotalPages;
                    this.CurrentPage = this.TotalPages;
                }
                else // Move First
                {
                    this.CurrentPage = 1;
                }

                if (fromPageSize || didPageChange)
                {
                    this.InvokePageChangeEvent(changeMode);
                }
            }
            else
            {
                this.CurrentPage = 0;
            }
        }

        private void InvokePageChangeEvent(PageChangeMode changeMode)
        {
            var handler = this.events[PageChangeEvent] as EventHandler<PageChangeEventArgs>;
            handler?.Invoke(this, new PageChangeEventArgs(changeMode));
        }

        private void MakeTotalPages()
        {
            if (this._totalRows == 0)
            {
                this.TotalPages = 0;
                this.CurrentPage = 0;
                return;
            }

            if (this.PageSize == 0) // For All option in page size list.
            {
                this.TotalPages = 1;
                this.CurrentPage = 1;
                return;
            }

            int totalPages = this.TotalRows / this.PageSize;
            if (this.TotalRows % this.PageSize > 0)
            {
                totalPages++;
            }

            this.TotalPages = totalPages;
        }

        private void InitPageSizes()
        {
            const int defaultPageSize = 20;
            this.PageSizes = new Dictionary<string, int> { { "20", defaultPageSize }, { "30", 30 }, { "40", 40 }, { "50", 50 }, { "70", 70 }, { "100", 100 }, { "All", 0 } };

            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            int configPageSize = configRoot?.PageSize ?? defaultPageSize;

            if (!this.PageSizes.ContainsValue(configPageSize))
            {
                this.PageSizes.Add(configPageSize.ToString(), configPageSize);
            }


            this.PageSize = configPageSize;
        }

        private void InitOnPageChange(Func<PageChangeMode, Task> onPageChange)
        {
            this.pageChangeAsyncCallback = onPageChange;

            if (this.pageChangeAsyncCallback != null)
            {
                this.PageChange += async (s, e) => await this.pageChangeAsyncCallback(e.Mode);
            }
        }
    }
}