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

namespace FolderSecurityViewer.Controls
{
    using System;
    using System.Collections.Generic;

    public class PathSelectorItemEqualityComparer : IEqualityComparer<PathSelectorItem>
    {
        public bool Equals(PathSelectorItem x, PathSelectorItem y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Path.Equals(y.Path, StringComparison.OrdinalIgnoreCase) &&
                   x.Text.Equals(y.Text, StringComparison.OrdinalIgnoreCase) &&
                   x.IsShareServer == y.IsShareServer;
        }

        public int GetHashCode(PathSelectorItem obj)
        {
            return HashCode.Combine(obj.Path, obj.Text, obj.IsShareServer);
        }
    }
}