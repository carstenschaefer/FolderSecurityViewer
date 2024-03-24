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

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("FSV.ViewModel")]
[assembly: AssemblyDescription("Part of G-TAC's NTFS Permissions Reporter 'FolderSecurityViewer'")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("G-TAC Software UG, Katzweiler, Germany")]
[assembly: AssemblyProduct("FolderSecurityViewer")]
[assembly: AssemblyCopyright("Copyright ©  2015 - 2022 G-TAC Software UG")]
[assembly: AssemblyTrademark("G-TAC")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("28588f8b-9e14-41d7-a7b2-684046254185")]

[assembly: AssemblyVersion("2.7.0")]
[assembly: AssemblyFileVersion("2.7.0")]


[assembly: InternalsVisibleTo("FSV")]
[assembly: NeutralResourcesLanguage("en")]

[assembly: InternalsVisibleTo("FSV.ViewModel.UnitTest", AllInternalsVisible = true)]