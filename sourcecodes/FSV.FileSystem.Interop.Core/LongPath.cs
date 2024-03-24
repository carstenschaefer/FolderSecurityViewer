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

namespace FSV.FileSystem.Interop.Core
{
    using System;

    public sealed class LongPath : IEquatable<LongPath>
    {
        private readonly string path;

        public LongPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            this.path = Win32FindDataHelper.HandleLongPath(path);
        }

        public int Length => this.path.Length;

        public bool Equals(LongPath other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.path == other.path;
        }

        public static implicit operator string(LongPath lp)
        {
            return lp.path;
        }

        public static implicit operator LongPath(string s)
        {
            return new LongPath(s);
        }

        public override string ToString()
        {
            return this.path;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is LongPath other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            return this.path != null ? this.path.GetHashCode() : 0;
        }
    }
}