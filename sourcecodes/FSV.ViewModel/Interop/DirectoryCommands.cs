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

namespace FSV.ViewModel.Interop
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal class DirectoryCommands
    {
        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);

        internal static bool ShowProperties(string path)
        {
            var info = new ShellExecuteInfo();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = path;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;

            return ShellExecuteEx(ref info);
        }

        internal static void ShowInConsole(string path)
        {
            var processInfo = new ProcessStartInfo("cmd.exe");
            processInfo.WorkingDirectory = path;
            Process.Start(processInfo);
        }

        internal static void ShowInExplorer(string path)
        {
            Process.Start(path);
        }
    }
}