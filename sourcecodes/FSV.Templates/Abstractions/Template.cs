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

namespace FSV.Templates.Abstractions
{
    using System;
    using System.Xml.Linq;

    public class Template : IEquatable<Template>
    {
        private string id;
        private string name;
        private string path;
        private int type;
        private string user;

        public Template()
        {
            this.Element = new XElement("Template");
            this.Id = Guid.NewGuid().ToString();
        }

        public Template(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (element.Name != "Template")
            {
                throw new ArgumentException($"Invalid xml element. Expected name Template, found {element.Name}.", nameof(element));
            }

            this.Element = element;

            if (element.Attribute("id") == null)
            {
                this.Id = Guid.NewGuid().ToString();
            }
        }

        internal Template(string name, int type, string path, string user = "") : this()
        {
            this.Name = name;
            this.Type = type;
            this.User = user;
            this.Id = Guid.NewGuid().ToString();
        }

        internal XElement Element { get; }

        public int Type
        {
            get
            {
                string attribute = this.Element.Attribute("type")?.Value;
                if (string.IsNullOrWhiteSpace(attribute) == false && int.TryParse(attribute, out this.type))
                {
                    return this.type;
                }

                const int defaultType = 1;
                return defaultType;
            }
            set
            {
                this.type = value;
                this.Element.SetAttributeValue("type", value);
            }
        }

        public string Name
        {
            get
            {
                string value = this.Element.Attribute("name")?.Value;
                this.name = value ?? string.Empty;
                return this.name;
            }
            set
            {
                this.name = value;
                this.Element.SetAttributeValue("name", value);
            }
        }

        public string Path
        {
            get
            {
                this.path = this.Element.Value ?? string.Empty;
                return this.path;
            }
            set
            {
                this.path = value;
                this.Element.Value = this.path;
            }
        }

        public string User
        {
            get
            {
                string value = this.Element.Attribute("user")?.Value;
                this.user = value;
                return this.user ?? string.Empty;
            }
            set
            {
                this.user = value;
                this.Element.SetAttributeValue("user", value);
            }
        }

        public string Id
        {
            get
            {
                string value = this.Element.Attribute("id")?.Value;
                this.id = value;
                return value;
            }
            private set
            {
                this.id = value;
                this.Element.SetAttributeValue("id", value);
            }
        }

        public bool Equals(Template other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.id == other.id && this.user == other.user && this.name == other.name && this.path == other.path && this.type == other.type && Equals(this.Element, other.Element);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((Template)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.id != null ? this.id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (this.name != null ? this.name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.path != null ? this.path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.type;
                hashCode = (hashCode * 397) ^ (this.Element != null ? this.Element.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}