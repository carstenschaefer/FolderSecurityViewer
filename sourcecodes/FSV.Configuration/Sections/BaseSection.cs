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

namespace FSV.Configuration.Sections
{
    using System;
    using System.Xml.Linq;

    public abstract class BaseSection
    {
        protected BaseSection(XElement element)
        {
            string className = this.GetType().Name;
            if (!className.Equals(element.Name))
            {
                throw new ArgumentException(string.Format("Name of node {0} and class {1} do not match.", element.Name, className));
            }

            this.XElement = element;
        }

        protected BaseSection(XElement element, string expectedNodeName)
        {
            if (string.IsNullOrWhiteSpace(expectedNodeName))
            {
                throw new ArgumentNullException(nameof(expectedNodeName));
            }

            if (element != null && !expectedNodeName.Equals(element.Name.LocalName))
            {
                throw new ArgumentException(string.Format("{1} type does not map with node {0}. {2} is expected.", element.Name, this.GetType().FullName, expectedNodeName));
            }

            if (element == null)
            {
                this.XElement = new XElement(expectedNodeName);
            }
            else
            {
                this.XElement = element;
            }
        }

        internal XElement XElement { get; }
    }
}