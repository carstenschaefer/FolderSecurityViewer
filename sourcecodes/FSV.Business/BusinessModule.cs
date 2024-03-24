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

namespace FSV.Business
{
    using System;
    using Abstractions;
    using AdServices.Abstractions;
    using Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Worker;

    internal class BusinessModule : IModule
    {
        public void Load(ServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<IAclCompareTask, AclCompareTask>();
            services.AddTransient<IFolderTask, FolderTask>();
            services.AddTransient<IPermissionListTask, PermissionListTask>();
            services.AddTransient<IPermissionTask, PermissionTask>();
            services.AddTransient<IUserPermissionTask, UserPermissionTask>();
            services.AddTransient<Func<GroupSearcher>>(provider =>
            {
                var searcher = provider.GetRequiredService<ISearcher>();
                var logger = provider.GetRequiredService<ILogger<GroupSearcher>>();
                return () => new GroupSearcher(searcher, logger);
            });
        }
    }
}