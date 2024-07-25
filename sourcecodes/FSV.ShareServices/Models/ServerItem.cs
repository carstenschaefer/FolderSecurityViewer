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

namespace FSV.ShareServices.Models
{
    using System;
    using System.Collections.Generic;
    using Constants;

    public sealed class ServerItem
    {
        public ServerItem(string name, ServerState state) : this(name, state, new List<ShareItem>())
        {
            this.Name = name;
            this.State = state;
        }

        public ServerItem(string name, ServerState state, IList<ShareItem> shares)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            this.Shares = shares ?? throw new ArgumentNullException(nameof(shares));

            this.Name = name;
            this.State = state;
        }

        public string Name { get; }
        public ServerState State { get; internal set; }
        public IList<ShareItem> Shares { get; }
    }
}