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
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Business.Worker;
    using Core;
    using Resources;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class SearchExclusionGroupViewModel : SettingWorkspaceViewModel
    {
        private readonly IDialogService dialogService;
        private readonly Func<GroupSearcher> groupSearcherFactory;
        private ICommand _addCommand;
        private ICommand _clearCommand;

        private string _groupName;

        private bool _groupSearcherInitFailed;
        private string _noResultsText;

        private ICommand _searchCommand;
        private GroupSearcher groupSearcher;

        public SearchExclusionGroupViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            Func<GroupSearcher> groupSearcherFactory) : base(dispatcherService, dialogService)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.groupSearcherFactory = groupSearcherFactory ?? throw new ArgumentNullException(nameof(groupSearcherFactory));
            this.DisplayName = ConfigurationResource.ExclusionGroupSearchTitle;

            this.ResultGroups = new ObservableCollection<GroupListItem>();
        }

        public string GroupName
        {
            get => this._groupName;
            set
            {
                this._groupName = value;
                this.RaisePropertyChanged(() => this.GroupName);
            }
        }

        public string NoResultsText
        {
            get => this._noResultsText;
            set
            {
                this._noResultsText = value;
                this.RaisePropertyChanged(() => this.NoResultsText);
            }
        }

        public IList<GroupListItem> ResultGroups { get; }

        public ICommand SearchCommand
        {
            get
            {
                if (this._searchCommand == null)
                {
                    this._searchCommand = new RelayCommand(this.Search, p => !this._groupSearcherInitFailed && !string.IsNullOrEmpty(this.GroupName) && !this.IsWorking);
                }

                return this._searchCommand;
            }
        }

        public ICommand ClearCommand
        {
            get
            {
                void Execute(object p)
                {
                    this.ResultGroups.Clear();
                    this.GroupName = string.Empty;
                }

                return this._clearCommand ??= new RelayCommand(Execute, p => this.ResultGroups.Count > 0);
            }
        }

        public ICommand AddCommand
        {
            get
            {
                void Execute(object p)
                {
                    if (this.ExclusionGroupsAdded == null)
                    {
                        return;
                    }

                    this.ExclusionGroupsAdded(this, new EventArgs());
                    this.CancelCommand.Execute(null);
                }

                return this._addCommand ??= new RelayCommand(Execute, p => this.ResultGroups.Any(m => m.Selected));
            }
        }

        public Task RunWorkerAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                this.DoProgress();
                try
                {
                    this.groupSearcher = this.groupSearcherFactory(); // new GroupSearcher(new Searcher(this.log));
                    this.groupSearcher.RunWorkerCompleted += this.GroupSearchCompleted;
                }
                catch (Exception ex)
                {
                    this._groupSearcherInitFailed = true;
                    this.dialogService.ShowMessage(ex.Message);
                }

                this.StopProgress();
            }, cancellationToken);
        }

        public event EventHandler ExclusionGroupsAdded;

        private void Search(object obj)
        {
            this.ResultGroups.Clear();
            this.DoProgress();
            this.groupSearcher.RunWorkerAsync(this.GroupName);
        }

        private void GroupSearchCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var groups = e.Result as List<string>;
            if (groups.Count > 0)
            {
                foreach (string item in groups)
                {
                    this.ResultGroups.Add(new GroupListItem { Name = item });
                }
            }
            else
            {
                Task.Run(() =>
                {
                    this.NoResultsText = string.Format(ConfigurationResource.ExclusionGroupNoResultText, this.GroupName);
                    Thread.Sleep(5000);
                    this.NoResultsText = string.Empty;
                });
            }

            this.StopProgress();
        }
    }
}