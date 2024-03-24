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

namespace FSV.Extensions.WindowConfiguration.Abstractions
{
    using System;

    public sealed class Position : IEquatable<Position>
    {
        public static readonly Position Empty = new();

        private Position()
        {
        }

        public Position(int width, int height, int left, int top, WindowState? state) : this()
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            this.Width = width;
            this.Height = height;
            this.Left = left;
            this.Top = top;
            this.State = state;
        }

        public int Width { get; }
        public int Height { get; }
        public int Left { get; }
        public int Top { get; }
        public WindowState? State { get; }

        public bool Equals(Position other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Width == other.Width && this.Height == other.Height && this.Left == other.Left && this.Top == other.Top && this.State == other.State;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is Position other && this.Equals(other));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.Width;
                hashCode = (hashCode * 397) ^ this.Height;
                hashCode = (hashCode * 397) ^ this.Left;
                hashCode = (hashCode * 397) ^ this.Top;
                hashCode = (hashCode * 397) ^ this.State.GetHashCode();
                return hashCode;
            }
        }
    }
}