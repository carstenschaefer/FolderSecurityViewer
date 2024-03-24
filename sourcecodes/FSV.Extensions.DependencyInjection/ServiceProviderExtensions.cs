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

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class ServiceProviderExtensions
    {
        public static T GetRequiredService<T>(this IServiceProvider serviceProvider, Action<Type, string, Exception> resolveErrorCallback, params object[] resolvedParameters)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            object instance = GetRequiredService(serviceProvider, typeof(T), resolveErrorCallback, resolvedParameters);
            if (instance is T model)
            {
                return model;
            }

            return default;
        }

        public static object GetRequiredService(this IServiceProvider serviceProvider, Type serviceType, Action<Type, string, Exception> resolveErrorCallback, params object[] resolvedParameters)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (resolvedParameters == null)
            {
                throw new ArgumentNullException(nameof(resolvedParameters));
            }

            ConstructorInfo[] constructorInfos = serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            static bool IsPublicOrInternalCtor(ConstructorInfo info)
            {
                return (info.IsPublic || info.IsAssembly) && info.IsPrivate == false;
            }

            ConstructorInfo ctor = constructorInfos.OrderByDescending(info => info.GetParameters().Length).FirstOrDefault(IsPublicOrInternalCtor);
            if (ctor is not null)
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                int numServices = parameters.Length - resolvedParameters.Length;
                var resolvedServices = new object[parameters.Length];
                for (var i = 0; i < numServices; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    Type next = parameter.ParameterType;
                    try
                    {
                        object serviceInstance = serviceProvider.GetRequiredService(next);
                        resolvedServices[i] = serviceInstance;
                    }
                    catch (Exception e)
                    {
                        resolveErrorCallback?.Invoke(next, parameter.Name, e);
                        if (resolveErrorCallback == null)
                        {
                            throw;
                        }
                    }
                }

                for (var i = 0; i < resolvedParameters.Length; i++)
                {
                    resolvedServices[i + numServices] = resolvedParameters[i];
                }

                return ctor.Invoke(resolvedServices);
            }

            return default;
        }

        public static T GetRequiredService<T>(this IServiceProvider serviceProvider, ResolverOverride[] resolvedParameters)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (resolvedParameters == null)
            {
                throw new ArgumentNullException(nameof(resolvedParameters));
            }

            Dictionary<string, object> namedServices = resolvedParameters.Where((x, i) => string.IsNullOrWhiteSpace(x.Name) == false)
                .ToDictionary(x => x.Name, x => x.Instance);

            Dictionary<Type, object> typedServices = resolvedParameters
                .Where(x => x.ServiceType != null)
                .ToDictionary(x => x.ServiceType, x => x.Instance);

            ConstructorInfo[] constructorInfos = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            static bool IsPublicOrInternalCtor(ConstructorInfo info)
            {
                return (info.IsPublic || info.IsAssembly) && info.IsPrivate == false;
            }

            ConstructorInfo ctor = constructorInfos.OrderByDescending(info => info.GetParameters().Length).FirstOrDefault(IsPublicOrInternalCtor);
            if (ctor is not null)
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                var parameterValues = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameterInfo = parameters[i];
                    if (namedServices.TryGetValue(parameterInfo.Name, out object serviceInstance))
                    {
                        parameterValues[i] = serviceInstance;
                    }

                    else if (typedServices.TryGetValue(parameterInfo.ParameterType, out serviceInstance))
                    {
                        parameterValues[i] = serviceInstance;
                    }

                    else
                    {
                        Type next = parameterInfo.ParameterType;
                        serviceInstance = serviceProvider.GetRequiredService(next);
                        parameterValues[i] = serviceInstance;
                    }
                }

                if (ctor.Invoke(parameterValues) is T service)
                {
                    return service;
                }
            }

            return default;
        }
    }
}