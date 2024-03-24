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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using Configuration;
    using Configuration.Abstractions;
    using Configuration.Sections.ConfigXml;
    using Database.Models;

    public class GridMetadataModel
    {
        private readonly IConfigurationManager configurationManager;

        public GridMetadataModel(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public IReadOnlyDictionary<string, string> GetColumns<T>()
        {
            var dictionary = new Dictionary<string, string>();

            Type type = typeof(T);
            Type attributeType = typeof(MapAtttribute);

            ConfigRoot configRoot = this.configurationManager.ConfigRoot;
            Report rootReport = configRoot.Report;
            ReportTrustee reportTrustee = rootReport.Trustee;

            IEnumerable<ConfigItem> columns = reportTrustee.TrusteeGridColumns.Where(item => item.Selected);

            PropertyInfo[] properties = type.GetProperties();

            bool HasMapAttribute(PropertyInfo m)
            {
                return m.CustomAttributes.Any(n => n.AttributeType == attributeType);
            }

            foreach (PropertyInfo property in properties.Where(HasMapAttribute))
            {
                if (!(property.GetCustomAttributes(attributeType, true).FirstOrDefault() is MapAtttribute attribute))
                {
                    continue;
                }

                bool FilterColumnByName(ConfigItem m)
                {
                    return m.Name == attribute.Name;
                }

                ConfigItem column = columns.FirstOrDefault(FilterColumnByName);
                if (column == null)
                {
                    continue;
                }

                dictionary.Add(property.Name, column.DisplayName);
            }

            return new ReadOnlyDictionary<string, string>(dictionary);
        }
    }
}