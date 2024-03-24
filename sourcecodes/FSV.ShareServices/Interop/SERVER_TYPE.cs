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

namespace FSV.ShareServices.Interop
{
    using System;

    [Flags]
    internal enum SERVER_TYPE : uint
    {
        SV_TYPE_WORKSTATION = 0x00000001,
        SV_TYPE_SERVER = 0x00000002,
        SV_TYPE_SQLSERVER = 0x00000004,
        SV_TYPE_DOMAIN_CTRL = 0x00000008,
        SV_TYPE_PRINTQ_SERVER = 0x00000200,
        SV_TYPE_CLUSTER_NT = 0x01000000,
        SV_TYPE_TERMINALSERVER = 0x02000000,
        SV_TYPE_ALL = 0xFFFFFFFF
    }
}