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

namespace FSV.Templates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;
    using Abstractions;

    internal class TemplateFile : ITemplateFile
    {
        public readonly string FileName = "Templates.xml";
        private TemplateCollection _templates;

        private XDocument xDoc;
        private XElement xRoot;

        public TemplateFile(string directory)
        {
            this.FilePath = Path.Combine(directory, this.FileName);
            this.TryFileAccess();
        }

        public string FilePath { get; }

        public TemplateCollection Templates
        {
            get
            {
                if (this.Inaccessible)
                {
                    return null;
                }

                return this._templates ?? (this._templates = this.GetTemplates());
            }
        }

        public bool Inaccessible { get; private set; }

        public void Save()
        {
            if (!this.Inaccessible)
            {
                this.xDoc.Save(this.FilePath);
            }
        }

        public Template CreateTemplate()
        {
            return new Template();
        }

        private TemplateCollection GetTemplates()
        {
            IEnumerable<XElement> elements = this.xRoot.Elements();
            var templateList = new List<Template>();

            foreach (XElement element in elements)
            {
                if (element.Name == "Template")
                {
                    templateList.Add(new Template(element));
                }
                else
                {
                    element.Remove();
                }
            }

            return new TemplateCollection(this.xRoot, templateList);
        }

        private void TryFileAccess()
        {
            try
            {
                if (!File.Exists(this.FilePath))
                {
                    this.CreateFile();
                }
                else
                {
                    this.xDoc = XDocument.Load(this.FilePath);
                    this.xRoot = this.xDoc.Root;
                    if (this.xRoot.Name != "Templates")
                    {
                        this.CreateFile();
                    }
                }
            }
            catch (Exception)
            {
                this.Inaccessible = true;
            }
        }

        private void CreateFile()
        {
            this.xDoc = new XDocument();
            this.xRoot = new XElement("Templates");
            this.xDoc.Add(this.xRoot);
            this.xDoc.Save(this.FilePath);
        }
    }
}