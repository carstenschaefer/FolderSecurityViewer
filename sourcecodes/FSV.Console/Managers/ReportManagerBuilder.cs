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

namespace FSV.Console.Managers
{
    using System;
    using Abstractions;
    using Commands;
    using Microsoft.Extensions.DependencyInjection;

    public class ReportManagerBuilder : IReportManagerBuilder
    {
        private readonly IArgumentValidationService argumentValidationService;
        private readonly IServiceProvider serviceProvider;

        public ReportManagerBuilder(IServiceProvider serviceProvider, IArgumentValidationService argumentValidationService)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.argumentValidationService = argumentValidationService ?? throw new ArgumentNullException(nameof(argumentValidationService));
        }

        public IReportManager Build(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return command switch
            {
                PermissionReportCommand permissionReportCommand => this.GetPermissionManager(permissionReportCommand),
                FolderReportCommand folderReportCommand => this.GetFolderManager(folderReportCommand),
                OwnerReportCommand ownerReportCommand => this.GetOwnerManager(ownerReportCommand),
                UserReportCommand userReportCommand => this.GetUserManager(userReportCommand),
                ShareReportCommand shareReportCommand => this.GetShareManager(shareReportCommand),
                _ => throw new InvalidOperationException()
            };
        }

        private IReportManager GetPermissionManager(PermissionReportCommand command)
        {
            this.argumentValidationService.ValidateDirectoryArgument(command.DirectoryArgument);

            var permissioData = new PermissionData(
                command.DirectoryArgument.Value,
                command.OptionExportType.Value(),
                command.OptionExportPath.Value(),
                command.OptionDb.HasValue(),
                command.OptionDifference.HasValue(),
                command.OptionDifferenceExportPath.Value());

            var resolverOverrides = new[]
            {
                new ResolverOverride(typeof(PermissionData), null, permissioData)
            };

            return this.serviceProvider.GetRequiredService<PermissionReportManager>(resolverOverrides);
        }

        private IReportManager GetFolderManager(FolderReportCommand command)
        {
            this.argumentValidationService.ValidateDirectoryArgument(command.DirectoryArgument);

            var folderData = new FolderData(
                command.DirectoryArgument.Value,
                command.OptionExportType.Value(),
                command.OptionExportPath.Value());

            var resolverOverrides = new[]
            {
                new ResolverOverride(typeof(FolderData), null, folderData)
            };

            return this.serviceProvider.GetRequiredService<FolderReportManager>(resolverOverrides);
        }

        private IReportManager GetOwnerManager(OwnerReportCommand command)
        {
            this.argumentValidationService.ValidateDirectoryArgument(command.DirectoryArgument);
            this.argumentValidationService.ValidateNameArgument(command.OwnerNameArgument);

            var ownerFolderData = new UserFolderData(
                command.DirectoryArgument.Value,
                command.OptionExportType.Value(),
                command.OptionExportPath.Value(),
                command.OwnerNameArgument.Value);

            var resolverOverrides = new[]
            {
                new ResolverOverride(typeof(UserFolderData), null, ownerFolderData)
            };

            return this.serviceProvider.GetRequiredService<OwnerReportManager>(resolverOverrides);
        }

        private IReportManager GetUserManager(UserReportCommand command)
        {
            this.argumentValidationService.ValidateDirectoryArgument(command.DirectoryArgument);
            this.argumentValidationService.ValidateNameArgument(command.UserArgument);

            var ownerFolderData = new UserFolderData(
                command.DirectoryArgument.Value,
                command.OptionExportType.Value(),
                command.OptionExportPath.Value(),
                command.UserArgument.Value);

            var resolverOverrides = new[]
            {
                new ResolverOverride(typeof(UserFolderData), null, ownerFolderData)
            };

            return this.serviceProvider.GetRequiredService<UserPermissionReportManager>(resolverOverrides);
        }

        private IReportManager GetShareManager(ShareReportCommand command)
        {
            this.argumentValidationService.ValidateNameArgument(command.ShareNameArgument);

            var ownerFolderData = new ShareData(
                command.ShareNameArgument.Value,
                command.OptionExportType.Value(),
                command.OptionExportPath.Value());

            var resolverOverrides = new[]
            {
                new ResolverOverride(typeof(ShareData), null, ownerFolderData)
            };

            return this.serviceProvider.GetRequiredService<ShareReportManager>(resolverOverrides);
        }
    }
}