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

    public class ReportTrusteeSettings : BaseSection
    {
        internal ReportTrusteeSettings(XElement element) : base(element, "Settings")
        {
            this.Init();
        }

        public bool ShowAcl
        {
            get => bool.Parse(this.XElement.Element("ShowAcl").Value);
            set => this.XElement.Element("ShowAcl").Value = value.ToString();
        }

        public bool ExcludeDisabledUsers
        {
            get => bool.Parse(this.XElement.Element("ExcludeDisabledUsers").Value);
            set => this.XElement.Element("ExcludeDisabledUsers").Value = value.ToString();
        }

        public int TrusteePageSize
        {
            get => int.Parse(this.XElement.Element("TrusteePageSize").Value);
            set => this.XElement.Element("TrusteePageSize").Value = value.ToString();
        }

        private void Init()
        {
            XElement showAcl = this.XElement.Element("ShowAcl");

            if (showAcl == null)
            {
                showAcl = new XElement("ShowAcl") { Value = true.ToString() };
                this.XElement.Add(showAcl);
            }

            XElement excludeDisabledUsers = this.XElement.Element("ExcludeDisabledUsers");

            if (excludeDisabledUsers == null)
            {
                excludeDisabledUsers = new XElement("ExcludeDisabledUsers") { Value = false.ToString() };
                this.XElement.Add(excludeDisabledUsers);
            }

            XElement trusteePageSize = this.XElement.Element("TrusteePageSize");

            if (trusteePageSize == null)
            {
                trusteePageSize = new XElement("TrusteePageSize") { Value = 30.ToString() };
                this.XElement.Add(trusteePageSize);
            }
        }
    }
}