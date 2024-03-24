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

namespace FSV.Business
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices;
    using AdServices.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using FileSystem.Interop.Core.Abstractions;
    using Microsoft.Extensions.Logging;

    public class UserPermissionTask : IUserPermissionTask
    {
        private readonly IAclModelBuilder _aclModelBuilder;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IActiveDirectoryFinderFactory _finderFactory;
        private readonly IAclViewProvider aclViewProvider;
        private readonly IDirectoryFolderEnumerator directoryEnumerator;
        private readonly Func<FolderEnumeratorOptions> folderEnumeratorOptionsFactory;
        private readonly ILogger<UserPermissionTask> logger;
        private readonly Func<ReportUser> reportUser;
        private long busy;

        public UserPermissionTask(
            IAclModelBuilder modelBuilder,
            IActiveDirectoryFinderFactory finderFactory,
            IAclViewProvider aclViewProvider,
            IDirectoryFolderEnumerator directoryFolderEnumerator,
            Func<FolderEnumeratorOptions> folderEnumeratorOptionsFactory,
            ILogger<UserPermissionTask> logger,
            Func<ReportUser> reportUser)
        {
            this._aclModelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            this._finderFactory = finderFactory ?? throw new ArgumentNullException(nameof(finderFactory));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.directoryEnumerator = directoryFolderEnumerator ?? throw new ArgumentNullException(nameof(directoryFolderEnumerator));
            this.folderEnumeratorOptionsFactory = folderEnumeratorOptionsFactory ?? throw new ArgumentNullException(nameof(folderEnumeratorOptionsFactory));
            this.reportUser = reportUser ?? throw new ArgumentNullException(nameof(reportUser));
        }

        public bool IsBusy => Interlocked.Read(ref this.busy) > 0;

        public bool CancelRequested => this._cancellationTokenSource.IsCancellationRequested;

        public void Cancel()
        {
            this._cancellationTokenSource.Cancel();
        }

        public void ClearActiveDirectoryCache()
        {
            this._finderFactory.Clear();
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="BusinessServiceException">
        ///     Throws a <see cref="BusinessServiceException" /> in case the current task
        ///     execution encounters an error.
        /// </exception>
        public async Task<UserPermissionTaskResult> RunAsync(string user, string path, Action<int> onProgress)
        {
            if (onProgress == null)
            {
                throw new ArgumentNullException(nameof(onProgress));
            }

            user = !string.IsNullOrEmpty(user) ? user : throw new ArgumentNullException(nameof(user));
            path = !string.IsNullOrEmpty(path) ? path : throw new ArgumentNullException(nameof(path));

            try
            {
                Interlocked.Increment(ref this.busy);

                void Progress(int count, int _)
                {
                    onProgress(count);
                }

                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this._cancellationTokenSource.Token);
                return await this.GenerateResultAsync(user, path, Progress, this._cancellationTokenSource.Token);
            }
            finally
            {
                Interlocked.Decrement(ref this.busy);
            }
        }

        public void Dispose()
        {
            this._cancellationTokenSource.Dispose();
        }

        private Task<UserPermissionTaskResult> GenerateResultAsync(string user, string path, Action<int, int> onProgress, CancellationToken cancellationToken)
        {
            return Task.Run(() => this.GenerateResult(user, path, onProgress, cancellationToken), cancellationToken);
        }

        private UserPermissionTaskResult GenerateResult(
            string user,
            string path,
            Action<int, int> onProgress,
            CancellationToken cancellationToken)
        {
            var adFinderResult = new ActiveDirectoryScanResult<int>(onProgress);
            IUserActiveDirectoryFinder activeDirectoryFinder = this._finderFactory.CreateUserActiveDirectoryFinder(user, adFinderResult);
            if (activeDirectoryFinder == null)
            {
                throw new BusinessServiceException($"The current {nameof(IActiveDirectoryFinderFactory)}-service did not return a valid instance for the given user.");
            }

            try
            {
                FolderEnumeratorOptions folderEnumeratorOptions = this.folderEnumeratorOptionsFactory();
                IEnumerable<IFolderReport> exceptionReports = this.ProcessDirectory(activeDirectoryFinder, path, folderEnumeratorOptions, cancellationToken);
                bool cancelled = cancellationToken.IsCancellationRequested;

                return new UserPermissionTaskResult(adFinderResult.Result, exceptionReports, cancelled);
            }
            catch (BusinessServiceException e)
            {
                const string errorMessage = "Failed to generate results due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new BusinessServiceException($"{errorMessage} See inner exception for further details.", e);
            }
            catch (ActiveDirectoryServiceException e)
            {
                const string errorMessage = "Failed to generate results due to an Active-Directory error.";
                this.logger.LogError(e, errorMessage);
                throw new BusinessServiceException($"{errorMessage} See inner exception for further details.", e);
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to generate results due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new BusinessServiceException($"{errorMessage} See inner exception for further details.", e);
            }
        }

        private IEnumerable<IFolderReport> ProcessDirectory(
            IUserActiveDirectoryFinder finder,
            string directory,
            FolderEnumeratorOptions folderEnumeratorOptions,
            CancellationToken cancellationToken)
        {
            IList<IFolderReport> folderReportsWithException = new List<IFolderReport>();
            int scanDepth = this.reportUser().ScanLevel;
            try
            {
                IEnumerable<IFolderReport> folderReports = this.directoryEnumerator.GetStructure(directory, folderEnumeratorOptions, () => { }, cancellationToken);
                foreach (IFolderReport folderReport in folderReports)
                {
                    if (folderReport.Exception == null)
                    {
                        string subDirectory = folderReport.FullName;
                        if (scanDepth != 0 && PathUtil.GetPathDepth(directory, subDirectory) > scanDepth)
                        {
                            continue;
                        }

                        this.OpenDirectoryAcl(finder, subDirectory, cancellationToken);
                    }
                    else
                    {
                        folderReportsWithException.Add(folderReport);
                    }
                }
            }
            catch (FindFileEnumeratorException e)
            {
                const string errorMessage = "Failed to iterate sub-directories due to a system error.";
                this.logger.LogError(e, errorMessage);
                throw new BusinessServiceException($"{errorMessage} See inner exception for further details.", e);
            }
            catch (UnauthorizedAccessException e)
            {
                const string errorMessage = "Failed to iterate sub-directories due to insufficient privileges.";
                this.logger.LogError(e, errorMessage);
                throw new BusinessServiceException($"{errorMessage} See inner exception for further details.", e);
            }

            return folderReportsWithException;
        }

        private void OpenDirectoryAcl(IUserActiveDirectoryFinder finder, string directoryPath, CancellationToken cancellationToken)
        {
            finder.CurrentDirectory = new DirectoryInfo(directoryPath);

            IEnumerable<IAcl> aclView = this.aclViewProvider.GetAclView(directoryPath);
            IEnumerable<IAclModel> items = aclView.Select(this._aclModelBuilder.Build);

            foreach (IAclModel item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                ActiveDirectoryScanOptions activeDirectoryScanOptions = finder.ScanOptions;
                if (activeDirectoryScanOptions == null)
                {
                    this.logger.LogWarning($"The ScanOptions-property of the current {nameof(IUserActiveDirectoryFinder)} instance is not set, thus the current ACL will be skipped.");
                    break;
                }

                var sid = string.Empty;
                bool skipScan = activeDirectoryScanOptions.CheckExclusionGroups(item.Account, sid);
                if (skipScan)
                {
                    break;
                }

                string host = directoryPath.GetServerName();

                var fileSystemRight = $"{item.TypeString}: {item.RightsString}";
                var localGroupName = string.Empty;
                string aclRight = item.InheritanceFlagsString;

                bool userFound = finder.FindUser(item.Account, aclRight, fileSystemRight, host, localGroupName, item.Account, cancellationToken);
                if (userFound)
                {
                    break;
                }
            }
        }
    }
}