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
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Abstractions;
    using Core;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class FlyOutService : IFlyOutService
    {
        private readonly IServiceProvider serviceProvider;
        private FlyoutViewModel _current;

        public FlyOutService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private ConcurrentStack<FlyoutViewModel> FlyoutHistory { get; } = new();

        public event EventHandler<FlyoutViewModel> ContentAdded;
        public event EventHandler FlyoutShown;

        public bool LastInHistory => this.FlyoutHistory.Count == 0;

        public void Show<T>(params object[] parameterValues) where T : FlyoutViewModel
        {
            if (this._current != null)
            {
                this.FlyoutHistory.Push(this._current);
            }
            else
            {
                this.FlyoutShown?.Invoke(this, new EventArgs());
            }

            this._current = this.serviceProvider.GetRequiredService<T>((type, s, arg3) => { }, parameterValues);
            this.ContentAdded?.Invoke(this, this._current);
            this._current.InitializeAsync().FireAndForgetSafeAsync();
        }

        public async Task ShowAsync<T>(params object[] parameterValues) where T : FlyoutViewModel
        {
            if (this._current != null)
            {
                this.FlyoutHistory.Push(this._current);
            }
            else if (this.FlyoutShown != null)
            {
                this.FlyoutShown.Invoke(this, new EventArgs());
            }

            this._current = this.serviceProvider.GetRequiredService<T>((type, s, arg3) => { }, parameterValues);
            this.ContentAdded?.Invoke(this, this._current);
            await this._current.InitializeAsync();
        }

        public void RemoveAll()
        {
            this._current = null;
            this.FlyoutHistory.Clear();
        }

        public FlyoutViewModel GetPrevious()
        {
            if (this.FlyoutHistory.TryPop(out this._current))
            {
                return this._current;
            }

            return null;
        }
    }
}