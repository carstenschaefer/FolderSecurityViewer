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

namespace FSV.ShareServices.Abstractions
{
    using System;
    using Interop;

    public class Base : IDisposable
    {
        private readonly string _domain;
        private readonly string _password;
        private readonly string _user;

        internal Impersonation Impersonator;

        public Base()
        {
        }

        public Base(string user, string domain, string password)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(user));
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(domain));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
            }

            this._user = user;
            this._domain = domain;
            this._password = password;
        }

        public bool AccrossNetworkImpersonation { get; set; } = false;

        public void Dispose()
        {
            this.Impersonator?.Dispose();
        }

        internal bool ShouldImpersonate()
        {
            return !string.IsNullOrEmpty(this._user);
        }

        internal void Impersonate()
        {
            this.Impersonator = new Impersonation().LogonUser(this._domain, this._user, this._password, this.AccrossNetworkImpersonation);
        }

        internal void UndoImpersonate()
        {
            this.Impersonator.UndoImpersonation();
        }
    }
}