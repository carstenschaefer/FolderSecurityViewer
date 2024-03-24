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

namespace FSV.AdServices
{
    using System;
    using System.Collections.Generic;
    using Cache;

    public sealed class PrincipalContextCache : IPrincipalContextCache
    {
        private readonly IDictionary<string, PrincipalContextInfo> contexts = new Dictionary<string, PrincipalContextInfo>();
        private readonly object syncObject = new();

        public void Clear()
        {
            lock (this.syncObject)
            {
                this.contexts.Clear();
            }
        }

        public bool TryGetContext(string contextKey, out PrincipalContextInfo principalContext)
        {
            if (string.IsNullOrWhiteSpace(contextKey))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(contextKey));
            }

            principalContext = null;
            lock (this.syncObject)
            {
                if (!this.contexts.TryGetValue(contextKey, out PrincipalContextInfo found))
                {
                    return false;
                }

                principalContext = found;
                return true;
            }
        }

        public void AddContext(string contextKey, PrincipalContextInfo principalContext)
        {
            if (contextKey == null)
            {
                throw new ArgumentNullException(nameof(contextKey));
            }

            if (principalContext == null)
            {
                throw new ArgumentNullException(nameof(principalContext));
            }

            lock (this.syncObject)
            {
                this.contexts[contextKey] = principalContext;
            }
        }
    }
}