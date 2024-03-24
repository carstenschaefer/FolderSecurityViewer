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

namespace FSV.Console.Services
{
    using System;
    using System.IO;
    using System.Security;
    using Abstractions;
    using Commands;
    using FileSystem.Interop.Abstractions;
    using Properties;

    public class ArgumentValidationService : IArgumentValidationService
    {
        private readonly IFileManagementService fileManagementService;

        public ArgumentValidationService(IFileManagementService fileManagementService)
        {
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        }

        public bool ValidateDirectoryArgument(DirectoryArgument argument)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            if (string.IsNullOrEmpty(argument.Value))
            {
                throw new ArgumentException(string.Format(Resources.ArgumentEmptyError, argument.Name));
            }

            if (!this.fileManagementService.GetDirectoryExist(argument.Value))
            {
                throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotExistsError, argument.Value));
            }

            if (this.fileManagementService.IsAccessDenied(argument.Value))
            {
                throw new SecurityException($"{Resources.DirectoryAccessDeniedError} {argument.Value}");
            }

            return true;
        }

        public bool ValidateNameArgument(NameArgument argument)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            if (string.IsNullOrEmpty(argument.Value))
            {
                throw new ArgumentException(string.Format(Resources.ArgumentEmptyError, argument.Name));
            }

            return true;
        }
    }
}