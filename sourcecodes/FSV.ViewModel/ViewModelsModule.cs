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
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using Core;
    using Extensions.DependencyInjection;
    using FSV.Templates.Abstractions;
    using Home;
    using Managers;
    using Microsoft.Extensions.DependencyInjection;
    using Permission;
    using Templates;
    using UserReport;

    /// <summary>
    ///     An <see cref="IModule" /> that registers all <see cref="ViewModelBase" /> types with the IOC.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ViewModelsModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<Template>();
            services.AddTransient<TemplateManager>();
            services.AddSingleton<GridMetadataModel>();

            IEnumerable<Type> nonAbstractModelTypes = GetViewModelTypes();
            foreach (Type modelType in nonAbstractModelTypes)
            {
                services.AddTransient(modelType);
            }

            // Info: the following view-model types may also be registered as transient services (needs further investigation)
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<SplashViewModel>();
            services.AddSingleton<LandingViewModel>();
            services.AddSingleton<ReportContainerViewModel>();
            services.AddSingleton<TemplateContainerViewModel>();
            services.AddSingleton<AllSavedReportListViewModel>();
            services.AddSingleton<SavedUserReportListViewModel>();
        }

        internal static IEnumerable<Type> GetViewModelTypes()
        {
            static bool HasComplexPublicOrInternalCtor(Type t)
            {
                ConstructorInfo[] constructorInfos = t.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                static bool IsPublicOrInternalCtor(ConstructorInfo info)
                {
                    return (info.IsPublic || info.IsAssembly) && info.IsPrivate == false;
                }

                return constructorInfos.Any(IsPublicOrInternalCtor);
            }

            Assembly assembly = typeof(ViewModelBase).Assembly;
            return assembly.GetTypes()
                .Where(t => (t.IsSubclassOf(typeof(ViewModelBase)) || typeof(ViewModelBase).IsAssignableFrom(t)) && !t.IsAbstract && HasComplexPublicOrInternalCtor(t))
                .ToList();
        }
    }
}