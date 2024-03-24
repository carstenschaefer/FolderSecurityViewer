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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Abstractions;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;

    internal static class FolderReportExceptionExtensions
    {
        public static void EnumerateFoldersWithExceptionAndLog(this IEnumerable<IFolderReport> folders, ILogger logger, IDisplayService displayService)
        {
            if (folders == null)
            {
                throw new ArgumentNullException(nameof(folders));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (displayService == null)
            {
                throw new ArgumentNullException(nameof(displayService));
            }

            var builder = new StringBuilder();
            IEnumerable<IFolderReport> foldersWithException = folders.Where(m => m.Exception != null);
            foreach (IFolderReport item in foldersWithException)
            {
                logger.LogError(item.Exception, "The operation for path {Path} completed with errors.", item.FullName);
                builder.AppendFormat($"The operation for the path {item.FullName} completed with an error: {item.Exception.Message}\n");
            }

            displayService.ShowError(builder.ToString());
        }
    }
}