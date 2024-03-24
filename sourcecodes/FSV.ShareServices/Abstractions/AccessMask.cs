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

namespace FSV.ShareServices
{
    using Interop;

    internal enum AccessMask : uint
    {
        Read = ACCESS_MASK.READ_CONTROL | ACCESS_MASK.SYNCHRONIZE | ACCESS_MASK.DESKTOP_READOBJECTS | ACCESS_MASK.DESKTOP_HOOKCONTROL | ACCESS_MASK.DESKTOP_JOURNALPLAYBACK | ACCESS_MASK.DESKTOP_WRITEOBJECTS,

        Change = ACCESS_MASK.DELETE | ACCESS_MASK.READ_CONTROL | ACCESS_MASK.SYNCHRONIZE | ACCESS_MASK.DESKTOP_READOBJECTS | ACCESS_MASK.DESKTOP_CREATEWINDOW | ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_HOOKCONTROL | ACCESS_MASK.DESKTOP_JOURNALRECORD | ACCESS_MASK.DESKTOP_JOURNALPLAYBACK | ACCESS_MASK.DESKTOP_WRITEOBJECTS |
                 ACCESS_MASK.DESKTOP_SWITCHDESKTOP,

        FullAccess = ACCESS_MASK.DESKTOP_READOBJECTS | ACCESS_MASK.DESKTOP_CREATEWINDOW | ACCESS_MASK.DESKTOP_CREATEMENU | ACCESS_MASK.DESKTOP_HOOKCONTROL | ACCESS_MASK.DESKTOP_JOURNALRECORD | ACCESS_MASK.DESKTOP_JOURNALPLAYBACK | ACCESS_MASK.DESKTOP_ENUMERATE | ACCESS_MASK.DESKTOP_WRITEOBJECTS | ACCESS_MASK.DESKTOP_SWITCHDESKTOP |
                     ACCESS_MASK.STANDARD_RIGHTS_ALL
    }
}