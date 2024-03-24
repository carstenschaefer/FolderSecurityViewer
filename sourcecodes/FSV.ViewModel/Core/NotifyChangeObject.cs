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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Resources;

    /// <summary>
    ///     Provides implementation of System.ComponentModel.INotifyPropertyChanged interface to raise notification of any
    ///     change in properties of derived classes.
    /// </summary>
    public abstract class NotifyChangeObject : INotifyPropertyChanged
    {
        protected virtual bool ThrowOnInvalidPropertyName { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private static string GetPropertyName<T>(Expression<Func<T>> action)
        {
            var expression = (MemberExpression)action.Body;
            string propertyName = expression.Member.Name;
            return propertyName;
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> action)
        {
            string propertyName = GetPropertyName(action);
            this.RaisePropertyChanged(propertyName);
        }

        protected void RaisePropertyChanged<T>(params Expression<Func<T>>[] actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("propertyNames");
            }

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler == null)
            {
                return;
            }

            foreach (Expression<Func<T>> action in actions)
            {
                string propertyName = GetPropertyName(action);
                this.RaisePropertyChanged(propertyName);
            }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            this.PropertyChanged?.Invoke(this, e);
            this.OnPropertyChange(e);
        }

        /// <summary>
        ///     Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void RaisePropertyChanged(params string[] propertyNames)
        {
            if (propertyNames == null)
            {
                throw new ArgumentNullException("propertyNames");
            }

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler == null)
            {
                return;
            }

            foreach (string propertyName in propertyNames)
            {
                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        ///     Warns the developer if this object does not have
        ///     a public property with the specified name. This
        ///     method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = string.Format(ErrorResource.InvalidPropertyNameText, propertyName);

                if (this.ThrowOnInvalidPropertyName)
                {
                    throw new Exception(msg);
                }

                Debug.Fail(msg);
            }
        }

        protected virtual void OnPropertyChange(PropertyChangedEventArgs e)
        {
            // Do nothing. 
        }

        /// <summary>
        ///     Sets the value only if it is not equal to currently assign value.
        /// </summary>
        /// <typeparam name="T">Type of property/</typeparam>
        /// <param name="local">A reference to local variable</param>
        /// <param name="value">A value in set property.</param>
        /// <param name="propertyName">Name of property to notify change of property.</param>
        protected void Set<T>(ref T local, T value, string propertyName)
        {
            if (local == null || !local.Equals(value))
            {
                local = value;
                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        ///     Sets the value.
        /// </summary>
        /// <typeparam name="T">Type of property/</typeparam>
        /// <param name="local">A reference to local variable</param>
        /// <param name="value">A value in set property.</param>
        /// <param name="propertyName">Name of property to notify change of property.</param>
        protected void DoSet<T>(ref T local, T value, string propertyName)
        {
            local = value;
            this.RaisePropertyChanged(propertyName);
        }
    }
}