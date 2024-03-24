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

namespace FSV.ViewModel.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class AsyncRelayCommand : IAsyncCommand, IRelayCommand
    {
        private readonly Func<object, Task<bool>> canExecute;
        private readonly Func<object, Task> execute;

        public AsyncRelayCommand(
            Func<object, Task> execute,
            Func<object, Task<bool>> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public AsyncRelayCommand(Func<object, Task> execute) : this(execute, p => Task.FromResult(true))
        {
        }

        public bool CanExecute(object parameter)
        {
            Task<bool> task = this.CanExecuteAsync(parameter);
            task.Wait();
            return task.Result;
        }

        public void Execute(object parameter)
        {
            this.ExecuteAsync(parameter).FireAndForgetSafeAsync();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public async Task ExecuteAsync(object parameter)
        {
            await this.execute(parameter);
            this.Executed?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> CanExecuteAsync(object parameter)
        {
            return await this.canExecute.Invoke(parameter);
        }

        public event EventHandler Executed;
    }
}