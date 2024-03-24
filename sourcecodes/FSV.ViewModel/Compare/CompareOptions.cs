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

namespace FSV.ViewModel.Compare
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Resources;

    public class CompareOptions : ReadOnlyDictionary<int, string>
    {
        public CompareOptions() : base(new Dictionary<int, string>(6))
        {
            this.Dictionary.Add(CompareOption.All, PermissionCompareResource.CompareAll);
            this.Dictionary.Add(CompareOption.Similar, PermissionCompareResource.CompareSimilar);
            this.Dictionary.Add(CompareOption.Changed, PermissionCompareResource.CompareChanged);
            this.Dictionary.Add(CompareOption.Added, PermissionCompareResource.CompareAdded);
            this.Dictionary.Add(CompareOption.Removed, PermissionCompareResource.CompareRemoved);
            this.Dictionary.Add(CompareOption.NotSimilar, PermissionCompareResource.CompareNotSimilar);
        }
    }
}