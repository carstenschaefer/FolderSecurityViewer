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
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using Models;

    public class AclCompareTask : IAclCompareTask
    {
        private readonly IAclModelBuilder aclModelBuilder;
        private readonly IAclViewProvider aclViewProvider;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<ReportTrustee> configReportTrustee;
        private readonly IFileManagementService fileManagementService;
        private long busy;

        public AclCompareTask(
            IAclModelBuilder aclModelBuilder,
            IAclViewProvider aclViewProvider,
            IFileManagementService fileManagementService,
            Func<ReportTrustee> configReportTrustee)
        {
            this.aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.configReportTrustee = configReportTrustee ?? throw new ArgumentNullException(nameof(configReportTrustee));
        }

        public bool IsBusy => Interlocked.Read(ref this.busy) > 0;

        public bool CancelRequested => this.cancellationTokenSource.IsCancellationRequested;

        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Dispose();
        }

        public async Task RunAsync(string path, Action<AclComparisonResult> onProgress, Action onComplete = null)
        {
            path = !string.IsNullOrEmpty(path) ? path : throw new ArgumentNullException(nameof(path));
            onProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));

            try
            {
                Interlocked.Increment(ref this.busy);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token);
                CancellationToken cancellationToken = linkedCancellationTokenSource.Token;
                await Task.Run(() => this.Start(path, onProgress, cancellationToken), cancellationToken);
                onComplete?.Invoke();
            }
            finally
            {
                Interlocked.Decrement(ref this.busy);
            }
        }

        private void Start(string path, Action<AclComparisonResult> progressCallback, CancellationToken cancellationToken)
        {
            IEnumerable<IAcl> result = this.aclViewProvider.GetAclView(path);
            var parentAcl = new List<IAclModel>(result.Select(this.aclModelBuilder.Build));

            const int level = 1;
            this.CompareSubDirectories(path, level, parentAcl, progressCallback, cancellationToken);
        }

        private void CompareSubDirectories(
            string dirName, int level,
            IEnumerable<IAclModel> parentAcl,
            Action<AclComparisonResult> progressCallback,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                ReportTrustee reportTrustee = this.configReportTrustee();
                if (level > reportTrustee.ScanLevel && reportTrustee.ScanLevel != 0)
                {
                    return;
                }

                IEnumerable<IAclModel> parentAclList = parentAcl.ToList();

                IEnumerable<IFolder> folderList = this.fileManagementService.GetDirectories(dirName).ToList();
                level++;
                foreach (IFolder item in folderList)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    string itemFullName = item.FullName;
                    if (this.fileManagementService.IsAccessDenied(itemFullName))
                    {
                        progressCallback(new AclComparisonError(itemFullName, "Access denied."));
                        continue;
                    }

                    IEnumerable<IAcl> folderItemAclView = this.aclViewProvider.GetAclView(itemFullName).ToList();
                    IEnumerable<IAclModel> thisAcl = folderItemAclView.Select(this.aclModelBuilder.Build).ToList();
                    if (!parentAclList.IsAclEqual(thisAcl))
                    {
                        progressCallback(new AclComparisonResult(itemFullName));
                    }

                    this.CompareSubDirectories(itemFullName, level, parentAclList, progressCallback, cancellationToken);
                }
            }
            catch (PathTooLongException e)
            {
                progressCallback(new AclComparisonError(dirName, "The given directory path name is too long.", e));
            }
            catch (IOException e)
            {
                progressCallback(new AclComparisonError(dirName, e.Message, e));
            }
            catch (Exception e)
            {
                progressCallback(new AclComparisonError(dirName, "An unhandled error has occurred.", e));
            }
        }
    }
}