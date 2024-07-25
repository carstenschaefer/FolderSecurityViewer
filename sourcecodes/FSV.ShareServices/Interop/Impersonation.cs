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
    using JetBrains.Annotations;

    public sealed class Impersonation : IDisposable
    {
        private bool isImpersonating;
        private readonly WindowsIdentity windowsIdentity;
        
        private Impersonation(string domain, string username, string password, bool useNetworkCreds)
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr hTokenDuplicate = IntPtr.Zero;

            int logonType = useNetworkCreds ? (int)LogonType.LOGON32_NEW_CREDENTIALS : (int)LogonType.LOGON32_LOGON_INTERACTIVE;
            int logonProvider = useNetworkCreds ? (int)LogonProvider.LOGON32_PROVIDER_WINNT50 : (int)LogonProvider.LOGON32_PROVIDER_DEFAULT;

            var logon = false;

            if (NativeMethods.RevertToSelf())
            {
                logon = NativeMethods.LogonUserA(username, domain, password, logonType, logonProvider, ref hToken) != 0;

                if (logon)
                {
                    if (NativeMethods.DuplicateToken(hToken, (int)ImpersonationLevel.SecurityImpersonation, ref hTokenDuplicate) != 0)
                    {
                        this.windowsIdentity = new WindowsIdentity(hTokenDuplicate);
                        this.isImpersonating = true;
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
                throw new Win32Exception("Logon not successful. Error: " + ErrorHelper.GetErrorMessage((uint)Marshal.GetLastWin32Error()));
            }

            if (this.windowsIdentity == null)
            {
                throw new Win32Exception("Impersonation not successful. Error: " + ErrorHelper.GetErrorMessage((uint)Marshal.GetLastWin32Error()));
            }
        }

        public void Dispose()
        {
            if (!this.isImpersonating)
            {
                return;
            }

            this.windowsIdentity?.Dispose();
            this.isImpersonating = false;
        }

        public static Impersonation LogonUserForUsing(string domain, string username, string password, bool useNetworkCreds)
        {
            return new Impersonation(domain, username, password, useNetworkCreds);
        }

        public static Impersonation LogonUser(string domain, string username, string password, bool useNetworkCreds)
        {
            return new Impersonation(domain, username, password, useNetworkCreds);
        }

        public void RunImpersonated([NotNull] Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (this.windowsIdentity == null || !this.isImpersonating)
            {
                throw new InvalidOperationException("No user is currently being impersonated.");
            }

            WindowsIdentity.RunImpersonated(this.windowsIdentity.AccessToken, action);
        }

        public T RunImpersonated<T>([NotNull] Func<T> func)
        {
            ArgumentNullException.ThrowIfNull(func);

            if (this.windowsIdentity == null || !this.isImpersonating)
            {
                throw new InvalidOperationException("No user is currently being impersonated.");
            }

            return WindowsIdentity.RunImpersonated(this.windowsIdentity.AccessToken, func);
        }

        public void UndoImpersonation()
        {
            
        }
    }
}