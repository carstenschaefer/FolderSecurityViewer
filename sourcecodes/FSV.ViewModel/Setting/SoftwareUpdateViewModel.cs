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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Core;
    using Events;
    using Prism.Events;
    using Resources;

    public class SoftwareUpdateViewModel : SettingWorkspaceViewModel
    {
        private readonly string _downloadedFile;
        private readonly IEventAggregator _eventAggregator;
        private readonly string _osArch;
        private readonly IConfigurationManager configurationManager;
        private readonly IDispatcherService dispatcherService;
        private ICommand _checkUpdateCommand;
        private bool _inProgress;
        private string _progressText;

        public SoftwareUpdateViewModel(
            IDispatcherService dispatcherService,
            IEventAggregator eventAggregator,
            IDialogService dialogService,
            IConfigurationManager configurationManager) : base(dispatcherService, dialogService)
        {
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this._eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            this.DisplayName = ConfigurationResource.UpdatesCaption;
            this.InProgress = false;

            this.IsEnabled = !this.configurationManager.ConfigRoot.SettingLocked;

            this._downloadedFile = Path.Combine(Path.GetTempPath(), "FolderSecurityViewer-Setup.msi");
            this._osArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        }

        public bool InProgress
        {
            get => this._inProgress;
            set
            {
                this._inProgress = value;
                this.RaisePropertyChanged(() => this.InProgress);
            }
        }

        public string ProgressText
        {
            get => this._progressText;
            private set
            {
                this._progressText = value;
                this.RaisePropertyChanged(() => this.ProgressText);
            }
        }

        public ICommand CheckUpdateCommand => this._checkUpdateCommand ??= new RelayCommand(this.CheckForUpdates);

        private void CheckForUpdates(object obj)
        {
            this.InProgress = true;
            this.ProgressText = ConfigurationResource.CheckingForUpdatesText;

            Task.Run(this.StartVersionCheck);
        }

        private void StartVersionCheck()
        {
            var request = WebRequest.Create("https://fsv.azurewebsites.net/version") as HttpWebRequest;
            request.AllowAutoRedirect = true;

            //Trust all certificates
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            //// trust sender
            //System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, cert, chain, errors) => cert.Subject.Contains("www.foldersecurityviewer.com"));

            //// validate cert by calling a function
            //ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            request.Proxy = this.GetProxy();

            Task<WebResponse> response = request.GetResponseAsync();
            response.ContinueWith(this.ResponseReceived);
        }

        private void ResponseReceived(Task<WebResponse> task)
        {
            try
            {
                using var response = task.Result as HttpWebResponse;
                using var reader = new StreamReader(response.GetResponseStream());
                var newVersion = new Version(reader.ReadToEnd());
                Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;
                int compareResult = currentVersion.CompareTo(newVersion);
                if (compareResult < 0)
                {
                    this.DownloadMsi();
                }
                else
                {
                    this.dispatcherService.InvokeAsync(() =>
                    {
                        this.InProgress = false;
                        this.ProgressText = ConfigurationResource.UpdateUpToDateText;
                    });
                }
            }
            catch (AggregateException ex)
            {
                this.dispatcherService.InvokeAsync(() =>
                {
                    this.InProgress = false;
                    this.ProgressText = ex.GetBaseException().Message;
                });
            }
        }

        private void DownloadMsi()
        {
            this.dispatcherService.InvokeAsync(() =>
            {
                this.InProgress = true;
                this.ProgressText = ConfigurationResource.UpdateDownloadingText;
            });

            Task.Run(() =>
            {
                var client = new WebClient { Proxy = this.GetProxy() };
                client.DownloadProgressChanged += this.SetupDownloadProgressChanged;
                client.DownloadFileCompleted += this.SetupDownloadComplete;
                //client.DownloadFileAsync(new Uri(string.Format("http://localhost/download/FolderSecurityViewer-Setup({0}).msi", _osArch)), _downloadedFile);
                client.DownloadFileAsync(new Uri($"https://fsv.azurewebsites.net/download?fileName=FolderSecurityViewer-Setup({this._osArch}).msi"), this._downloadedFile);
            });
        }

        private void SetupDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.dispatcherService.InvokeAsync(() =>
                {
                    this.InProgress = false;
                    this.ProgressText = string.Format(ConfigurationResource.UpdateDownloadingError, e.Error.Message);
                });

                return;
            }

            this.dispatcherService.InvokeAsync(() =>
            {
                this.InProgress = false;
                this.ProgressText = ConfigurationResource.UpdateDownloadCompleteText;
                this._eventAggregator.GetEvent<CloseApplicationEvent>().Publish();
                Process.Start(this._downloadedFile);
            });
        }

        private void SetupDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.dispatcherService.InvokeAsync(() => { this.ProgressText = string.Format(ConfigurationResource.UpdateDownloadingProgressText, e.ProgressPercentage); });
        }

        private IWebProxy GetProxy()
        {
            IWebProxy proxy = null;

            if (NetworkConfigurationManager.ProxyType == ProxyType.Default)
            {
                proxy = WebRequest.DefaultWebProxy;

                // WebRequest.DefaultWebProxy doesn't return WebProxy object. So we cannot set proxy.UseDefaultCredentials = true property.
                if (NetworkConfigurationManager.UseCredentials)
                {
                    proxy.Credentials = new NetworkCredential(NetworkConfigurationManager.Username, NetworkConfigurationManager.Password);
                }
            }
            else if (NetworkConfigurationManager.ProxyType == ProxyType.Custom)
            {
                proxy = new WebProxy(NetworkConfigurationManager.ProxyServer, NetworkConfigurationManager.ProxyPort);
                if (NetworkConfigurationManager.UseCredentials)
                {
                    proxy.Credentials = new NetworkCredential(NetworkConfigurationManager.Username, NetworkConfigurationManager.Password);
                }
                else
                {
                    ((WebProxy)proxy).UseDefaultCredentials = true;
                }
            }

            return proxy;
        }
    }
}