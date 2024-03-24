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

namespace FSV.Configuration.Sections.ConfigXml
{
    using System.Xml.Linq;

    public class ReportFolder : BaseSection
    {
        internal ReportFolder(XElement element) : base(element, "Folder")
        {
            this.Init();
        }

        public bool Owner
        {
            get => bool.Parse(this.XElement.Element("Owner").Value);
            set => this.XElement.Element("Owner").Value = value.ToString();
        }

        public bool IncludeCurrentFolder
        {
            get => bool.Parse(this.XElement.Element("IncludeCurrentFolder").Value);
            set => this.XElement.Element("IncludeCurrentFolder").Value = value.ToString();
        }

        public bool IncludeHiddenFolder
        {
            get => bool.Parse(this.XElement.Element("IncludeHiddenFolders").Value);
            set => this.XElement.Element("IncludeHiddenFolders").Value = value.ToString();
        }

        public bool IncludeSubFolder
        {
            get => bool.Parse(this.XElement.Element("IncludeSubFolders").Value);
            set => this.XElement.Element("IncludeSubFolders").Value = value.ToString();
        }

        public bool IncludeFileCount
        {
            get => bool.Parse(this.XElement.Element("IncludeFileCount").Value);
            set => this.XElement.Element("IncludeFileCount").Value = value.ToString();
        }

        public bool IncludeSubFolderFileCount
        {
            get => bool.Parse(this.XElement.Element("IncludeSubFolderFileCount").Value);
            set => this.XElement.Element("IncludeSubFolderFileCount").Value = value.ToString();
        }

        private void Init()
        {
            if (this.XElement.Element("Owner") == null)
            {
                this.XElement.Add(new XElement("Owner") { Value = bool.TrueString });
            }

            if (this.XElement.Element("IncludeCurrentFolder") == null)
            {
                this.XElement.Add(new XElement("IncludeCurrentFolder") { Value = bool.TrueString });
            }

            if (this.XElement.Element("IncludeSubFolders") == null)
            {
                this.XElement.Add(new XElement("IncludeSubFolders") { Value = bool.TrueString });
            }

            if (this.XElement.Element("IncludeHiddenFolders") == null)
            {
                this.XElement.Add(new XElement("IncludeHiddenFolders") { Value = bool.TrueString });
            }

            if (this.XElement.Element("IncludeFileCount") == null)
            {
                this.XElement.Add(new XElement("IncludeFileCount") { Value = bool.TrueString });
            }

            if (this.XElement.Element("IncludeSubFolderFileCount") == null)
            {
                this.XElement.Add(new XElement("IncludeSubFolderFileCount") { Value = bool.TrueString });
            }
        }
    }
}