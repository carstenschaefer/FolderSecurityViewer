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
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Windows.Input;
    using Abstractions;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ReportPermissionViewModel : ReportWorkspaceViewModel
    {
        private readonly ModelBuilder<BuiltInGroupListViewModel> builtInGroupListViewModelBuilder;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly IConfigurationManager configurationManager;
        private readonly IDialogService dialogService;
        private readonly ModelBuilder<SearchExclusionGroupViewModel> searchExclusionGroupViewModelBuilder;
        private ObservableCollection<ConfigItem> _adProperties;
        private BuiltInGroupListViewModel _builtInGroupsViewModel;
        private ObservableCollection<ConfigItem> _exclusionGroups;

        private SearchExclusionGroupViewModel _exclusionGroupViewModel;
        private ICommand _openBuiltInGroupsCommand;
        private ICommand _openTranslationCommand, _deleteTranslationCommand;
        private ICommand _searchExclusionGroupCommand, _deleteAdGroupCommand;
        private ConfigItem _selectedAdGroup;

        private ConfigItem _selectedTranslation;

        private ObservableCollection<ConfigItem> _translationItems;
        private bool disposed;

        public ReportPermissionViewModel(
            IDialogService dialogService,
            IConfigurationManager configurationManager,
            ModelBuilder<SearchExclusionGroupViewModel> searchExclusionGroupViewModelBuilder,
            ModelBuilder<BuiltInGroupListViewModel> builtInGroupListViewModelBuilder)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.searchExclusionGroupViewModelBuilder = searchExclusionGroupViewModelBuilder ?? throw new ArgumentNullException(nameof(searchExclusionGroupViewModelBuilder));
            this.builtInGroupListViewModelBuilder = builtInGroupListViewModelBuilder ?? throw new ArgumentNullException(nameof(builtInGroupListViewModelBuilder));
            this.DisplayName = ConfigurationResource.ReportPermissionCaption;

            this.LoadConfigurations();
            this.IsEnabled = !configurationManager.ConfigRoot.SettingLocked;
            this.configurationManager.LogReset += this.HandleConfigurationManagerLogReset;
        }

        public int ScanLevel
        {
            get => this.configurationManager.ConfigRoot.Report.Trustee.ScanLevel;
            set
            {
                this.configurationManager.ConfigRoot.Report.Trustee.ScanLevel = value;
                this.RaisePropertyChanged(nameof(this.ScanLevel));
            }
        }

        public bool ACLVisible
        {
            get => this.configurationManager.ConfigRoot.Report.Trustee.Settings.ShowAcl;
            set
            {
                this.configurationManager.ConfigRoot.Report.Trustee.Settings.ShowAcl = value;
                this.RaisePropertyChanged(() => this.ACLVisible);
            }
        }

        public bool ExcludeDisabledUsers
        {
            get => this.configurationManager.ConfigRoot.Report.Trustee.Settings.ExcludeDisabledUsers;
            set
            {
                this.configurationManager.ConfigRoot.Report.Trustee.Settings.ExcludeDisabledUsers = value;
                this.RaisePropertyChanged(() => this.ExcludeDisabledUsers);
            }
        }

        public ObservableCollection<ConfigItem> ADProperties
        {
            get => this._adProperties;
            private set => this.Set(ref this._adProperties, value, nameof(this.ADProperties));
        }

        public ObservableCollection<ConfigItem> ExclusionGroups
        {
            get => this._exclusionGroups;
            private set => this.Set(ref this._exclusionGroups, value, nameof(this.ExclusionGroups));
        }

        public ObservableCollection<ConfigItem> TranslationItems
        {
            get => this._translationItems;
            private set => this.Set(ref this._translationItems, value, nameof(this.TranslationItems));
        }

        public ConfigItem SelectedTranslation
        {
            get => this._selectedTranslation;
            set => this.Set(ref this._selectedTranslation, value, nameof(this.SelectedTranslation));
        }

        public ConfigItem SelectedADGroup
        {
            get => this._selectedAdGroup;
            set => this.Set(ref this._selectedAdGroup, value, nameof(this.SelectedADGroup));
        }

        public ICommand SearchExclusionGroupCommand => this._searchExclusionGroupCommand ??= new RelayCommand(this.SearchExclusionGroup);

        public ICommand OpenTranslationCommand => this._openTranslationCommand ??= new RelayCommand(this.OpenTranslation, this.CanOpenTranslation);

        public ICommand OpenBuiltInGroupsCommand => this._openBuiltInGroupsCommand ??= new RelayCommand(this.OpenBuiltInGroups);

        public ICommand DeleteTranslationCommand => this._deleteTranslationCommand ??= new RelayCommand(this.DeleteTranslation, p => this.SelectedTranslation != null);

        public ICommand DeleteADGroupCommand => this._deleteAdGroupCommand ??= new RelayCommand(this.DeleteAdGroup, p => this.SelectedADGroup != null);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !this.disposed)
            {
                this.configurationManager.LogReset -= this.HandleConfigurationManagerLogReset;
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.disposed = true;
            }
        }

        private void SearchExclusionGroup(object obj)
        {
            if (this._exclusionGroupViewModel == null)
            {
                this._exclusionGroupViewModel = this.searchExclusionGroupViewModelBuilder.Build();
                this._exclusionGroupViewModel.ExclusionGroupsAdded += this.ExclusionGroupsAdded;
                this._exclusionGroupViewModel.RunWorkerAsync(this.cancellationTokenSource.Token);
            }
            else
            {
                this._exclusionGroupViewModel.ClearCommand.Execute(null);
            }

            this.dialogService.ShowDialog(this._exclusionGroupViewModel);
        }

        private void OpenTranslation(object obj)
        {
            // doWhat denotes which action to take on command.
            // Value 1 asks to create a new entry, and 2 asks for editing selected entry.
            var doWhat = 1;
            if (obj != null)
            {
                doWhat = Convert.ToInt32(obj);
            }

            TranslationRightsViewModel viewModel;
            if (doWhat == 1)
            {
                viewModel = new TranslationRightsViewModel();
            }
            else
            {
                viewModel = new TranslationRightsViewModel(this.SelectedTranslation.Name, this.SelectedTranslation.DisplayName);
            }

            viewModel.ItemChanged += (s, e) =>
            {
                if (doWhat == 1)
                {
                    // Since a new item is created, it has to be added in the list.
                    this.TranslationItems.Add(viewModel.NewTranslatedItem);

                    // Apparently, ObservableCollection doesn't update list or enumerable passed in the constructor parameter.
                    // Make sure to add new item in ConfigurationManager as well.
                    this.configurationManager.ConfigRoot.Report.Trustee.RightsTranslations.Add(viewModel.NewTranslatedItem);
                }
                else
                {
                    // Simply changing the selected item does not work unless we implement NotifyPropertyChanged in properties of FSVPro.Configuration.ConfigItem class.
                    // Seek index of the selected item in TranslationItem
                    int index = this.TranslationItems.IndexOf(this.SelectedTranslation);
                    if (index >= 0)
                    {
                        // Index is appropriate. Now replace an item at that index with new item.
                        this.TranslationItems[index] = viewModel.NewTranslatedItem;

                        // Apparently, ObservableCollection doesn't update list or enumerable passed in its constructor parameter.
                        // Make sure to update item in ConfigurationManager as well.
                        this.configurationManager.ConfigRoot.Report.Trustee.RightsTranslations[index] = viewModel.NewTranslatedItem;
                    }
                }
            };

            // Trigger loading of view in modal window.
            this.dialogService.ShowDialog(viewModel);
        }

        private bool CanOpenTranslation(object p)
        {
            var doWhat = 1;
            if (p != null)
            {
                doWhat = Convert.ToInt32(p);
            }

            return doWhat == 1 || this.SelectedTranslation != null;
        }

        private void DeleteTranslation(object obj)
        {
            // Remove a selected item from ConfigurationManager as well.
            ConfigItem item = this.configurationManager.ConfigRoot.Report.Trustee.RightsTranslations.FirstOrDefault(m => m.Name.Equals(this.SelectedTranslation.Name));
            this.configurationManager.ConfigRoot.Report.Trustee.RightsTranslations.Remove(item);

            // Removes a selected item from collection.
            this.TranslationItems.Remove(this.SelectedTranslation);
        }

        private void OpenBuiltInGroups(object obj)
        {
            this._builtInGroupsViewModel ??= this.builtInGroupListViewModelBuilder.Build();
            this.dialogService.ShowDialog(this._builtInGroupsViewModel);
        }

        private void HandleConfigurationManagerLogReset(object sender, EventArgs e)
        {
            this.LoadConfigurations();
        }

        private void ExclusionGroupsAdded(object sender, EventArgs e)
        {
            IEnumerable<ExclusionGroupItem> selectedItems = from item in this._exclusionGroupViewModel.ResultGroups
                where item.Selected
                select new ExclusionGroupItem { Name = item.Name, Selected = item.Selected };

            foreach (ExclusionGroupItem item in selectedItems)
            {
                if (this.configurationManager.ConfigRoot.Report.Trustee.ExclusionGroups.FirstOrDefault(m => m.Name == item.Name) == null)
                {
                    this.ExclusionGroups.Add(item);
                    this.configurationManager.ConfigRoot.Report.Trustee.ExclusionGroups.Add(item);
                }
            }
        }

        private void DeleteAdGroup(object p)
        {
            ConfigItem item = this.configurationManager.ConfigRoot.Report.Trustee.ExclusionGroups.FirstOrDefault(m => m.Name.Equals(this.SelectedADGroup.Name));
            this.configurationManager.ConfigRoot.Report.Trustee.ExclusionGroups.Remove(item);

            // Removes a selected item from collection.
            this.ExclusionGroups.Remove(this.SelectedADGroup);
        }

        private void LoadConfigurations()
        {
            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportTrustee reportTrustee = rootReport.Trustee;

            this.ADProperties = new ObservableCollection<ConfigItem>(reportTrustee.TrusteeGridColumns);

            ConfigItem item1 = this.ADProperties.First(p => p.Name == "OriginatingGroup");
            ConfigItem item2 = this.ADProperties.First(p => p.Name == "Rights");

            this.ADProperties.Remove(item1);
            this.ADProperties.Remove(item2);

            this.ExclusionGroups = new ObservableCollection<ConfigItem>(reportTrustee.ExclusionGroups);

            this.TranslationItems = new ObservableCollection<ConfigItem>(reportTrustee.RightsTranslations);

            this._builtInGroupsViewModel = this.builtInGroupListViewModelBuilder.Build();
        }
    }
}