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

namespace FSV.ViewModel.Abstractions
{
    using System;
    using System.ComponentModel;

    public class SortOrder : IEquatable<SortOrder>
    {
        private const string AscendingDirection = "Ascending";
        private const string DescendingDirection = "Descending";
        private const string IndeterminateDirection = "";

        public static readonly SortOrder Ascending = new(AscendingDirection);
        public static readonly SortOrder Descending = new(DescendingDirection);
        public static readonly SortOrder Indeterminate = new(IndeterminateDirection);

        private readonly string direction;

        private SortOrder(string direction)
        {
            this.direction = direction;
        }

        public bool Equals(SortOrder other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.direction == other.direction;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((SortOrder)obj);
        }

        public override int GetHashCode()
        {
            return this.direction != null ? this.direction.GetHashCode() : 0;
        }

        public ListSortDirection ToSortDirection()
        {
            return this.direction == IndeterminateDirection ? (ListSortDirection)(-1) : (ListSortDirection)Enum.Parse(typeof(ListSortDirection), this.direction);
        }

        public override string ToString()
        {
            return this.direction;
        }

        public string ToShortString()
        {
            return this.direction switch
            {
                AscendingDirection => "ASC",
                DescendingDirection => "DESC",
                _ => IndeterminateDirection
            };
        }

        public static SortOrder From(ListSortDirection direction)
        {
            return direction switch
            {
                ListSortDirection.Ascending => Ascending,
                ListSortDirection.Descending => Descending,
                _ => Indeterminate
            };
        }
    }
}