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
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices;
    using AdServices.Abstractions;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;

    public class PermissionListTask : IPermissionListTask
    {
        private readonly IAclModelBuilder _aclModelBuilder;
        private readonly IActiveDirectoryFinderFactory _finderFactory;
        private readonly IAclViewProvider aclViewProvider;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<PermissionListTask> logger;
        private long busy;

        public PermissionListTask(
            IAclModelBuilder modelBuilder,
            IAclViewProvider aclViewProvider,
            IActiveDirectoryFinderFactory finderFactory,
            ILogger<PermissionListTask> logger)
        {
            this._aclModelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this._finderFactory = finderFactory ?? throw new ArgumentNullException(nameof(finderFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        /// <summary>
        ///     Runs permission scan asynchronously.
        /// </summary>
        /// <param name="paths">An array of paths to run permission report.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <param name="onProgress">
        ///     An <see cref="Action{int, int}" /> to notify the progress of the item at given index. First parameter denotes
        ///     count, and second denotes index.
        /// </param>
        /// <param name="onComplete">
        ///     An <see cref="Action{DataTable, int}" /> to notify that scan is complete. First parameter is DataTable that
        ///     contains result of scan and second is index of item that is scanned.
        /// </param>
        public async Task RunAsync(
            IEnumerable<string> paths,
            Action<int, int> onProgress,
            Action<DataTable, int> onComplete)
        {
            paths = paths ?? throw new ArgumentNullException(nameof(paths));

            try
            {
                Interlocked.Increment(ref this.busy);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token);
                CancellationToken cancellationToken = linkedCancellationTokenSource.Token;
                await Task.Run(() => this.Start(paths, onProgress, onComplete, cancellationToken), cancellationToken);
            }
            catch (AggregateException e)
            {
                throw new PermissionTaskExecutionException("Execution of the task failed with errors. See inner exception for further details.", e);
            }
            finally
            {
                Interlocked.Decrement(ref this.busy);
            }
        }

        private void Start(IEnumerable<string> paths, Action<int, int> progressCallback, Action<DataTable, int> completeCallback, CancellationToken cancellationToken)
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 10
            };

            var exceptions = new List<Exception>();
            Parallel.ForEach(paths, parallelOptions, (path, loopState, index) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    loopState.Stop();
                }

                try
                {
                    this.GenerateResult(path, (int)index, progressCallback, completeCallback, cancellationToken);
                }
                catch (PermissionTaskExecutionException e)
                {
                    exceptions.Add(e);
                }
            });

            if (exceptions.Any())
            {
                throw new AggregateException("Failed to generate results.", exceptions);
            }
        }

        private void GenerateResult(
            string path, int index,
            Action<int, int> progressCallback,
            Action<DataTable, int> completeCallback,
            CancellationToken cancellationToken)
        {
            var adFinderResult = new ActiveDirectoryScanResult<int>(progressCallback)
            {
                Passable = index
            };

            string host = path.GetServerName();
            IActiveDirectoryFinder activeDirectoryFinder = this._finderFactory.CreateActiveDirectoryFinder(adFinderResult);

            try
            {
                IEnumerable<IAclModel> items = this.aclViewProvider.GetAclView(path).Select(this._aclModelBuilder.Build).ToList();
                foreach (IAclModel item in items)
                {
                    bool skipScan = activeDirectoryFinder.ScanOptions.CheckExclusionGroups(item.Account, string.Empty);

                    if (skipScan)
                    {
                        continue;
                    }

                    var fileSystemRight = $"{item.TypeString}: {item.RightsString}";
                    activeDirectoryFinder.FindAdObject(item.Account, item.InheritanceFlagsString, fileSystemRight, host, string.Empty, item.Account, cancellationToken);
                }

                completeCallback(adFinderResult.Result, index);
            }
            catch (Exception e)
            {
                const string errorMessage = "Failed to generate results due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                throw new PermissionTaskExecutionException(errorMessage + " See inner exception for further details.", e);
            }
        }
    }
}