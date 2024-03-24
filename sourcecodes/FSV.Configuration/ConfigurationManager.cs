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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Abstractions;
    using FSV.Extensions.Logging.Abstractions;
    using Sections.ConfigXml;

    public sealed class ConfigurationManager : IConfigurationManager
    {
        private const string ConfigName = "Config.xml";

        private const string LogFolder = "Logs";

        private const string DefaultLogFileName = "Log.txt";

        private static readonly object LogResetEvent = new();

        private readonly IConfigurationPaths configurationPaths;

        private readonly EventHandlerList events = new();

        private readonly ILoggingLevelSwitchAdapter loggingLevelSwitchAdapter;

        private XDocument ConfigDoc;

        private XElement ConfigReport;

        private ConfigRoot configRoot;

        public ConfigurationManager(
            IConfigurationPaths configurationPaths,
            ILoggingLevelSwitchAdapter loggingLevelSwitchAdapter)
        {
            this.configurationPaths = configurationPaths ?? throw new ArgumentNullException(nameof(configurationPaths));
            this.loggingLevelSwitchAdapter = loggingLevelSwitchAdapter ?? throw new ArgumentNullException(nameof(loggingLevelSwitchAdapter));

            try
            {
                this.LogDirectory = configurationPaths.LogDirectory ?? throw new ArgumentException($"The {nameof(configurationPaths.LogDirectory)} property of the given {nameof(IConfigurationPaths)} object cannot be null.", nameof(this.configurationPaths));
                this.LogFile = DefaultLogFileName;
            }
            catch (ArgumentException e)
            {
                throw new ConfigurationException("The destination folder-path for log-files contains invalid characters, and this cannot be set. See inner exception for further details.", e);
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Cannot set the log-file due to an unhandled error. See inner exception for further details.", e);
            }
        }

        public string LogDirectory { get; }

        public string LogFile { get; }

        ConfigRoot IConfigurationManager.ConfigRoot => this.configRoot;

        public event EventHandler LogReset
        {
            add => this.events.AddHandler(LogResetEvent, value);
            remove => this.events.RemoveHandler(LogResetEvent, value);
        }

        public async Task CreateDefaultConfigFileAsync(bool overwrite)
        {
            try
            {
                string configFilePath = this.configurationPaths.GetUserFilePath(ConfigName);

                if (!overwrite)
                {
                    if (File.Exists(configFilePath))
                    {
                        this.ConfigDoc = XDocument.Load(configFilePath);

                        // Required to change an old config structure to new Report based structure for trustee settings.
                        // Report based structure has been added in version 1.5.0.
                        this.CreateReportElement();
                        this.configRoot = new ConfigRoot(this.ConfigDoc.Root);
                        await this.SaveAsync();
                    }
                    else
                    {
                        await this.CreateDefaultInstance();
                    }
                }
                else
                {
                    await this.CreateDefaultInstance();
                    this.InvokeLogReset(EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Failed to write default configuration file due to an unhandled error. See inner exception for further details.", e);
            }

            this.ToggleLogLevelSwitch();
        }

        public Task SaveAsync()
        {
            return Task.Run(() =>
            {
                string configFilePath = ConfigPath.GetUserFilePath(ConfigName);
                if (string.IsNullOrWhiteSpace(configFilePath))
                {
                    throw new ConfigurationException("Unable to obtain destination configuration file path.");
                }

                try
                {
                    this.ConfigDoc.Save(configFilePath);
                }
                catch (Exception e)
                {
                    throw new ConfigurationException("Failed to save the configuration file due to an unhandled error. See inner exception for further details.", e);
                }
            });
        }

        public void LogInitialSettings()
        {
            var builder = new StringBuilder();
            builder.Append("Application Started with following configuration:\r\n");
            builder.AppendFormat("ACL Visible: {0}\r\n", this.configRoot.Report.Trustee.Settings.ShowAcl);

            int scalLevel = this.configRoot.Report.Trustee.ScanLevel;
            builder.AppendFormat("Scan Level: {0}\r\n", scalLevel > 0 ? scalLevel.ToString() : "All");
            builder.AppendFormat("Number of rows in Permissions grid: {0}\r\n", this.configRoot.PageSize);

            builder.AppendLine("These properties are displayed in Permissions grid:");
            foreach (ConfigItem item in this.configRoot.Report.Trustee.TrusteeGridColumns)
            {
                builder.AppendFormat("\t{0}: {1} - {2}\r\n", item.Name, item.DisplayName, item.Selected ? "Visible" : "Hidden");
            }

            IEnumerable<string> exclusions = this.configRoot.Report.Trustee.ExclusionGroups.Select(m => m.Name);
            builder.AppendFormat("Exclusion Groups: {0}\r\n\r\n", !exclusions.Any() ? "None" : string.Join(", ", exclusions));

            builder.AppendLine("These translated items are displayed in Permissions grid:");
            foreach (ConfigItem item in this.configRoot.Report.Trustee.RightsTranslations)
            {
                builder.AppendFormat("\t{0}: {1} - {2}\r\n", item.Name, item.DisplayName, item.Selected ? "Visible" : "Hidden");
            }
        }

        private void ToggleLogLevelSwitch()
        {
            bool isLoggingConfigured = this.configRoot?.Logging ?? false;
            if (isLoggingConfigured)
            {
                this.loggingLevelSwitchAdapter.Enable();
            }
            else
            {
                this.loggingLevelSwitchAdapter.Disable();
            }
        }

        private void InvokeLogReset(EventArgs e)
        {
            var handler = this.events[LogResetEvent] as EventHandler;
            handler?.Invoke(this, e);
        }

        /// <summary>
        ///     Creates a new Report element if it doesn't exist in config file.
        /// </summary>
        /// <remarks>
        ///     Added in version 1.5.0, Report element is used to organize existing trustee view settings.
        ///     In older version, all trustee view settings were available under FolderSecurityViewer root element.
        ///     Report element has few child nodes - Permission, Folder, and User.
        ///     Report element has few child nodes - Permission, Folder, and User.
        /// </remarks>
        private void CreateReportElement()
        {
            this.ConfigReport = this.ConfigDoc.Root.Element("Report");

            if (this.ConfigReport == null)
            {
                XElement root = this.ConfigDoc.Root;

                this.ConfigReport = new XElement("Report");
                root.Add(this.ConfigReport);

                var reportTrustee = new XElement("Trustee"); // Older name retained for Permission settings.
                var reportFolder = new XElement("Folder");
                var reportUser = new XElement("User");

                this.ConfigReport.Add(reportTrustee);
                this.ConfigReport.Add(reportFolder);
                this.ConfigReport.Add(reportUser);

                XElement settings = root.Element("Settings");
                settings?.Remove();

                XElement exclusionGroups = root.Element("ExclusionGroups");
                exclusionGroups?.Remove();

                XElement trusteeGridColumns = root.Element("TrusteeGridColumns");
                trusteeGridColumns?.Remove();

                XElement rightsTranslations = root.Element("RightsTranslations");
                rightsTranslations?.Remove();

                XElement excludedBuiltInGroups = root.Element("ExcludedBuiltInGroups");
                excludedBuiltInGroups?.Remove();

                XElement scanLevel = root.Element("ScanLevel");
                scanLevel?.Remove();

                reportTrustee.Add(scanLevel ?? new XElement("ScanLevel"));
                reportTrustee.Add(exclusionGroups ?? new XElement("ExclusionGroups"));
                reportTrustee.Add(trusteeGridColumns);
                reportTrustee.Add(rightsTranslations);
                reportTrustee.Add(excludedBuiltInGroups);

                //In new configuration, FolderSecurityViewer\Settings\TrusteePageSize has been removed. It is replaced with FolderSecurityViewer\PageSize tag.
                XElement trusteePageSize = settings.Element("TrusteePageSize");

                this.ConfigDoc.Root.Add(new XElement("PageSize")
                {
                    Value = trusteePageSize.Value
                });

                trusteePageSize.Remove();

                string configFilePath = ConfigPath.GetUserFilePath(ConfigName);
                this.ConfigDoc.Save(configFilePath);
            }
        }

        private async Task CreateDefaultInstance()
        {
            try
            {
                Assembly assembly = typeof(ConfigurationManager).Assembly;
                using (Stream stream = assembly.GetManifestResourceStream("FSV.Configuration.Defaults.Config.xml"))
                {
                    this.ConfigDoc = XDocument.Load(stream);
                    this.configRoot = new ConfigRoot(this.ConfigDoc.Root);
                }

                await this.SaveAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Configuration File is corrupt: " + ex.Message);
            }
        }

        private static void FillDefaultBuiltInGroups(IList<BuiltInGroup> list)
        {
            Assembly assembly = typeof(ConfigurationManager).Assembly;
            using Stream stream = assembly.GetManifestResourceStream("FSV.Configuration.Defaults.BuiltInGroups.xml");
            XDocument xDoc = XDocument.Load(stream);
            if (xDoc.Root != null)
            {
                foreach (XElement item in xDoc.Root.Elements())
                {
                    list.Add(new BuiltInGroup
                    {
                        Sid = item.Attribute("Sid").Value,
                        Name = item.Attribute("Name").Value,
                        Excluded = bool.Parse(item.Attribute("Excluded").Value),
                        Description = item.Value
                    });
                }
            }
        }
    }
}