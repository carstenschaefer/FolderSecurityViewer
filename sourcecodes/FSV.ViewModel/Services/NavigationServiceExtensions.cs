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
    using System.Threading.Tasks;
    using Abstractions;
    using Core;

    public static class NavigationServiceExtensions
    {
        public static void AddFor<T>(this INavigationService service, Action<T> func) where T : ViewModelBase
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            AddFor<T>(service, async modelBase =>
            {
                func(modelBase);
                await Task.CompletedTask;
            });
        }

        public static void AddFor<T>(this INavigationService service, Func<T, Task> func) where T : ViewModelBase
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            service.AddFor(typeof(T), async modelBase => await func((T)modelBase));
        }
    }
}