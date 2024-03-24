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

namespace FSV.FileSystem.Interop
{
    using System;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices;

    internal static class MarshalExtensions
    {
        public static IntPtr OffsetOf<T>(Expression<Func<T, object>> fieldExpression)
        {
            if (fieldExpression == null)
            {
                throw new ArgumentNullException("fieldExpression");
            }

            string fieldName = null;
            var visitor = new MemberExpressionVisitor(x => { fieldName = x.Name; });
            visitor.Visit(fieldExpression);

            if (string.IsNullOrWhiteSpace(fieldName) == false)
            {
                return Marshal.OffsetOf(typeof(T), fieldName);
            }

            return IntPtr.Zero;
        }

        public static bool PtrToStructure<T>(IntPtr p, out T result)
        {
            result = default;
            object obj = Marshal.PtrToStructure(p, typeof(T));
            if (obj != null)
            {
                result = (T)obj;
                return true;
            }

            return false;
        }
    }
}