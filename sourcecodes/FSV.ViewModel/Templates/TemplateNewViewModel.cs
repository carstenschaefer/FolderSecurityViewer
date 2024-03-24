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
    using System.Collections.Generic;
    using System.Linq;
    using Resources;

    public class TemplateNewViewModel : WorkspaceViewModel
    {
        private string _name;
        private string _path;
        private TemplateType _type = TemplateType.PermissionReport;
        private string _user;

        internal TemplateNewViewModel()
        {
            this.DisplayName = TemplateResource.NewTemplateTitle;
            this.Type = TemplateType.PermissionReport;

            this.AddTypes();
        }

        internal TemplateNewViewModel(TemplateType templateType, bool hideOtherTypes)
        {
            this.DisplayName = TemplateResource.NewTemplateTitle;
            this.Type = templateType;

            this.AddTypes(hideOtherTypes);
        }

        public string Name
        {
            get => this._name;
            set => this.Set(ref this._name, value, nameof(this.Name));
        }

        public string User
        {
            get => this._user;
            set => this.Set(ref this._user, value, nameof(this.User));
        }

        public string Path
        {
            get => this._path;
            set => this.Set(ref this._path, value, nameof(this.Path));
        }

        public TemplateType Type
        {
            get => this._type;
            set
            {
                this.Set(ref this._type, value, nameof(this.Type));
                if (this._type == TemplateType.PermissionReport)
                {
                    this.User = string.Empty;
                }
            }
        }

        public IDictionary<TemplateType, string> Types { get; private set; }

        protected override bool CanOk(object p)
        {
            return !string.IsNullOrEmpty(this.Name) && !string.IsNullOrEmpty(this.Path) && (this.Type == TemplateType.PermissionReport || !string.IsNullOrEmpty(this.User));
        }

        private void AddTypes(bool hideOtherTypes = false)
        {
            var types = new Dictionary<TemplateType, string>(3)
            {
                { TemplateType.PermissionReport, HomeResource.PermissionReportCaption },
                { TemplateType.OwnerReport, HomeResource.OwnerReportCaption },
                { TemplateType.UserReport, HomeResource.UserReportCaption }
            };

            this.Types = hideOtherTypes ? types.Where(m => m.Key == this.Type).ToDictionary(m => m.Key, n => n.Value) : types;
        }
    }
}