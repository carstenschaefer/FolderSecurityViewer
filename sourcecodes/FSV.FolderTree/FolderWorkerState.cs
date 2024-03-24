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

namespace FSV.FolderTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class FolderWorkerState
    {
        private readonly object syncObject = new();
        private readonly List<string> uncPaths = new();

        public bool TryAddUncPath(string pathRoot)
        {
            if (string.IsNullOrWhiteSpace(pathRoot))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pathRoot));
            }

            lock (this.syncObject)
            {
                if (this.uncPaths.FirstOrDefault(m => string.Equals(m, pathRoot, StringComparison.CurrentCultureIgnoreCase)) == null)
                {
                    this.uncPaths.Add(pathRoot);
                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveUncPath(string pathRoot)
        {
            if (string.IsNullOrWhiteSpace(pathRoot))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pathRoot));
            }

            lock (this.syncObject)
            {
                string contains = this.uncPaths.FirstOrDefault(m => string.Equals(m, pathRoot, StringComparison.CurrentCultureIgnoreCase));
                if (!string.IsNullOrEmpty(contains))
                {
                    this.uncPaths.Remove(contains);
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<string> GetUncPaths()
        {
            lock (this.syncObject)
            {
                return this.uncPaths.AsReadOnly();
            }
        }
    }
}