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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml.Linq;

    public class TemplateCollection : Collection<Template>
    {
        public TemplateCollection(XElement root, IList<Template> templates) : base(templates)
        {
            this.Root = root;
        }

        internal TemplateCollection(XElement root) : this(root, new List<Template>(0))
        {
        }

        private XElement Root { get; }

        protected override void InsertItem(int index, Template item)
        {
            this.Root.Add(item.Element);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this.Root.Elements().ElementAt(index).Remove();
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, Template item)
        {
            this.Root.Elements().ElementAt(index).ReplaceWith(item.Element);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            this.Root.RemoveAll();
            base.ClearItems();
        }
    }
}