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
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Events;
    using Passables;
    using Prism.Events;
    using Resources;
    using ShareServices.Abstractions;
    using ShareServices.Constants;
    using ShareServices.Models;

    /// <summary>
    /// </summary>
    public class SharedServerViewModel : ReportViewModel
    {
        internal static readonly SharedServerViewModel Empty = new(SharedServersResource.NoServersAvailableCaption);

        public static readonly string StateScanned = ServerState.Scanned.ToString();
        public static readonly string StateNotScanned = ServerState.NotScanned.ToString();
        public static readonly string StateFailure = ServerState.Failure.ToString();
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly IExportService exportService;
        private readonly ServerItem serverItem;

        private readonly ModelBuilder<ServerShare, ShareDetailViewModel> shareDetailViewModelBuilder;
        private readonly IShareScannerFactory shareScannerFactory;

        private readonly IShareServersManager shareServersManager;
        private readonly ModelBuilder<ShareItem, string, ShareViewModel> shareViewModelBuilder;

        private bool _expanded, _isNew, _selected;
        private ICommand _exportCommand;

        private ICommand _listSharesCommand;
        private ICommand _removeCommand;
        private ShareViewModel _selectedShare;

        private ShareDetailViewModel _shareDetail;

        private IShareScanner _shareScanner;
        private string _state;

        private SharedServerViewModel(string text)
        {
            this.IsEmpty = true;
            this.DisplayName = text;
        }

        internal SharedServerViewModel(
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            IShareServersManager shareServersManager,
            IShareScannerFactory shareScannerFactory,
            IExportService exportService,
            ModelBuilder<ServerShare, ShareDetailViewModel> shareDetailViewModelBuilder,
            ModelBuilder<ShareItem, string, ShareViewModel> shareViewModelBuilder,
            ServerItem server,
            bool isNew)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            this.shareServersManager = shareServersManager ?? throw new ArgumentNullException(nameof(shareServersManager));
            this.shareScannerFactory = shareScannerFactory ?? throw new ArgumentNullException(nameof(shareScannerFactory));
            this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            this.shareDetailViewModelBuilder = shareDetailViewModelBuilder ?? throw new ArgumentNullException(nameof(shareDetailViewModelBuilder));
            this.shareViewModelBuilder = shareViewModelBuilder ?? throw new ArgumentNullException(nameof(shareViewModelBuilder));
            this.serverItem = server ?? throw new ArgumentNullException(nameof(server));

            this.ReportType = ReportType.Share;
            this.ReportTypeCaption = ShareReport;

            this.IsNew = isNew;

            this.Header = new HeaderViewModel
            {
                RefreshCommand = this.ListSharesCommand
            };

            this.DisplayName = this.serverItem.Name;
            this.State = this.serverItem.State.ToString();

            if (this.State == StateFailure)
            {
                this.Header.SetText(ShareViewModel.EnumerationFailed.DisplayName, true);
            }
            else if (this.State == StateNotScanned)
            {
                this.Header.SetText(SharedServersResource.ServerNotScannedError, true);
            }

            if (!this.serverItem.Shares.Any())
            {
                this.Shares.Add(ShareViewModel.Empty);
                this.Header.SetText(ShareViewModel.Empty.DisplayName, true);
            }
            else
            {
                foreach (ShareItem item in this.serverItem.Shares)
                {
                    this.Shares.Add(this.shareViewModelBuilder.Build(item, this.DisplayName));
                }

                this.Header.SetText(string.Empty);
            }
        }


        private IDictionary<string, ShareDetailViewModel> ShareDetails { get; } = new Dictionary<string, ShareDetailViewModel>();

        public bool IsEmpty { get; }

        public string State
        {
            get => this._state;
            private set => this.DoSet(ref this._state, value, nameof(this.State));
        }

        public bool Expanded
        {
            get => this._expanded;
            set => this.Set(ref this._expanded, value, nameof(this.Expanded));
        }

        public bool Selected
        {
            get => this._selected;
            set => this.Set(ref this._selected, value, nameof(this.Selected));
        }

        public bool IsNew
        {
            get => this._isNew;
            internal set => this.Set(ref this._isNew, value, nameof(this.IsNew));
        }

        public ShareViewModel SelectedShare
        {
            get => this._selectedShare;
            set => this.Set(ref this._selectedShare, value, nameof(this.SelectedShare));
        }

        public ShareDetailViewModel ShareDetail
        {
            get => this._shareDetail;
            private set => this.Set(ref this._shareDetail, value, nameof(this.ShareDetail));
        }

        public IList<ShareViewModel> Shares { get; } = new ObservableCollection<ShareViewModel>();

        public ICommand ListSharesCommand => this._listSharesCommand ??= this.serverItem != null ? new AsyncRelayCommand(this.StartScanAsync) : null;
        public ICommand RemoveCommand => this._removeCommand ??= this.serverItem != null ? new RelayCommand(this.Remove) : null;
        public ICommand ExportCommand => this._exportCommand ??= this.serverItem != null && this.State == StateScanned ? new RelayCommand(this.Export, p => this.State == StateScanned && this.Shares.Count > 0) : null;

        internal void Remove(object p)
        {
            var messageViewModel = new MessageViewModel(string.Format(SharedServersResource.RemoveServerMessage, this.DisplayName)) { ShowCancel = true };

            void MessageClosing(object s, CloseCommandEventArgs e)
            {
                if (e.IsOK)
                {
                    this.eventAggregator.GetEvent<RemoveServerEvent>().Publish(this);
                }
            }

            messageViewModel.Closing += MessageClosing;
            this.dialogService.ShowDialog(messageViewModel);
            messageViewModel.Closing -= MessageClosing;
        }

        internal async Task<bool> StartScanAsync()
        {
            if (this.IsEmpty)
            {
                return await Task.FromResult(false);
            }

            this.Header.ShowProgress();
            this.Header.SetText(ShareViewModel.Loading.DisplayName);
            this.Expanded = true;

            await this.dispatcherService.InvokeAsync(() =>
            {
                this.Shares.Clear();
                this.Shares.Add(ShareViewModel.Loading);
            });

            var result = false;

            ShareViewModel selectedShare = this.SelectedShare;
            this.SelectedShare = null;
            this.ShareDetail = null;

            try
            {
                this.shareServersManager.RemoveShares(this.serverItem);

                IList<Share> listOfShares = await this.GetShareInstance().GetSharesOfServerAsync(this.DisplayName);

                if (listOfShares.Count > 0)
                {
                    foreach (Share item in listOfShares)
                    {
                        ShareItem shareItem = this.shareServersManager.CreateShare(this.serverItem, item.Name, item.Path);
                        await this.dispatcherService.InvokeAsync(() => { this.Shares.Add(this.shareViewModelBuilder.Build(shareItem, this.DisplayName)); });
                    }

                    this.shareServersManager.SetStateScanned(this.serverItem);
                    this.Header.SetText(string.Empty);
                    result = true;
                }
                else
                {
                    await this.dispatcherService.InvokeAsync(() => this.Shares.Add(ShareViewModel.Empty));
                    this.Header.SetText(ShareViewModel.Empty.DisplayName, true);
                }

                this.State = StateScanned;
            }
            catch (Exception)
            {
                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.Shares.Add(ShareViewModel.EnumerationFailed);
                    this.Header.SetText(SharedServersResource.EnumerationFailedCaption, true);
                });
                this.shareServersManager.SetStateScanFailed(this.serverItem);
                this.State = StateFailure;
            }
            finally
            {
                await this.dispatcherService.InvokeAsync(() =>
                {
                    this.Shares.Remove(ShareViewModel.Loading);

                    if (selectedShare != null)
                    {
                        ShareViewModel theShareFromList = this.Shares.FirstOrDefault(m => m.Path.ToLower().Equals(selectedShare?.Path.ToLower()));
                        if (theShareFromList != null)
                        {
                            this.SelectedShare = theShareFromList;
                        }
                    }
                });
            }

            this.Header.EndProgress();

            return result;
        }

        internal Share GetShareDetail(string shareName)
        {
            return this.GetShareInstance().GetShare(this.DisplayName, shareName);
        }

        private async Task StartScanAsync(object p)
        {
            this.ShareDetails.Clear();
            if (await this.StartScanAsync())
            {
                this.shareServersManager.Save();
            }
        }

        private void Export(object p)
        {
            this.exportService.ExportShareReport(this);
        }

        private IShareScanner GetShareInstance()
        {
            return this._shareScanner ??= this.shareScannerFactory.CreateShareScanner();
        }

        protected override void OnPropertyChange(PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(this.SelectedShare)) && this.SelectedShare != null && this.SelectedShare != ShareViewModel.Empty && this.SelectedShare != ShareViewModel.EnumerationFailed && this.SelectedShare != ShareViewModel.Loading)
            {
                if (!this.ShareDetails.ContainsKey(this.SelectedShare.DisplayName))
                {
                    this.ShareDetails[this.SelectedShare.DisplayName] = this.shareDetailViewModelBuilder.Build(new ServerShare(this.DisplayName, this.SelectedShare.DisplayName));
                    this.ShareDetails[this.SelectedShare.DisplayName].SharePath = this.SelectedShare.Share;
                }

                this.ShareDetail = this.ShareDetails[this.SelectedShare.DisplayName];
            }

            base.OnPropertyChange(e);
        }
    }
}