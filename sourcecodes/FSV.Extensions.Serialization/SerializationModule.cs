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

namespace FSV.Extensions.Serialization
{
    using System;
    using Abstractions;
    using DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Wrappers;

    public class SerializationModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<XmlSerializationWrapper>();
            services.AddTransient<JsonSerializationWrapper>();
            services.AddTransient<Func<SerializerType, ISerializationWrapper>>(provider =>
            {
                return type =>
                {
                    return type switch
                    {
                        SerializerType.Json => provider.GetRequiredService<JsonSerializationWrapper>(),
                        SerializerType.Xml => provider.GetRequiredService<XmlSerializationWrapper>(),
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                    };
                };
            });
        }
    }
}