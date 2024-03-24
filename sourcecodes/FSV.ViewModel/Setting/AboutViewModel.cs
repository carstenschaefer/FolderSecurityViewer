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
    using System.Reflection;
    using System.Text;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using Abstractions;
    using Resources;

    public class AboutViewModel : SettingWorkspaceViewModel
    {
        public AboutViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService) : base(dispatcherService, dialogService)
        {
            this.DisplayName = ConfigurationResource.AboutCaption;

            this.AboutText = ConfigurationResource.AboutText;

            var xamlBuild = new StringBuilder();
            xamlBuild.Append("<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            xamlBuild.Append("<Paragraph>");
            xamlBuild.Append(ConfigurationResource.AboutText);
            xamlBuild.Append("</Paragraph>");
            xamlBuild.Append("<Paragraph>");
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            xamlBuild.Append(string.Format(ConfigurationResource.Version, version.Major, version.Minor, version.Build));
            xamlBuild.Append("</Paragraph>");
            xamlBuild.Append("</FlowDocument>");

            this.AboutDocument = XamlReader.Parse(xamlBuild.ToString()) as FlowDocument;
        }

        public string AboutText { get; }

        public FlowDocument AboutDocument { get; }
    }
}