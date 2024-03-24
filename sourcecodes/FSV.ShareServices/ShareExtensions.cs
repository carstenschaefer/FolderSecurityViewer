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
    using Abstractions;
    using Interop;
    using JetBrains.Annotations;

    public static class ShareExtensions
    {
        public static Share Get(this Share instance, [NotNull] string server, [NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(server));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            try
            {
                if (instance.ShouldImpersonate())
                {
                    instance.Impersonate();
                }

                SharePermissionEntry[] sharePermissions;
                SHARE_INFO_2 shareInfo2 = NetShareFunctions.GetInfo(server, name, out sharePermissions);

                var share = new Share
                {
                    Name = shareInfo2.NetName,
                    Description = shareInfo2.Remark,
                    MaxUsers = shareInfo2.MaxUsers,
                    Path = shareInfo2.Path,
                    Trustees = TrusteeFunctions.GetShareTrustees(sharePermissions) //get shareTrustees from sharePermissionEntries
                };

                return share;
            }
            catch (ShareLibException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ShareLibException($"Exception while getting share. Error: {ex.Message}");
            }
            finally
            {
                if (instance.Impersonator != null)
                {
                    instance.UndoImpersonate();
                }
            }
        }

        public static IList<Share> GetOfServer(this Share share, string server)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(server));
            }

            try
            {
                if (share.ShouldImpersonate())
                {
                    share.Impersonate();
                }

                var shares = new List<Share>();

                IEnumerable<ShareInfo2> shareInfos = NetShare.Enum2(server);

                foreach (ShareInfo2 shareInfo in shareInfos)
                {
                    if (shareInfo.ShareType == SHARE_TYPE.STYPE_DISK || shareInfo.ShareType == SHARE_TYPE.STYPE_HIDDEN_DISK)
                    {
                        var shareModel = new Share
                        {
                            Name = shareInfo.NetName,
                            Description = shareInfo.Description,
                            ClientConnections = (int)shareInfo.CurrentUses,
                            Path = shareInfo.Path
                        };

                        shares.Add(shareModel);
                    }
                }

                return shares;
            }
            catch (ShareLibException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ShareLibException($"Exception while getting share. Error: {ex.Message}");
            }
            finally
            {
                if (share.Impersonator != null)
                {
                    share.UndoImpersonate();
                }
            }
        }
    }
}