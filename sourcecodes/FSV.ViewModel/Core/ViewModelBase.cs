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

    /// <summary>
    ///     Base class for all ViewModel classes in the application.
    ///     It provides support for property change notifications
    ///     and has a DisplayName property.  This class is abstract.
    /// </summary>
    public abstract class ViewModelBase : NotifyChangeObject, IDisposable
    {
        /// <summary>
        ///     Invoked when this object is being removed from the application
        ///     and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     Child classes can override this method to perform
        ///     clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        ///     Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~ViewModelBase()
        {
            this.Dispose(false);
            GC.SuppressFinalize(this);
        }

        #region DisplayName

        private string _displayName;

        /// <summary>
        ///     Returns the user-friendly name of this object.
        ///     Child classes can set this property to a new value,
        ///     or override it to determine the value on-demand.
        /// </summary>
        public virtual string DisplayName
        {
            get => this._displayName;
            protected set => this.Set(ref this._displayName, value, nameof(this.DisplayName));
        }

        #endregion // DisplayName
    }
}