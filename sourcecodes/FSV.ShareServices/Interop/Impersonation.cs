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
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    public sealed class Impersonation : IDisposable
    {
        private readonly WindowsImpersonationContext _context;
        private Impersonation _impersonation;

        public Impersonation()
        {
        }

        private Impersonation(string domain, string username, string password, bool useNetworkCreds)
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr hTokenDuplicate = IntPtr.Zero;

            int logonType = useNetworkCreds == false ? (int)LogonType.LOGON32_LOGON_INTERACTIVE : (int)LogonType.LOGON32_NEW_CREDENTIALS;
            int logonProvider = useNetworkCreds == false ? (int)LogonProvider.LOGON32_PROVIDER_DEFAULT : (int)LogonProvider.LOGON32_PROVIDER_WINNT50;

            var logon = false;

            if (NativeMethods.RevertToSelf())
            {
                logon = NativeMethods.LogonUserA(username, domain, password, logonType, logonProvider, ref hToken) != 0;

                if (logon)
                {
                    if (NativeMethods.DuplicateToken(hToken, (int)ImpersonationLevel.SecurityImpersonation, ref hTokenDuplicate) != 0)
                    {
                        var windowsIdentity = new WindowsIdentity(hTokenDuplicate);
                        this._context = windowsIdentity.Impersonate();
                    }
                }
            }

            if (hToken != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(hToken);
            }

            if (hTokenDuplicate != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(hTokenDuplicate);
            }

            if (!logon)
            {
                throw new Win32Exception("Logon not successfull. Error: " + ErrorHelper.GetErrorMessage((uint)Marshal.GetLastWin32Error()));
            }

            if (this._context == null)
            {
                throw new Win32Exception("Impersonation not successfull. Error: " + ErrorHelper.GetErrorMessage((uint)Marshal.GetLastWin32Error()));
            }
        }

        public void Dispose()
        {
            this._context.Undo();
            this._context.Dispose();
        }

        public static Impersonation LogonUserForUsing(string domain, string username, string password, bool useNetworkCreds)
        {
            return new Impersonation(domain, username, password, useNetworkCreds);
        }

        public Impersonation LogonUser(string domain, string username, string password, bool useNetworkCreds)
        {
            this._impersonation = new Impersonation(domain, username, password, useNetworkCreds);
            return this._impersonation;
        }

        public void UndoImpersonation()
        {
            this._context.Undo();
            this._context.Dispose();
        }
    }
}