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
    using System;
    using System.Globalization;
    using System.Threading;
    using Abstractions;

    internal class CultureManager : ICultureManager
    {
        private const string SystemName = "system";

        private readonly IConfigurationManager configurationManager;

        public CultureManager(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public void InitializeCulture()
        {
            string culture = this.configurationManager.ConfigRoot.Culture ?? SystemName;

            if (!string.IsNullOrEmpty(culture) && !culture.Equals(SystemName, StringComparison.InvariantCultureIgnoreCase))
            {
                var cultureInfo = new CultureInfo(culture);

                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
            }
        }
    }
}