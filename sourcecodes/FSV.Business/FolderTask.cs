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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions;
    using FileSystem.Interop.Abstractions;

    public sealed class FolderTask : IFolderTask
    {
        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();
        private readonly IDirectoryEnumerator directoryEnumerator;
        private long busy;

        public FolderTask(IDirectoryEnumerator directoryEnumerator)
        {
            this.directoryEnumerator = directoryEnumerator ?? throw new ArgumentNullException(nameof(directoryEnumerator));
        }

        public bool IsBusy => Interlocked.Read(ref this.busy) > 0;

        public bool CancelRequested => this.cancellationSource.IsCancellationRequested;

        public void Cancel()
        {
            this.cancellationSource.Cancel();
        }

        public void Dispose()
        {
            this.cancellationSource.Dispose();
        }

        public async Task<IEnumerable<IFolderReport>> RunAsync(string path, Action<long> onProgress)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            try
            {
                Interlocked.Increment(ref this.busy);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationSource.Token);
                CancellationToken cancellationToken = linkedCancellationTokenSource.Token;
                IEnumerable<IFolderReport> result = await Task.Run(() => this.StartFolder(path, onProgress, cancellationToken), cancellationToken);
                return this.cancellationSource.IsCancellationRequested ? Enumerable.Empty<IFolderReport>() : result;
            }
            finally
            {
                Interlocked.Decrement(ref this.busy);
            }
        }

        public async Task<IEnumerable<IFolderReport>> RunAsync(string path, string user, Action<long> onProgress)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            try
            {
                Interlocked.Increment(ref this.busy);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationSource.Token);
                CancellationToken cancellationToken = linkedCancellationTokenSource.Token;
                IEnumerable<IFolderReport> result = await Task.Run(() => this.StartUser(path, user, onProgress, cancellationToken), cancellationToken);

                return this.cancellationSource.IsCancellationRequested ? Enumerable.Empty<IFolderReport>() : result;
            }
            finally
            {
                Interlocked.Decrement(ref this.busy);
            }
        }

        private IEnumerable<IFolderReport> StartFolder(string scanDirectory, Action<long> progressCallback, CancellationToken cancellationToken)
        {
            var folderCount = 0L;

            void ProgressCallback()
            {
                progressCallback?.Invoke(++folderCount);
            }

            return this.directoryEnumerator.GetFolders(scanDirectory, ProgressCallback, cancellationToken);
        }

        private IEnumerable<IFolderReport> StartUser(string scanDirectory, string user, Action<long> progressCallback, CancellationToken cancellationToken)
        {
            var folderCount = 0L;

            void ProgressCallback()
            {
                progressCallback?.Invoke(++folderCount);
            }

            return this.directoryEnumerator.GetFolders(scanDirectory, user, ProgressCallback, cancellationToken);
        }
    }
}