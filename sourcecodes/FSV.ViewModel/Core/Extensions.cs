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

namespace FSV.ViewModel.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Windows.Data;

    internal static class Extensions
    {
        internal static void SetCurrentWorkspace<T>(this IList<T> list, T viewModel) where T : ViewModelBase
        {
            // Retrieve the view of collection in which view models are added.
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(list);

            collectionView?.MoveCurrentTo(viewModel); // Set the focus to current view model.
        }

        internal static void SetCurrentWorkspace<T>(this IList<T> list, int index) where T : ViewModelBase
        {
            // Retrieve the view of collection in which view models are added.
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(list);

            collectionView?.MoveCurrentTo(list[index]); // Set the focus to current view model.
        }

        internal static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, string propertyName)
        {
            return source.OrderBy(ToLambda<T>(propertyName).Compile());
        }

        internal static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source, string propertyName)
        {
            return source.OrderByDescending(ToLambda<T>(propertyName).Compile());
        }

        private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            MemberExpression property = Expression.Property(parameter, propertyName);
            UnaryExpression propAsObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
        }
    }
}