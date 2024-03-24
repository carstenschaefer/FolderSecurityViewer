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

namespace FSV.Business.Worker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Models;

    public class AclComparerWorker : BackgroundWorker
    {
        private const int UnlimitedScanLevel = 0;
        private readonly IAclViewProvider accessControlListViewProvider;
        private readonly IAclModelBuilder aclModelBuilder;
        private readonly IFileManagementService fileManagementService;
        private readonly ILogger<AclComparerWorker> logger;

        private readonly int maxLevel;


        public AclComparerWorker(
            IAclModelBuilder aclModelBuilder,
            IConfigurationManager configurationManager,
            IAclViewProvider accessControlListViewProvider,
            IFileManagementService fileManagementService,
            ILogger<AclComparerWorker> logger)
        {
            this.aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.accessControlListViewProvider = accessControlListViewProvider ?? throw new ArgumentNullException(nameof(accessControlListViewProvider));
            this.fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;

            ReportTrustee reportTrustee = configurationManager.GetReportTrustee();
            this.maxLevel = reportTrustee?.ScanLevel ?? UnlimitedScanLevel;
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var directoryPath = e.Argument.ToString();
            this.CompareSubDirectories(directoryPath);
        }

        private IEnumerable<IAclModel> GetAclView(string directoryPath)
        {
            IEnumerable<IAcl> nextAclView = this.accessControlListViewProvider.GetAclView(directoryPath).ToList();
            return nextAclView.Select(this.aclModelBuilder.Build).ToList();
        }

        private void CompareSubDirectories(string dirName)
        {
            var stack = new Stack<TraversalItem>();
            stack.Push(new TraversalItem(dirName, 0, this.GetAclView(dirName)));

            while (this.CancellationPending == false && stack.Any())
            {
                TraversalItem next = stack.Pop();

                IEnumerable<IFolder> folderList = this.fileManagementService.GetDirectories(next.DirectoryPath);
                foreach (IFolder folder in folderList)
                {
                    if (this.CancellationPending)
                    {
                        break;
                    }

                    try
                    {
                        string folderFullName = folder.FullName;
                        if (this.fileManagementService.IsAccessDenied(folder))
                        {
                            this.logger.LogInformation("Skipped {Folder}. Access denied.", folderFullName);
                            continue;
                        }

                        IEnumerable<IAclModel> folderAcl = this.GetAclView(folderFullName).ToList();
                        if (!next.Acl.IsAclEqual(folderAcl))
                        {
                            this.ReportProgress(1, new AclComparisonError(folderFullName, "The ACL differs from the parent´s directory ACL."));
                        }

                        if (next.Level <= this.maxLevel || this.maxLevel == UnlimitedScanLevel)
                        {
                            var traversalItem = new TraversalItem(folderFullName, next.Level + 1, folderAcl);
                            stack.Push(traversalItem);
                        }
                    }
                    catch (PathTooLongException e)
                    {
                        this.ReportProgress(1, new AclComparisonError(dirName, "The given directory path is too long.", e));
                    }
                    catch (Exception e)
                    {
                        this.ReportProgress(1, new AclComparisonError(dirName, "An unhandled error has occurred.", e));
                    }
                }
            }
        }

        private class TraversalItem
        {
            public TraversalItem(string directoryPath, int level, IEnumerable<IAclModel> acl)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(directoryPath));
                }

                this.DirectoryPath = directoryPath;
                this.Level = level;
                this.Acl = acl ?? throw new ArgumentNullException(nameof(acl));
            }

            public string DirectoryPath { get; }
            public int Level { get; }
            public IEnumerable<IAclModel> Acl { get; }
        }
    }
}