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

namespace FSV.Configuration.Database
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class DatabaseProvider : IEquatable<DatabaseProvider>
    {
        internal DatabaseProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(provider));
            }

            this.Name = provider;
        }

        /// <summary>
        ///     Gets a value indicating the name of the provider.
        /// </summary>
        public string Name { get; }

        public bool Equals(DatabaseProvider other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Name == other.Name;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public static DatabaseProvider Parse(string name)
        {
            if (TryParse(name, out DatabaseProvider provider))
            {
                return provider;
            }

            throw new ArgumentException($"The given value ({name}) is not a valid database-provider.", nameof(name));
        }

        public static bool TryParse(string name, out DatabaseProvider provider)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            IEnumerable<DatabaseProvider> knownProviders = DatabaseProviders.GetKnownProviders();
            provider = knownProviders.SingleOrDefault(provider => string.Compare(name, provider.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
            return provider != null;
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

            return this.Equals((DatabaseProvider)obj);
        }

        public override int GetHashCode()
        {
            return this.Name != null ? this.Name.GetHashCode() : 0;
        }
    }
}