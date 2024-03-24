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

namespace FSV.ViewModel.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface IFlyOutService
    {
        /// <summary>
        ///     Gets if the content is last in history.
        /// </summary>
        bool LastInHistory { get; }

        /// <summary>
        ///     Raised when a flyout content is requested.
        /// </summary>
        event EventHandler<FlyoutViewModel> ContentAdded;

        event EventHandler FlyoutShown;

        /// <summary>
        ///     Shows a flyout.
        /// </summary>
        /// <typeparam name="T">Type of flyout content.</typeparam>
        /// <param name="parameterValues">A value or object to pass in T constructor.</param>
        void Show<T>(params object[] parameterValues) where T : FlyoutViewModel;

        Task ShowAsync<T>(params object[] parameterValues) where T : FlyoutViewModel;

        /// <summary>
        ///     Removes all flyouts.
        /// </summary>
        void RemoveAll();

        /// <summary>
        ///     Gets a previous flyout content from history.
        /// </summary>
        /// <returns>FSV.ViewModel.FlyoutViewModel.</returns>
        FlyoutViewModel GetPrevious();
    }
}