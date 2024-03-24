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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdServices.Abstractions;
    using Configuration.Abstractions;
    using Core;
    using Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    public class ConfigurationViewModel : SettingWorkspaceViewModel
    {
        private readonly IActiveDirectoryFinderFactory _adFinderFactory;
        private readonly CultureInfo _systemCulture;
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;

        private readonly IDispatcherService dispatcherService;
        private readonly ILogger<ConfigurationViewModel> logger;
        private readonly ILoggingLevelSwitchAdapter loggingLevelSwitchAdapter;
        private ICommand _cleanADCacheCommand;
        private CultureInfoViewModel _culture;
        private bool _cultureChanged;

        private ICommand _restoreDefaultCommand;

        public ConfigurationViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IConfigurationManager configurationManager,
            IActiveDirectoryFinderFactory finderFactory,
            ILogger<ConfigurationViewModel> logger,
            ILoggingLevelSwitchAdapter loggingLevelSwitchAdapter,
            CultureInfo systemCulture) :
            base(dispatcherService, dialogService, true, true)
        {
            this.DisplayName = ConfigurationResource.ConfigurationCaption;
            this.IsEnabled = !configurationManager.ConfigRoot.SettingLocked;

            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this._systemCulture = systemCulture;
            this._adFinderFactory = finderFactory;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.loggingLevelSwitchAdapter = loggingLevelSwitchAdapter ?? throw new ArgumentNullException(nameof(loggingLevelSwitchAdapter));

            this.FillCultures(systemCulture);
        }

        public IList<CultureInfoViewModel> Cultures { get; } = new List<CultureInfoViewModel>(2);

        public bool EnableLog
        {
            get => this.configurationManager.ConfigRoot.Logging;
            set
            {
                this.configurationManager.ConfigRoot.Logging = value;
                this.RaisePropertyChanged(nameof(this.EnableLog));
            }
        }

        public int PageSize
        {
            get => this.configurationManager.ConfigRoot.PageSize;
            set
            {
                this.configurationManager.ConfigRoot.PageSize = value;
                this.RaisePropertyChanged(nameof(this.PageSize));
            }
        }

        public CultureInfoViewModel Culture
        {
            get => this._culture;
            set
            {
                if (value.Culture != this._culture.Culture)
                {
                    this.DoSet(ref this._culture, value, nameof(this.Culture));
                    this._cultureChanged = true;
                }
            }
        }

        public ICommand RestoreDefaultCommand => this._restoreDefaultCommand ??= new AsyncRelayCommand(this.RestoreToDefault);
        public ICommand CleanADCacheCommand => this._cleanADCacheCommand ??= new AsyncRelayCommand(this.CleanActiveDirectoryCacheAsync);

        private async Task CleanActiveDirectoryCacheAsync(object obj)
        {
            this.DoProgress();
            await Task.Run(this._adFinderFactory.Clear);
            await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(CommonResource.ADCacheClearedText));
            this.StopProgress();
        }

        private async Task RestoreToDefault(object obj)
        {
            await this.configurationManager.CreateDefaultConfigFileAsync(true);

            this.RaisePropertyChanged(nameof(this.EnableLog));
            this.RaisePropertyChanged(nameof(this.PageSize));

            this.ResetCulture();
        }

        internal override async Task<bool> Save()
        {
            if (this.configurationManager.ConfigRoot.Logging)
            {
                this.logger.LogInformation("Log Enabled");
                this.loggingLevelSwitchAdapter.Enable();
            }
            else
            {
                this.logger.LogInformation("Log is disabled");
                this.loggingLevelSwitchAdapter.Disable();
            }

            this.DoProgress();

            var result = false;

            try
            {
                this.configurationManager.ConfigRoot.Culture = this.Culture.Culture;
                await this.configurationManager.SaveAsync();

                result = true;
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to save configuration due to an unhandled error.";
                this.logger.LogError(ex, errorMessage);
                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(errorMessage));
                return false;
            }
            finally
            {
                this.StopProgress();
            }

            if (this._cultureChanged)
            {
                string culture = this.Culture.Culture == CultureInfoViewModel.SystemName ? this._systemCulture.Name : this.Culture.Culture;

                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

                await this.dispatcherService.InvokeAsync(() => this.dialogService.ShowMessage(ConfigurationResource.RestartRequired));

                this._cultureChanged = false;
            }

            return result;
        }

        private void FillCultures(CultureInfo systemCulture)
        {
            this.Cultures.Add(new CultureInfoViewModel(string.Format(CultureResource.SystemCaption, systemCulture.DisplayName), CultureInfoViewModel.SystemName));
            this.Cultures.Add(new CultureInfoViewModel(CultureResource.EnglishCaption, "en"));
            this.Cultures.Add(new CultureInfoViewModel(CultureResource.GermanCaption, "de-de"));

            string configCulture = this.configurationManager.ConfigRoot.Culture.ToLower();
            this._culture = this.Cultures.FirstOrDefault(m => m.Culture == configCulture) ?? this.Cultures.FirstOrDefault();
        }

        private void ResetCulture()
        {
            string configCulture = this.configurationManager.ConfigRoot.Culture.ToLower();
            this._culture = this.Cultures.FirstOrDefault(m => m.Culture == configCulture) ?? this.Cultures.FirstOrDefault();

            this.RaisePropertyChanged(nameof(this.Culture));
        }
    }
}