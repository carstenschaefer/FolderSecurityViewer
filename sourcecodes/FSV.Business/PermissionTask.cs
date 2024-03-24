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

    public class PermissionTask : IPermissionTask
    {
        private readonly IAclModelBuilder _aclModelBuilder;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IActiveDirectoryFinderFactory _finderFactory;
        private readonly IAclViewProvider aclViewProvider;
        private readonly ILogger<PermissionTask> logger;
        private long busy;

        public PermissionTask(
            IAclModelBuilder modelBuilder,
            IActiveDirectoryFinderFactory finderFactory,
            IAclViewProvider aclViewProvider,
            ILogger<PermissionTask> logger)
        {
            this._aclModelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            this._finderFactory = finderFactory ?? throw new ArgumentNullException(nameof(finderFactory));
            this.aclViewProvider = aclViewProvider ?? throw new ArgumentNullException(nameof(aclViewProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsBusy => Interlocked.Read(ref this.busy) > 0;

        public bool CancelRequested => this._cancellationTokenSource.IsCancellationRequested;

        public void Cancel()
        {
            this._cancellationTokenSource.Cancel();
        }

        public void ClearADCache()
        {
            this._finderFactory.Clear();
        }

        public async Task<DataTable> RunAsync(string path, Action<int> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            void Progress(int count, int _)
            {
                progressCallback(count);
            }

            try
            {
                Interlocked.Increment(ref this.busy);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this._cancellationTokenSource.Token);
                return await Task.Run(() => this.GenerateResult(path, Progress, this._cancellationTokenSource.Token), this._cancellationTokenSource.Token);
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

        private DataTable GenerateResult(string path, Action<int, int> onProgress, CancellationToken cancellationToken)
        {
            var adFinderResult = new ActiveDirectoryScanResult<int>(onProgress);

            string host = path.GetServerName();

            IActiveDirectoryFinder activeDirectoryFinder = this._finderFactory.CreateActiveDirectoryFinder(adFinderResult);

            try
            {
                List<IAclModel> items = this.aclViewProvider.GetAclView(path).Select(this._aclModelBuilder.Build).ToList();
                foreach (IAclModel item in items)
                {
                    bool skipScan = activeDirectoryFinder.ScanOptions.CheckExclusionGroups(item.Account, string.Empty);

                    if (!skipScan)
                    {
                        var fileSystemRight = $"{item.TypeString}: {item.RightsString}";
                        activeDirectoryFinder.FindAdObject(item.Account, item.InheritanceFlagsString, fileSystemRight, host, string.Empty, item.Account, cancellationToken);
                    }
                }

                bool cancelled = cancellationToken.IsCancellationRequested;
                return cancelled ? null : adFinderResult.Result;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to generate results due to an unhandled error.");
            }

            return null;
        }
    }
}