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
    using System.Xml.Linq;
    using Extensions;

    public class ConfigRoot : BaseSection
    {
        private XElement culture;
        private XElement logging;
        private XElement pageSize;
        private XElement settingsLocked;
        private XElement showTourPopupAtStart;

        public ConfigRoot(XElement element) : base(element, "FolderSecurityViewer")
        {
            this.Init();
            this.Report = new Report(element.Element("Report"));
        }

        public Report Report { get; }

        public bool Logging
        {
            get => this.logging.BoolValue();
            set => this.logging.Value = value.ToString();
        }

        public int PageSize
        {
            get => this.pageSize.IntValue();
            set => this.pageSize.Value = value.ToString();
        }

        public bool SettingLocked
        {
            get => this.settingsLocked.BoolValue();
            set => this.settingsLocked.Value = value.ToString();
        }

        public bool ShowTourPopupAtStart
        {
            get => this.showTourPopupAtStart.BoolValue(true);
            set => this.showTourPopupAtStart.Value = value.ToString();
        }

        public string Culture
        {
            get => this.culture.Value;
            set => this.culture.Value = value;
        }

        private void Init()
        {
            void ConfigureDefaultCulture()
            {
                const string CultureElementName = "Culture";
                this.culture = CreateConfigurationElement(this.XElement, CultureElementName, string.Empty);
            }

            void EnableStartupTour(bool tourEnabled)
            {
                const string TourStartElementName = "TourAtStart";
                this.showTourPopupAtStart = CreateConfigurationElement(this.XElement, TourStartElementName, tourEnabled);
            }

            void LockSettingsUI(bool locked)
            {
                const string SettingsUILockedElementName = "SettingsUILocked";
                this.settingsLocked = CreateConfigurationElement(this.XElement, SettingsUILockedElementName, locked);
            }

            void SetPageSize(int value)
            {
                const string PageSizeElementName = "PageSize";
                this.pageSize = CreateConfigurationElement(this.XElement, PageSizeElementName, value);
            }

            void CreateDefaultLoggingConfiguration(bool enableLogging = true)
            {
                const string LoggingElementName = "Logging";
                this.logging = CreateConfigurationElement(this.XElement, LoggingElementName, enableLogging);
            }

            static XElement CreateConfigurationElement(XContainer target, string name, object value)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
                }

                XElement element = target.Element(name);
                if (element != null)
                {
                    return element;
                }

                element = new XElement(name, value);
                target.Add(element);

                return element;
            }

            const int defaultPageSize = 30;
            SetPageSize(defaultPageSize);

            const bool lockSettingsUi = false;
            LockSettingsUI(lockSettingsUi);

            CreateDefaultLoggingConfiguration();

            const bool enableTour = true;
            EnableStartupTour(enableTour);

            ConfigureDefaultCulture();
        }
    }
}