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

namespace FSV.FileSystem.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Threading;
    using Abstractions;
    using Core;
    using Core.Abstractions;
    using Types;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public sealed class FileManagement : IFileManagement
    {
        private static int[] cachedFileSystemRights;
        private readonly IAdvapi32 advapi32;
        private readonly IKernel32 kernel32;
        private readonly IKernel32FindFile kernel32FindFile;
        private readonly ISidUtil sidUtil;

        public FileManagement(
            IAdvapi32 advapi32,
            IKernel32 kernel32,
            IKernel32FindFile kernel32FindFile,
            ISidUtil sidUtil)
        {
            this.advapi32 = advapi32 ?? throw new ArgumentNullException(nameof(advapi32));
            this.kernel32 = kernel32 ?? throw new ArgumentNullException(nameof(kernel32));
            this.kernel32FindFile = kernel32FindFile ?? throw new ArgumentNullException(nameof(kernel32FindFile));
            this.sidUtil = sidUtil ?? throw new ArgumentNullException(nameof(sidUtil));
        }

        public bool DirectoryExist(LongPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var info = new DirectoryInfo(path);
            return info.Exists;
        }

        public IEnumerable<IFolder> GetDirectories(LongPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, $@"{directoryPath}\*");
            while (fileEnumerator.MoveNext())
            {
                Win32FindDataWrapper next = fileEnumerator.Current;
                if (next.IsValid == false || next.IsAccessDenied())
                {
                    string directoryName = Path.GetDirectoryName(next.Path);
                    yield return new Folder(next.Path, directoryName, false) { AccessDenied = true };
                    continue;
                }

                Win32FindData findData = next.GetWin32FindData();
                if (!findData.WorkOnFolder(false, false))
                {
                    continue;
                }

                string currentFileName = findData.cFileName;
                string newPath = Path.Combine(directoryPath, currentFileName);

                yield return new Folder(newPath, currentFileName, false)
                {
                    AccessDenied = false
                };
            }
        }

        public bool HasSubFolders(LongPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, $@"{directoryPath}\*");
            while (fileEnumerator.MoveNext())
            {
                Win32FindDataWrapper next = fileEnumerator.Current;
                if (next.IsValid == false || next.IsAccessDenied())
                {
                    continue;
                }

                Win32FindData findData = next.GetWin32FindData();
                if (!findData.IsDirectory())
                {
                    continue;
                }

                if (findData.IsHidden() || findData.IsSystem() || findData.IsTemporary())
                {
                    continue;
                }

                if (!findData.IsCurrentDirectory()
                    && !findData.IsParentDirectory())
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAccessDenied(LongPath dirName)
        {
            if (dirName == null)
            {
                throw new ArgumentNullException(nameof(dirName));
            }

            try
            {
                using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, $@"{dirName}\*");
                fileEnumerator.MoveNext();

                Win32FindDataWrapper next = fileEnumerator.Current;
                return next.IsValid == false || next.IsAccessDenied();
            }
            catch (DirectoryNotFoundException)
            {
                return true;
            }
            catch (FindFileEnumeratorException)
            {
                return true;
            }
        }

        public IEnumerable<LongPath> FindFilesAndDirs(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            var results = new List<LongPath>();
            using var tokenSource = new CancellationTokenSource();
            this.FindFilesAndDirs(directoryPath, results.Add, tokenSource.Token);
            return results;
        }

        public void FindFilesAndDirs(LongPath directoryPath, Action<LongPath> foundCallback, CancellationToken cancellationToken)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (foundCallback == null)
            {
                throw new ArgumentNullException(nameof(foundCallback));
            }

            var processingStack = new Stack<LongPath>();
            processingStack.Push(directoryPath);

            while (!cancellationToken.IsCancellationRequested && processingStack.Any())
            {
                LongPath currentPath = processingStack.Pop();
                string path = currentPath + @"\*";

                using var fileEnumerator = new FindFileEnumerator(this.kernel32, this.kernel32FindFile, path);
                while (fileEnumerator.MoveNext())
                {
                    Win32FindDataWrapper next = fileEnumerator.Current;
                    if (next.IsValid == false || next.IsAccessDenied())
                    {
                        continue;
                    }

                    Win32FindData data = next.GetWin32FindData();
                    string currentFileName = data.cFileName;

                    if (data.IsDirectory())
                    {
                        if (!data.IsCurrentDirectory() && !data.IsParentDirectory())
                        {
                            LongPath nextPath = Path.Combine(currentPath, currentFileName);
                            processingStack.Push(nextPath);
                            foundCallback(nextPath);
                        }
                    }
                    else
                    {
                        LongPath result = Path.Combine(currentPath, currentFileName);
                        foundCallback(result);
                    }
                }
            }
        }

        public IEnumerable<IAcl> GetAclViewManaged(string directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            var directoryInfo = new DirectoryInfo(directoryPath);
            return directoryInfo.GetAclView();
        }

        public IEnumerable<IAcl> GetAclView(LongPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            uint err = this.advapi32.GetNamedSecurityInfo(
                directoryPath,
                SeObjectType.SeFileObject,
                SecurityInformation.DaclSecurityInformation,
                IntPtr.Zero,
                out IntPtr _,
                out IntPtr _,
                out IntPtr _,
                out IntPtr securityDescriptor);

            if (err == WinError.NoError)
            {
                var result = new List<Acl>();

                try
                {
                    IntPtr acl = IntPtr.Zero;
                    if (this.advapi32.GetSecurityDescriptorDacl(
                            securityDescriptor, out bool daclPresent, out acl, out bool _))
                    {
                        if (daclPresent && !acl.Equals(IntPtr.Zero))
                        {
                            var aclSize = new AclSizeInformation();
                            var aclInformationLength = (uint)Marshal.SizeOf(typeof(AclSizeInformation));
                            if (this.advapi32.GetAclInformation(acl, ref aclSize, aclInformationLength, AclInformationClass.AclSizeInformation))
                            {
                                uint count = aclSize.AceCount;
                                for (var aceIndex = 0; aceIndex < count; aceIndex++)
                                {
                                    this.GetAceAt(acl, aceIndex, result.Add);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    this.kernel32.LocalFree(securityDescriptor);
                }

                return result;
            }

            string errorMessage = this.kernel32.GetErrorMessage(err);
            throw new IOException(errorMessage);
        }

        public string[] GetPathParts(string path)
        {
            string[] segments = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            var uri = new Uri(path);

            if (uri.IsUnc)
            {
                if (segments.Length == 1)
                {
                    throw new ArgumentException("Path is incomplete.", nameof(path));
                }

                return new[] { @"\\" + segments[0] + @"\" + segments[1] }
                    .Concat(segments.Skip(2))
                    .ToArray();
            }

            segments[0] = segments[0] + Path.DirectorySeparatorChar; // The first part is drive, and adding.

            return segments;
        }

        public DirectoryInfo GetDirectoryInfo(LongPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                throw new DirectoryNotFoundException($"{path}");
            }

            return info;
        }

        public bool DirectoryExistUnmanaged(LongPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            long attributes = this.kernel32.GetFileAttributes(directoryPath);
            bool validAttributes = attributes != Constants.InvalidFileAttributes;
            bool isDirectory = (attributes & Constants.FileAttributeDirectory) != 0;

            return validAttributes && isDirectory;
        }

        private void GetAceAt(IntPtr acl, int index, Action<Acl> nextCallback)
        {
            if (this.advapi32.GetAce(acl, index, out IntPtr acePtr))
            {
                if (MarshalExtensions.PtrToStructure(acePtr, out AccessAllowedAce ace))
                {
                    IntPtr fieldOffset = MarshalExtensions.OffsetOf<AccessAllowedAce>(x => x.SidStart);
                    var sidPointer = new IntPtr((long)acePtr + (long)fieldOffset);

                    IAclAccountModel aclAccount = this.sidUtil.GetAccountName(sidPointer);

                    FileSystemRights rights = GetRights(ace.Mask);
                    InheritanceFlags inheritanceFlag = GetInheritanceFlags(ace.Header.AceFlags, out bool inherited);
                    PropagationFlags propagationFlags = GetPropagationFlags(ace.Header.AceFlags);

                    if (string.IsNullOrEmpty(aclAccount.Name))
                    {
                        return;
                    }

                    var act = AccessControlType.Allow;

                    if ((uint)ace.Header.AceType == 1)
                    {
                        act = AccessControlType.Deny;
                    }

                    var accessRule = new FileSystemAccessRule(aclAccount.Name, rights, inheritanceFlag, propagationFlags, act);

                    Acl aclInformation = accessRule.GetAclInformation(inherited);
                    aclInformation.AccountType = aclAccount.Type;
                    nextCallback(aclInformation);
                }
            }
        }

        private static PropagationFlags GetPropagationFlags(byte flags)
        {
            var ret = PropagationFlags.None;

            var mask = (AceFlags)flags;
            if ((mask & AceFlags.NoPropagateInherit) == AceFlags.NoPropagateInherit)
            {
                ret |= PropagationFlags.NoPropagateInherit;
            }

            if ((mask & AceFlags.InheritOnly) == AceFlags.InheritOnly)
            {
                ret |= PropagationFlags.InheritOnly;
            }

            return ret;
        }

        private static InheritanceFlags GetInheritanceFlags(byte flags, out bool isInherited)
        {
            var ret = InheritanceFlags.None;
            isInherited = false;

            var mask = (AceFlags)flags;

            if ((mask & AceFlags.ContainerInherit) == AceFlags.ContainerInherit)
            {
                ret |= InheritanceFlags.ContainerInherit;
            }

            if ((mask & AceFlags.ObjectInherit) == AceFlags.ObjectInherit)
            {
                ret |= InheritanceFlags.ObjectInherit;
            }

            if ((mask & AceFlags.Inherited) == AceFlags.Inherited)
            {
                isInherited = true;
            }

            return ret;
        }

        private static FileSystemRights GetRights(int mask)
        {
            var result = 0;
            int[] fileSystemRightValues = cachedFileSystemRights ??= Enum.GetValues(typeof(FileSystemRights)).Cast<int>().ToArray();
            foreach (int item in fileSystemRightValues)
            {
                if ((mask & item) == item)
                {
                    result |= item;
                }
            }

            return (FileSystemRights)result;
        }
    }
}