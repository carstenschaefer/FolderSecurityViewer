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
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Core;

    public abstract class WorkspaceViewModel : ViewModelBase
    {
        private static readonly object ClosingEvent = new();
        private readonly EventHandlerList events = new();
        private ICommand cancelCommand;

        private bool disposed;
        private bool featureAvailable = true;

        private bool isWorking, isClosing;
        private ICommand okCommand;

        protected WorkspaceViewModel()
        {
            this.ExportDate = DateTime.Now;
        }

        public bool IsWorking
        {
            get => this.isWorking;
            private set => this.Set(ref this.isWorking, value, nameof(this.IsWorking));
        }

        public bool FeatureAvailable
        {
            get => this.featureAvailable;
            protected set => this.Set(ref this.featureAvailable, value, nameof(this.FeatureAvailable));
        }

        public DateTime ExportDate { get; protected set; }

        public HeaderViewModel Header { get; protected set; }

        public ICommand OKCommand => this.okCommand ??= new RelayCommand(p => this.DoClose(true), this.CanOk);
        public ICommand CancelCommand => this.cancelCommand ??= new RelayCommand(p => this.DoClose(false), this.CanCancel);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !this.disposed)
            {
                this.events.Dispose();
                this.disposed = true;
            }
        }

        public event EventHandler<CloseCommandEventArgs> Closing
        {
            add => this.events.AddHandler(ClosingEvent, value);
            remove => this.events.RemoveHandler(ClosingEvent, value);
        }

        private void DoClose(bool isOk)
        {
            if (!this.isClosing)
            {
                this.isClosing = true;
                this.OnClosing(new CloseCommandEventArgs(isOk));
                this.isClosing = false;
                this.Dispose();
            }
        }

        /// <summary>
        ///     Fires the <see cref="Closing" /> event.
        /// </summary>
        /// <param name="e">An <see cref="CloseCommandEventArgs" /> object representing event data.</param>
        protected virtual void OnClosing(CloseCommandEventArgs e)
        {
            if (this.events[ClosingEvent] is EventHandler<CloseCommandEventArgs> handler)
            {
                handler(this, e);
            }
        }

        protected virtual bool CanOk(object p)
        {
            return true;
        }

        protected virtual bool CanCancel(object p)
        {
            return true;
        }

        internal void DoProgress()
        {
            this.IsWorking = true;
        }

        internal void StopProgress()
        {
            this.IsWorking = false;
        }

        public virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }
    }
}