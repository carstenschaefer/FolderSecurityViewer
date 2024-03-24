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

    public class Report : BaseSection
    {
        internal Report() : base(new XElement("Report"))
        {
            this.Init();
        }

        internal Report(XElement element) : base(element, nameof(Report))
        {
            this.Init();
        }

        public ReportFolder Folder { get; private set; }

        public ReportTrustee Trustee { get; private set; }

        public ReportUser User { get; private set; }

        private void Init()
        {
            this.Trustee = new ReportTrustee(this.XElement.Element("Trustee"));
            this.Folder = new ReportFolder(this.XElement.Element("Folder"));

            XElement userElement = this.XElement.Element("User");

            if (userElement == null)
            {
                userElement = new XElement("User");
                this.XElement.Add(userElement);
            }

            this.User = new ReportUser(this.XElement.Element("User"));
        }
    }
}