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
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml.Linq;

    public class ExcludedBuiltInGroupList : Collection<BuiltInGroup>
    {
        private readonly XElement _parent;

        internal ExcludedBuiltInGroupList(XElement parent)
        {
            this._parent = parent ?? throw new ArgumentNullException(nameof(parent));

            foreach (XElement item in parent.Elements())
            {
                this.Items.Add(new BuiltInGroup(item));
            }
        }

        protected override void InsertItem(int index, BuiltInGroup item)
        {
            base.InsertItem(index, item);
            this._parent.Add(item.Element);
        }

        protected override void SetItem(int index, BuiltInGroup item)
        {
            base.SetItem(index, item);
            XElement element = this._parent.Elements().Where((el, idx) => idx == index).FirstOrDefault();
            element?.ReplaceWith(item.Element);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            XElement element = this._parent.Elements().Where((el, idx) => idx == index).FirstOrDefault();
            element?.Remove();
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            this._parent.RemoveAll();
        }
    }
}