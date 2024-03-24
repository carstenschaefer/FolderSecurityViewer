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
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Core;
    using Events;
    using Managers;
    using Microsoft.Extensions.Logging;
    using Prism.Events;
    using Resources;

    public class TemplateContainerViewModel : WorkspaceViewModel
    {
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcherService;

        private readonly IEventAggregator eventAggregator;
        private readonly ILogger<TemplateContainerViewModel> logger;
        private readonly TemplateManager manager;
        private readonly object syncObject = new();

        private ICommand _addTemplateCommand;

        private string _error;
        private bool _initialized;
        private ICommand _removeTemplateCommand;

        public TemplateContainerViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            TemplateManager templateManager,
            ILogger<TemplateContainerViewModel> logger)
        {
            this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            this.dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.manager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ICommand AddTemplateCommand => this._addTemplateCommand ??= new RelayCommand(this.ShowTemplateEditor, this.CanCreate);
        public ICommand RemoveTemplateCommand => this._removeTemplateCommand ??= new RelayCommand(this.RemoveTemplates, this.CanRemove);

        public string Error
        {
            get => this._error;
            private set => this.Set(ref this._error, value, nameof(this.Error));
        }

        public ObservableCollection<TemplateItemViewModel> Templates { get; } = new();

        internal bool ShowTemplateEditor(TemplateType templateType, string path, string user = null, bool hideOtherTypes = false)
        {
            var model = new TemplateNewViewModel(templateType, hideOtherTypes)
            {
                Path = path,
                User = user
            };

            void HandleAddViewModelClosing(object s, CloseCommandEventArgs e)
            {
                if (e.IsOK)
                {
                    TemplateItemViewModel templateItem = this.manager.Add(model);

                    lock (this.syncObject)
                    {
                        this.Templates.Add(templateItem);
                    }
                }

                model.Closing -= HandleAddViewModelClosing;
            }

            model.Closing += HandleAddViewModelClosing;

            return this.dialogService.ShowDialog(model);
        }

        internal void ClearSelections()
        {
            if (this.Templates == null)
            {
                return;
            }

            lock (this.syncObject)
            {
                foreach (TemplateItemViewModel item in this.Templates.Where(item => item.Selected))
                {
                    item.Selected = false;
                }
            }
        }

        public override async Task InitializeAsync()
        {
            if (this._initialized)
            {
                return;
            }

            this.eventAggregator.GetEvent<RemoveTemplateEvent>().Subscribe(this.RemoveTemplate);
            this.eventAggregator.GetEvent<EditTemplateEvent>().Subscribe(this.EditTemplate);

            this.DoProgress();

            try
            {
                ReadOnlyObservableCollection<TemplateItemViewModel> templates = await this.manager.GetTemplatesAsync();

                lock (this.syncObject)
                {
                    this.Templates.Clear();
                    foreach (TemplateItemViewModel template in templates)
                    {
                        this.Templates.Add(template);
                    }
                }
            }
            catch (TemplateFileCorruptException e)
            {
                const string errorMessage = "Failed to initialize templates due to an unhandled error.";
                this.logger.LogError(e, errorMessage);
                this.Error = $"{errorMessage} {e.Message}";
            }
            finally
            {
                await this.dispatcherService.InvokeAsync(this.StopProgress);
                this._initialized = true;
            }
        }

        private void EditTemplate(TemplateItemViewModel item)
        {
            var model = new TemplateEditViewModel(item);

            void HandleTemplateEditViewModelClosing(object sender, CloseCommandEventArgs e)
            {
                if (e.IsOK)
                {
                    TemplateItemViewModel updatedModel = this.manager.Update(model);
                    lock (this.syncObject)
                    {
                        var item = this.Templates
                            .Select((model, idx) => new { model, index = idx })
                            .FirstOrDefault(m => m.model.Id == updatedModel.Id);

                        if (item != null && item.index >= 0)
                        {
                            this.Templates.RemoveAt(item.index);
                            this.Templates.Insert(item.index, updatedModel);
                        }
                    }
                }

                model.Closing -= HandleTemplateEditViewModelClosing;
            }

            model.Closing += HandleTemplateEditViewModelClosing;

            this.dialogService.ShowDialog(model);
        }

        private void ShowTemplateEditor(object obj)
        {
            this.ShowTemplateEditor(TemplateType.PermissionReport, null);
        }

        private void RemoveTemplates(object obj)
        {
            if (this.dialogService.Ask(TemplateResource.AskRemovetAllText))
            {
                lock (this.syncObject)
                {
                    List<TemplateItemViewModel> itemsToRemove = this.Templates.Where(m => m.Selected).ToList();
                    this.manager.Remove(itemsToRemove);

                    foreach (TemplateItemViewModel item in itemsToRemove)
                    {
                        this.Templates.Remove(item);
                    }
                }
            }
        }

        private void RemoveTemplate(TemplateItemViewModel model)
        {
            if (this.dialogService.Ask(string.Format(TemplateResource.AskRemovetText, model.Name)))
            {
                this.manager.Remove(model);

                lock (this.syncObject)
                {
                    var item = this.Templates
                        .Select((model, idx) => new { model, index = idx })
                        .FirstOrDefault(m => m.model.Id == model.Id);

                    if (item != null && item.index >= 0)
                    {
                        this.Templates.RemoveAt(item.index);
                    }
                }
            }
        }

        private bool CanRemove(object _)
        {
            bool canRemove;
            lock (this.syncObject)
            {
                canRemove = this.Templates.Any(m => m.Selected);
            }

            return canRemove;
        }

        private bool CanCreate(object _)
        {
            return !this.IsWorking;
        }
    }
}