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

namespace FSV.ViewModel.ShareReport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdBrowser;
    using Core;
    using Events;
    using Microsoft.Extensions.Logging;
    using Prism.Events;
    using Resources;
    using Security.Abstractions;
    using ShareServices.Abstractions;
    using ShareServices.Models;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ServersContainerViewModel : WorkspaceViewModel
    {
        private const int HideSeconds = 30 * 1000;
        private readonly ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder;

        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<ServersContainerViewModel> logger;
        private readonly ISecurityContext securityContext;
        private readonly ModelBuilder<ServerItem, bool, SharedServerViewModel> sharedServiceViewModelBuilder;
        private readonly IShareScannerFactory shareScannerFactory;

        private readonly IShareServersManager shareServersManager;

        private readonly object syncObject = new();
        private ICommand _addServerModalCommand;

        private ICommand _addServerTextCommand;
        private IAsyncCommand _clearServersCommand;

        private string _newServerName;
        private IAsyncCommand _refreshServersCommand;

        private ShareViewModel _selectedShare;
        private ICommand _serverScanCommand;

        private string _serversCountText;
        private IAsyncCommand addServerFromAdBrowserCommand;

        private IList<SharedServerViewModel> internalServers;
        private bool loadErrorOccured;

        public ServersContainerViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            IShareServersManager shareServersManager,
            IShareScannerFactory shareScannerFactory,
            ISecurityContext securityContext,
            ILogger<ServersContainerViewModel> logger,
            ModelBuilder<ServerItem, bool, SharedServerViewModel> sharedServiceViewModelBuilder,
            ModelBuilder<ADBrowserType, ADBrowserViewModel> adBrowserViewModelBuilder)
        {
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.shareServersManager = shareServersManager ?? throw new ArgumentNullException(nameof(shareServersManager));
            this.shareScannerFactory = shareScannerFactory ?? throw new ArgumentNullException(nameof(shareScannerFactory));
            this.securityContext = securityContext ?? throw new ArgumentNullException(nameof(securityContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.sharedServiceViewModelBuilder = sharedServiceViewModelBuilder ?? throw new ArgumentNullException(nameof(sharedServiceViewModelBuilder));
            this.adBrowserViewModelBuilder = adBrowserViewModelBuilder ?? throw new ArgumentNullException(nameof(adBrowserViewModelBuilder));

            this.eventAggregator.GetEvent<RemoveServerEvent>().Subscribe(x => this.RemoveServerAsync(x).FireAndForgetSafeAsync());
            this.eventAggregator.GetEvent<AddServersEvent>().Subscribe(x => this.AddServersFromAdBrowser(x).FireAndForgetSafeAsync());

            this.internalServers = new List<SharedServerViewModel>(0);
        }

        public string NewServerName
        {
            get => this._newServerName;
            set => this.Set(ref this._newServerName, value, nameof(this.NewServerName));
        }

        public string ServersCountText
        {
            get => this._serversCountText;
            set => this.Set(ref this._serversCountText, value, nameof(this.ServersCountText));
        }

        public IEnumerable<SharedServerViewModel> Servers => this.internalServers;

        public bool ServersEmpty { get; private set; }

        public bool LoadErrorOccured
        {
            get => this.loadErrorOccured;
            private set => this.Set(ref this.loadErrorOccured, value, nameof(this.LoadErrorOccured));
        }

        public SharedServerViewModel SelectedServer { get; set; }

        public ShareViewModel SelectedShare
        {
            get => this._selectedShare;
            set => this.Set(ref this._selectedShare, value, nameof(this.SelectedShare));
        }

        public ICommand AddServerTextCommand => this._addServerTextCommand ??= new AsyncRelayCommand(this.AddServerFromTextAsync);
        public ICommand AddServerWinCommand => this._addServerModalCommand ??= new AsyncRelayCommand(this.AddServerViewAsync);
        public IAsyncCommand AddServerFromAdBrowserCommand => this.addServerFromAdBrowserCommand ??= new AsyncRelayCommand(this.AddServerFromADBrowser);
        public ICommand ScanServerCommand => this._serverScanCommand ??= new AsyncRelayCommand(this.StartServersScanAsync);
        public IAsyncCommand ClearServersCommand => this._clearServersCommand ??= new AsyncRelayCommand(this.ClearServersAsync);
        public IAsyncCommand RefreshServersCommand => this._refreshServersCommand ??= new AsyncRelayCommand(this.RefreshServersAsync);

        /// <summary>
        ///     Gets server of selected share or selected server.
        /// </summary>
        internal SharedServerViewModel GetCurrentServer()
        {
            SharedServerViewModel server;

            lock (this.syncObject)
            {
                server = this.internalServers.AsParallel().FirstOrDefault(m => m.Shares.Any(n => n == this.SelectedShare));
            }

            if (server == null)
            {
                return this.SelectedServer;
            }

            server.SelectedShare = this.SelectedShare;
            return server;
        }

        public async Task FillServersAsync()
        {
            this.DoProgress();

            try
            {
                IEnumerable<ServerItem> elements = await this.shareServersManager.GetServerListAsync();
                IList<SharedServerViewModel> viewModels = elements
                    .Select(m => this.sharedServiceViewModelBuilder.Build(m, false))
                    .OrderBy(m => m.DisplayName)
                    .ToList();

                await this.SetServersAsync(viewModels);
            }
            catch (ShareServersManagerException e)
            {
                this.logger.LogError(e, "Failed to load servers data file due to an unhandled error.");

                this.LoadErrorOccured = true;
                this.ServersEmpty = true;
            }
            finally
            {
                this.StopProgress();
            }
        }

        private async Task AddServerFromTextAsync(object p)
        {
            // action by press 'Add server' button
            if (!string.IsNullOrEmpty(this.NewServerName))
            {
                await this.AddServersFromStringAsync(this.NewServerName);
                await this.HideNewIndicator();

                this.NewServerName = string.Empty;
            }
            else
            {
                this.dialogService.ShowMessage(SharedServersResource.ServerNameEmptyMessage);
            }
        }

        private async Task AddServersFromStringAsync(string serverSet)
        {
            serverSet = serverSet.Replace("\\\\", string.Empty);

            // possible set of names - comma (semicolon/space) as delimeter
            string[] names = serverSet
                .Replace(";", ",")
                .Replace(" ", ",")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            await this.AddServersFromStringListAsync(names);
        }

        private async Task AddServersFromStringListAsync(IEnumerable<string> servers)
        {
            var existingNames = new List<string>();
            var list = new List<SharedServerViewModel>(servers.Count());

            foreach (string singleName in servers)
            {
                if (string.IsNullOrEmpty(singleName))
                {
                    continue;
                }

                if (!this.shareServersManager.ServerExists(singleName))
                {
                    list.Add(this.sharedServiceViewModelBuilder.Build(this.shareServersManager.CreateServer(singleName), true));
                }
                else
                {
                    existingNames.Add(singleName);
                }
            }

            await this.ConcatServersAsync(list);

            await this.PopulateSharesAsync(list);

            if (existingNames.Count > 0)
            {
                string concatExistingName = string.Join(",", existingNames);
                this.dialogService.ShowMessage(string.Format(SharedServersResource.ServersExistMessage, concatExistingName));
            }
        }

        private async Task ConcatServersAsync(List<SharedServerViewModel> list)
        {
            IList<SharedServerViewModel> concatOrderedList;

            lock (this.syncObject)
            {
                concatOrderedList = this.internalServers
                    .Where(m => m != SharedServerViewModel.Empty)
                    .Concat(list)
                    .OrderBy(m => m.DisplayName)
                    .ToList();
            }

            await this.SetServersAsync(concatOrderedList);
        }

        private async Task AddServerViewAsync(object p)
        {
            var addServerViewModel = new AddServersViewModel();

            async void OnAddServerViewModelClosing(object s, CloseCommandEventArgs e)
            {
                if (e.IsOK)
                {
                    await this.AddServersFromStringAsync(addServerViewModel.NewServerNames);
                    this.ServersCountText = this.GetServersCountText();

                    await this.HideNewIndicator();
                }

                addServerViewModel.Closing -= OnAddServerViewModelClosing;
            }

            addServerViewModel.Closing += OnAddServerViewModelClosing;
            this.dialogService.ShowDialog(addServerViewModel);
            await Task.CompletedTask;
        }

        private async Task AddServersFromAdBrowser(IEnumerable<string> servers)
        {
            await this.AddServersFromStringListAsync(servers);
            await this.HideNewIndicator();
        }

        private async Task AddServerFromADBrowser(object p)
        {
            ADBrowserViewModel adBrowserViewModel = this.adBrowserViewModelBuilder.Build(ADBrowserType.Computers);
            await adBrowserViewModel.InitializeAsync();

            this.dialogService.ShowDialog(adBrowserViewModel);
        }

        private async Task StartServersScanAsync(object _)
        {
            this.DoProgress();

            // Trying SecurityContext to test that there is no error when app is run with non-admin user.
            await this.securityContext.RunAsync(this.ScanServerAsync);
        }

        private async Task ScanServerAsync(object _)
        {
            IShareServerScanner serverScanner = this.shareScannerFactory.CreateServerScanner();
            IEnumerable<Server> scannedServers = await serverScanner.GetServersAsync();

            var list = new List<SharedServerViewModel>(scannedServers.Count());

            foreach (Server item in scannedServers.OrderBy(m => m.Name))
            {
                SharedServerViewModel existingServer;

                lock (this.syncObject)
                {
                    existingServer = this.internalServers.FirstOrDefault(e => string.Compare(e.DisplayName, item.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
                }

                if (existingServer == null)
                {
                    list.Add(this.sharedServiceViewModelBuilder.Build(this.shareServersManager.CreateServer(item.Name), true));
                }
            }

            await this.ConcatServersAsync(list);

            await this.PopulateSharesAsync(list);

            await this.HideNewIndicator();
        }

        private async Task RefreshServersAsync(object p)
        {
            await this.PopulateSharesAsync(this.internalServers);
        }

        private async Task RemoveServerAsync(SharedServerViewModel server)
        {
            if (server == null)
            {
                return;
            }

            this.DoProgress();

            this.shareServersManager.RemoveServer(server.DisplayName);

            IList<SharedServerViewModel> itemsAfterRemoved;
            lock (this.syncObject)
            {
                itemsAfterRemoved = this.internalServers.Where(m => m != server).ToList();
            }

            await this.SetServersAsync(itemsAfterRemoved);

            this.SelectedServer = null;

            this.StopProgress();
        }

        private async Task ClearServersAsync(object p)
        {
            var messageViewModel = new MessageViewModel(SharedServersResource.RemoveAllServersMessage) { ShowCancel = true };
            if (!this.dialogService.ShowDialog(messageViewModel))
            {
                return;
            }

            this.DoProgress();

            this.shareServersManager.RemoveServers();
            this.shareServersManager.Save();

            await this.SetServersAsync(new List<SharedServerViewModel>(0));

            this.StopProgress();
        }

        private string GetServersCountText()
        {
            var text = string.Empty;

            var count = 0;
            var newCount = 0;

            lock (this.syncObject)
            {
                count = this.internalServers.Count(m => m != SharedServerViewModel.Empty);
                newCount = this.internalServers.Count(m => m.IsNew);
            }

            if (count == 1)
            {
                text = string.Format(SharedServersResource.OneServerCountCaption, count);
            }
            else if (count > 1)
            {
                text = string.Format(SharedServersResource.MoreServersCountCaption, count);
            }

            if (newCount > 0)
            {
                text += string.Format(SharedServersResource.NewServersCountCaption, newCount);
            }

            return text;
        }

        private async Task HideNewIndicator()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(HideSeconds));

                lock (this.syncObject)
                {
                    IEnumerable<SharedServerViewModel> items = this.internalServers.Where(m => m.IsNew);
                    foreach (SharedServerViewModel item in items)
                    {
                        item.IsNew = false;
                    }
                }

                this.ServersCountText = this.GetServersCountText();
            });
        }

        private async Task PopulateSharesAsync(IEnumerable<SharedServerViewModel> newServers)
        {
            var waiters = new List<Task<bool>>(newServers.Count());
            this.DoProgress();

            foreach (SharedServerViewModel item in newServers)
            {
                waiters.Add(item.StartScanAsync());
            }

            await Task.WhenAll(waiters);

            this.shareServersManager.Save();

            this.StopProgress();
        }

        private async Task SetServersAsync(IList<SharedServerViewModel> items)
        {
            await this.dispatcherService.InvokeAsync(() =>
            {
                if (items.Count == 0)
                {
                    this.ServersEmpty = true;
                    items.Add(SharedServerViewModel.Empty);
                }
                else
                {
                    this.ServersEmpty = false;
                }

                lock (this.syncObject)
                {
                    this.internalServers = items;
                }
            });

            this.RaisePropertyChanged(nameof(this.Servers), nameof(this.ServersEmpty));

            this.ServersCountText = this.GetServersCountText();
        }
    }
}