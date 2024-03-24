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

namespace FSV.ViewModel.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using FSV.Templates.Abstractions;
    using Resources;
    using Templates;

    public class TemplateManager
    {
        private readonly ITemplateFile templateFile;
        private readonly ModelBuilder<Template, TemplateItemViewModel> templateModelBuilder;
        private ReadOnlyObservableCollection<TemplateItemViewModel> _readOnlyTemplates;
        private ObservableCollection<TemplateItemViewModel> _templates;
        private TemplateCollection _templatesFromFile;

        public TemplateManager(ITemplateFile templateFile, ModelBuilder<Template, TemplateItemViewModel> templateModelBuilder)
        {
            this.templateModelBuilder = templateModelBuilder ?? throw new ArgumentNullException(nameof(templateModelBuilder));
            this.templateFile = templateFile ?? throw new ArgumentNullException(nameof(templateFile));
        }

        internal async Task<ReadOnlyObservableCollection<TemplateItemViewModel>> GetTemplatesAsync()
        {
            if (this.templateFile.Inaccessible)
            {
                throw new TemplateFileCorruptException(string.Format(TemplateResource.TemplateFileCorruptError, this.templateFile.FilePath));
            }

            return await Task.Run(() =>
            {
                if (this._templates == null)
                {
                    this._templatesFromFile = this.templateFile.Templates;
                    if (this._templatesFromFile != null && !this.templateFile.Inaccessible)
                    {
                        this._templates = new ObservableCollection<TemplateItemViewModel>(this._templatesFromFile.Select(m => this.templateModelBuilder.Build(m)));
                        this._readOnlyTemplates = new ReadOnlyObservableCollection<TemplateItemViewModel>(this._templates);
                    }
                }

                return this._readOnlyTemplates;
            });
        }

        internal TemplateItemViewModel Add(TemplateNewViewModel templateViewModel)
        {
            Template template = this.templateFile.CreateTemplate();

            template.Name = templateViewModel.Name;
            template.Path = templateViewModel.Path;
            template.User = templateViewModel.User;
            template.Type = (int)templateViewModel.Type;

            this._templatesFromFile.Add(template);
            this.templateFile.Save();

            TemplateItemViewModel model = this.templateModelBuilder.Build(template);
            this._templates.Add(model);

            return model;
        }

        internal void Remove(TemplateItemViewModel model)
        {
            this._templatesFromFile.Remove(model.Template);
            this.templateFile.Save();
            this._templates.Remove(model);
        }

        internal void Remove(IEnumerable<TemplateItemViewModel> models)
        {
            foreach (TemplateItemViewModel model in models.ToArray())
            {
                this._templatesFromFile.Remove(model.Template);
                this._templates.Remove(model);
            }

            this.templateFile.Save();
        }

        internal TemplateItemViewModel Update(TemplateEditViewModel templateEditViewModel)
        {
            TemplateItemViewModel templateItem = this._templates.FirstOrDefault(m => m.Id == templateEditViewModel.Id) ??
                                                 throw new ArgumentException(ErrorResource.InvalidTemplate, nameof(templateEditViewModel));
            Template template = templateItem.Template;

            template.Name = templateEditViewModel.Name;
            template.Path = templateEditViewModel.Path;
            template.User = templateEditViewModel.User;
            template.Type = (int)templateEditViewModel.Type;

            this.templateFile.Save();

            int index = this._templates.IndexOf(templateItem);
            this._templates[index] = this.templateModelBuilder.Build(template);

            return this._templates[index];
        }
    }
}