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
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Resources;

    public class ReportFolderViewModel : ReportWorkspaceViewModel
    {
        private readonly Report _report;

        public ReportFolderViewModel(
            IConfigurationManager configurationManager)
        {
            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }

            this.DisplayName = ConfigurationFolderResource.ReportFolderCaption;
            this.IsEnabled = !configurationManager.ConfigRoot.SettingLocked;

            this._report = configurationManager.ConfigRoot.Report;
        }

        public bool Owner
        {
            get => this._report.Folder.Owner;
            set
            {
                this._report.Folder.Owner = value;
                this.RaisePropertyChanged(nameof(this.Owner));
            }
        }

        public bool IncludeCurrentFolder
        {
            get => this._report.Folder.IncludeCurrentFolder;
            set
            {
                this._report.Folder.IncludeCurrentFolder = value;
                this.RaisePropertyChanged(nameof(this.IncludeCurrentFolder));
            }
        }

        public bool IncludeSubFolders
        {
            get => this._report.Folder.IncludeSubFolder;
            set
            {
                this._report.Folder.IncludeSubFolder = value;
                this.RaisePropertyChanged(nameof(this.IncludeSubFolders));
                if (!value)
                {
                    this.IncludeHiddenFolders = false;
                }
            }
        }

        public bool IncludeHiddenFolders
        {
            get => this._report.Folder.IncludeHiddenFolder;
            set
            {
                this._report.Folder.IncludeHiddenFolder = value;
                this.RaisePropertyChanged(nameof(this.IncludeHiddenFolders));
            }
        }

        public bool FileCountAndSize
        {
            get => this._report.Folder.IncludeFileCount;
            set
            {
                this._report.Folder.IncludeFileCount = value;
                this.RaisePropertyChanged(nameof(this.FileCountAndSize));
            }
        }

        public bool SubFolderFileCountAndSize
        {
            get => this._report.Folder.IncludeSubFolderFileCount;
            set
            {
                this._report.Folder.IncludeSubFolderFileCount = value;
                this.RaisePropertyChanged(nameof(this.SubFolderFileCountAndSize));
            }
        }
    }
}