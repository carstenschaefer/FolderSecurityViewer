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

namespace FSV.Configuration
{
    using System.Xml.Linq;

    public class BuiltInGroup
    {
        internal BuiltInGroup() : this(new XElement("Group"))
        {
        }

        internal BuiltInGroup(XElement element)
        {
            this.Element = element;
        }

        public string Name
        {
            get => this.Element.Attribute("Name").Value;
            set => this.Element.SetAttributeValue("Name", value);
        }

        public string Description
        {
            get => this.Element.Value;
            set => this.Element.Value = value;
        }

        public string Sid
        {
            get => this.Element.Attribute("Sid").Value;
            set => this.Element.SetAttributeValue("Sid", value);
        }

        public bool Excluded
        {
            get => bool.Parse(this.Element.Attribute("Excluded")?.Value ?? bool.TrueString);
            set => this.Element.SetAttributeValue("Excluded", value);
        }

        internal XElement Element { get; }
    }
}