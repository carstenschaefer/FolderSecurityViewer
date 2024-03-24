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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Abstractions;
    using Interop;

    public static class ServerExtensions
    {
        private const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;

        public static IEnumerable<Server> GetDetailed(this Server server)
        {
            var networkComputers = new List<Server>();
            IntPtr buffer = IntPtr.Zero;

            try
            {
                uint entriesRead;
                uint totalEntries;
                uint resHandle;

                var serverType = (uint)SERVER_TYPE.SV_TYPE_ALL;
                int sizeofINFO = Marshal.SizeOf(typeof(SERVER_INFO_101));

                uint ret = NetApi32.NetServerEnum(null, 101, ref buffer, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, serverType, null, out resHandle);

                if (ret == (uint)RETURN_VALUE.NERR_Success)
                {
                    for (var i = 0; i < totalEntries; i++)
                    {
                        var tmpBuf = new IntPtr((long)buffer + i * sizeofINFO);

                        var svrInfo = (SERVER_INFO_101)Marshal.PtrToStructure(tmpBuf, typeof(SERVER_INFO_101));

                        var s = new Server();
                        s.Name = svrInfo.sv101_name;
                        s.Comment = svrInfo.sv101_comment;

                        var p = (SERVER_PLATFORM)svrInfo.sv101_platform_id;
                        s.PlatformName = p.ToString();

                        s.ServerType = string.Join(", ", GetServerType(svrInfo.sv101_type).ToArray());

                        s.Version = new Version((int)svrInfo.sv101_version_major, (int)svrInfo.sv101_version_minor);

                        networkComputers.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ShareLibException("Enum-Fehler: " + ex.Message);
            }
            finally
            {
                NetApi32.NetApiBufferFree(buffer);
            }

            return networkComputers;
        }

        private static IEnumerable<string> GetServerType(uint sv101_type)
        {
            var list = new List<string>();

            bool isWorkstation = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_WORKSTATION) == (uint)SERVER_TYPE.SV_TYPE_WORKSTATION;
            bool isServer = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_SERVER) == (uint)SERVER_TYPE.SV_TYPE_SERVER;
            bool isSqlServer = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_SQLSERVER) == (uint)SERVER_TYPE.SV_TYPE_SQLSERVER;
            bool isDomainController = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_DOMAIN_CTRL) == (uint)SERVER_TYPE.SV_TYPE_DOMAIN_CTRL;
            bool isPrintServer = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_PRINTQ_SERVER) == (uint)SERVER_TYPE.SV_TYPE_PRINTQ_SERVER;
            bool isClusterServer = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_CLUSTER_NT) == (uint)SERVER_TYPE.SV_TYPE_CLUSTER_NT;
            bool isTerminalServer = (sv101_type & (uint)SERVER_TYPE.SV_TYPE_TERMINALSERVER) == (uint)SERVER_TYPE.SV_TYPE_TERMINALSERVER;

            if (isWorkstation)
            {
                list.Add("Workstation");
            }

            if (isServer)
            {
                list.Add("Server");
            }

            if (isSqlServer)
            {
                list.Add("MS SQL Server");
            }

            if (isDomainController)
            {
                list.Add("Domain Controller");
            }

            if (isPrintServer)
            {
                list.Add("Printserver");
            }

            if (isClusterServer)
            {
                list.Add("Cluster-Server");
            }

            if (isTerminalServer)
            {
                list.Add("Terminalserver");
            }

            return list;
        }
    }
}