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

namespace FSV.FileSystem.Interop.Types
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Abstractions;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal abstract class FolderReport : IFolderReport
    {
        private readonly IList<Exception> exceptions = new List<Exception>();
        private AggregateException aggregateException;

        protected FolderReport()
        {
        }

        protected FolderReport(string fullName, string name = null)
        {
            this.FullName = fullName;
            this.Name = name;
        }

        public string FullName { get; }

        public string Name { get; }

        public long FileCount { get; set; }

        public double Size { get; set; }

        public long FileCountInclSub { get; set; }

        public double SizeInclSub { get; set; }

        public string Owner { get; set; }

        public Exception Exception => this.exceptions.Any()
            ? this.aggregateException ??= new AggregateException(this.exceptions)
            : null;

        public FolderReportStatus Status { get; protected set; }

        internal void AddException(Exception e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            this.exceptions.Add(e);
        }
    }
}