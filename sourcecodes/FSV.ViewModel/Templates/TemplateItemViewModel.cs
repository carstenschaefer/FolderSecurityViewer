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

namespace FSV.ViewModel.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Events;
    using FSV.Templates.Abstractions;
    using Owner;
    using Passables;
    using Permission;
    using Prism.Events;
    using Resources;
    using UserReport;

    public class TemplateItemViewModel : ViewModelBase
    {
        private readonly TemplateStartedEvent _templateStartedEvent;
        private readonly IEventAggregator eventAggregator;

        private readonly INavigationService navigationService;

        private ICommand _editCommand;
        private ICommand _removeCommand;

        private bool _selected;

        public TemplateItemViewModel(INavigationService navigationService, IEventAggregator eventAggregator, Template template)
        {
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.Template = template ?? throw new ArgumentNullException(nameof(template));

            this._templateStartedEvent = this.eventAggregator.GetEvent<TemplateStartedEvent>();

            this.SetPropsBasedOnTemplateType();
        }

        private static IDictionary<TemplateType, string> Types => new Dictionary<TemplateType, string>(3)
        {
            { TemplateType.PermissionReport, HomeResource.PermissionReportCaption },
            { TemplateType.OwnerReport, HomeResource.OwnerReportCaption },
            { TemplateType.UserReport, HomeResource.UserReportCaption }
        };

        internal Template Template { get; }

        public string ToolTip { get; private set; }

        public string Id => this.Template.Id;

        public string Name => this.Template.Name;

        public string User => this.Template.User;

        public string Path => this.Template.Path;

        public TemplateType Type => (TemplateType)this.Template.Type;

        public string TypeName => Types[(TemplateType)this.Template.Type];

        public bool Selected
        {
            get => this._selected;
            set => this.Set(ref this._selected, value, nameof(this.Selected));
        }

        public ICommand StartCommand { get; private set; }
        public ICommand EditCommand => this._editCommand ??= new RelayCommand(this.Edit);
        public ICommand RemoveCommand => this._removeCommand ??= new RelayCommand(this.Remove);

        private void PermissionReportCommand(object p)
        {
            this.navigationService.NavigateWithAsync<PermissionsViewModel>(this.Path);
            this._templateStartedEvent.Publish(this);
        }

        private void OwnerReportCommand(object p)
        {
            this.navigationService.NavigateWithAsync<OwnerReportViewModel>(new UserPath(this.User, this.Path));
            this._templateStartedEvent.Publish(this);
        }

        private void UserReportCommand(object p)
        {
            this.navigationService.NavigateWithAsync<UserReportViewModel>(new UserPath(this.User, this.Path));
            this._templateStartedEvent.Publish(this);
        }

        private void SetPropsBasedOnTemplateType()
        {
            switch (this.Type)
            {
                case TemplateType.PermissionReport:
                    this.StartCommand = new RelayCommand(this.PermissionReportCommand);
                    this.ToolTip = TemplateResource.StartPermissionReportText;
                    break;
                case TemplateType.OwnerReport:
                    this.StartCommand = new RelayCommand(this.OwnerReportCommand);
                    this.ToolTip = TemplateResource.StartOwnerReportText;
                    break;
                case TemplateType.UserReport:
                    this.StartCommand = new RelayCommand(this.UserReportCommand);
                    this.ToolTip = TemplateResource.StartUserReportText;
                    break;
                default:
                    this.StartCommand = null;
                    break;
            }
        }

        private void Edit(object obj)
        {
            this.eventAggregator.GetEvent<EditTemplateEvent>().Publish(this);
        }

        private void Remove(object obj)
        {
            this.eventAggregator.GetEvent<RemoveTemplateEvent>().Publish(this);
        }
    }
}