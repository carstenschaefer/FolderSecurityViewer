﻿// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
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

    public class ReportUser : BaseSection
    {
        public ReportUser(XElement element) : base(element, "User")
        {
            this.Init();
        }

        public int ScanLevel
        {
            get
            {
                XElement element = this.XElement.Element("ScanLevel");
                return element == null || string.IsNullOrEmpty(element.Value) ? 0 : int.Parse(element.Value);
            }
            set
            {
                XElement element = this.XElement.Element("ScanLevel");
                if (element == null)
                {
                    element = new XElement("ScanLevel");
                    this.XElement.Add(element);
                }

                element.Value = value.ToString();
            }
        }

        private void Init()
        {
            if (this.XElement.Element("ScanLevel") == null)
            {
                this.XElement.Add(new XElement("ScanLevel") { Value = 0.ToString() });
            }
        }
    }
}