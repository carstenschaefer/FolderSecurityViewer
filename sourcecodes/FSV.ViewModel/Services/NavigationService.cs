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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Abstractions;
    using Core;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class NavigationService : INavigationService, IDisposable
    {
        private static readonly object NavigateEvent = new();
        private readonly IDictionary<string, List<Func<ViewModelBase, Task>>> dictionary;
        private readonly EventHandlerList events = new();
        private readonly IServiceProvider serviceProvider;
        private readonly object syncObject = new();

        public NavigationService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.dictionary = new Dictionary<string, List<Func<ViewModelBase, Task>>>();
        }

        public void Dispose()
        {
            this.events?.Dispose();
        }

        public async Task NavigateWithAsync<T>(T instance) where T : ViewModelBase
        {
            await this.Raise(instance);
        }

        public async Task NavigateWithAsync<T>() where T : ViewModelBase
        {
            var instance = this.serviceProvider.GetRequiredService<T>();
            await this.Raise(instance);
        }

        public async Task NavigateWithAsync<T>(object parameter) where T : ViewModelBase
        {
            Type parameterType = parameter.GetType();

            if (parameterType.Namespace == null) // Anonymous type
            {
                PropertyInfo[] properties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                ResolverOverride[] overridesArray = properties
                    .Select(p => new ResolverOverride(p.Name, p.GetValue(parameter, null)))
                    .ToArray();

                var instance = this.serviceProvider.GetRequiredService<T>(overridesArray);
                await this.Raise(instance);
            }
            else
            {
                var paramOverrides = new[] { new ResolverOverride(parameterType, null, parameter) };
                var instance = this.serviceProvider.GetRequiredService<T>(paramOverrides);
                await this.Raise(instance);
            }
        }

        public async Task NavigateWithAsync<T>(params object[] parameters) where T : ViewModelBase
        {
            ResolverOverride[] resolvedServices = parameters.Select((x, i) => new ResolverOverride(x.GetType(), null, x)).ToArray();
            var instance = this.serviceProvider.GetRequiredService<T>(resolvedServices);
            await this.Raise(instance);
        }

        public void AddFor(Type type, Func<ViewModelBase, Task> action)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (this.syncObject)
            {
                string typeName = type.FullName;
                if (this.dictionary.TryGetValue(typeName, out List<Func<ViewModelBase, Task>> list) == false)
                {
                    list = new List<Func<ViewModelBase, Task>>();
                    this.dictionary[typeName] = list;
                }

                list.Add(action);
            }
        }

        public event EventHandler<NavigationEventArgs> Navigate
        {
            add => this.events.AddHandler(NavigateEvent, value);
            remove => this.events.RemoveHandler(NavigateEvent, value);
        }

        private async Task Raise<T>(T instance) where T : ViewModelBase
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            IEnumerable<Func<ViewModelBase, Task>> GetActions()
            {
                lock (this.syncObject)
                {
                    string typeName = typeof(T).FullName;
                    if (this.dictionary.TryGetValue(typeName!, out List<Func<ViewModelBase, Task>> list))
                    {
                        return new List<Func<ViewModelBase, Task>>(list);
                    }
                }

                return Enumerable.Empty<Func<ViewModelBase, Task>>();
            }

            IEnumerable<Func<ViewModelBase, Task>> actions = GetActions();
            foreach (Func<ViewModelBase, Task> action in actions)
            {
                await action.Invoke(instance);
            }

            this.InvokeNavigateEvent(instance);
        }

        private void InvokeNavigateEvent<T>(T instance) where T : ViewModelBase
        {
            var e = new NavigationEventArgs(instance);
            this.OnNavigate(e);
        }

        private void OnNavigate(NavigationEventArgs e)
        {
            var handler = this.events[NavigateEvent] as EventHandler<NavigationEventArgs>;
            handler?.Invoke(this, e);
        }
    }
}