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
    using System.Diagnostics;
    using Core;
    using Microsoft.Extensions.DependencyInjection;

    internal static class ModelBuilderHelper
    {
        public static void DebuggerLogResolveError(Type type, string s, Exception arg3)
        {
            Debugger.Log(0, null, $"Failed to resolve service instance of type {type} for parameter {s}.");
        }
    }

    public sealed class ModelBuilder<T1, TModel> where TModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        public ModelBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public TModel Build(T1 arg1)
        {
            return this.serviceProvider.GetRequiredService<TModel>(ModelBuilderHelper.DebuggerLogResolveError, arg1);
        }
    }

    public sealed class ModelBuilder<T1, T2, TModel> where TModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        public ModelBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public TModel Build(T1 arg1, T2 arg2)
        {
            return this.serviceProvider.GetRequiredService<TModel>(ModelBuilderHelper.DebuggerLogResolveError, arg1, arg2);
        }
    }

    public sealed class ModelBuilder<T1, T2, T3, TModel> where TModel : ViewModelBase
    {
        private readonly IServiceProvider serviceProvider;

        public ModelBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public TModel Build(T1 arg1, T2 arg2, T3 arg3)
        {
            return this.serviceProvider.GetRequiredService<TModel>(ModelBuilderHelper.DebuggerLogResolveError, arg1, arg2, arg3);
        }
    }

    public class ModelBuilder<TModel>
    {
        private readonly IServiceProvider serviceProvider;

        public ModelBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TModel Build()
        {
            return this.serviceProvider.GetRequiredService<TModel>();
        }
    }
}