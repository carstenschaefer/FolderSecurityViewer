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
    using System.ComponentModel;
    using System.IO;
    using System.Xml.Linq;
    using Abstractions;
    using Crypto.Abstractions;
    using Database;

    public sealed class DatabaseConfigurationManager : IDisposable, IDatabaseConfigurationManager
    {
        private const string CONFIG_FILE_NAME = "Database.xml";

        private static XDocument configDoc;
        private static XElement configConnectionString;
        private static XElement configProvider;
        private static XElement configRoot;

        private static BaseConfiguration configBase;
        private static XElement configEncrypted;
        private static readonly object ConfigurationChangedEvent = new();
        private readonly EventHandlerList events = new();

        private readonly ISecure secure;

        public DatabaseConfigurationManager(ISecure secure)
        {
            this.secure = secure ?? throw new ArgumentNullException(nameof(secure));
        }

        public BaseConfiguration Config
        {
            get => configBase;
            private set
            {
                configBase = value;
                this.InvokeConfigurationChangedEvent();
            }
        }

        public event EventHandler<DatabaseConfigurationChangedEventArgs> ConfigChanged
        {
            add => this.events.AddHandler(ConfigurationChangedEvent, value);
            remove => this.events.RemoveHandler(ConfigurationChangedEvent, value);
        }

        public void InitializeConfiguration()
        {
            string file = ConfigPath.GetUserFilePath(CONFIG_FILE_NAME);

            if (File.Exists(file))
            {
                configDoc = XDocument.Load(file);
                configRoot = configDoc.Root;

                configProvider = configRoot.Element("Provider");
                configConnectionString = configRoot.Element("ConnectionString");
                configEncrypted = configRoot.Element("Encrypted");

                ProtectedConnectionString protectedConnectionString = this.GetProtectedConnectionString(configConnectionString.Value);
                this.Config = this.GetConfiguration(configProvider.Value, protectedConnectionString);
                this.Config.Encrypted = Convert.ToBoolean(configEncrypted?.Value ?? bool.FalseString);
            }
            else
            {
                this.CreateDefaultInstance();
            }
        }

        public void Save(BaseConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            configProvider.Value = config.DatabaseProvider.Name;
            configConnectionString.Value = config.GetProtectedConnectionString()?.ToString() ?? string.Empty;

            if (configEncrypted == null)
            {
                configEncrypted = new XElement("Encrypted", config.Encrypted);
                configRoot.Add(configEncrypted);
            }
            else
            {
                configEncrypted.Value = config.Encrypted.ToString();
            }

            configDoc.Save(ConfigPath.GetUserFilePath(CONFIG_FILE_NAME));

            this.Config = config;
        }

        public void Dispose()
        {
            this.events?.Dispose();
        }

        private void InvokeConfigurationChangedEvent()
        {
            var handler = this.events[ConfigurationChangedEvent] as EventHandler<EventArgs>;
            handler?.Invoke(null, new DatabaseConfigurationChangedEventArgs(this.Config));
        }

        private void CreateDefaultInstance()
        {
            configRoot = new XElement("Database");

            configProvider = new XElement("Provider", DatabaseProviders.None.Name);
            configConnectionString = new XElement("ConnectionString");
            configEncrypted = new XElement("Encrypted", false);

            configRoot.Add(configProvider);
            configRoot.Add(configConnectionString);
            configRoot.Add(configEncrypted);

            configDoc = new XDocument();
            configDoc.Add(configRoot);

            this.Save(new NoneConfiguration());
        }

        private BaseConfiguration GetConfiguration(string providerName, ProtectedConnectionString connectionString)
        {
            if (DatabaseProvider.TryParse(providerName, out DatabaseProvider dbType))
            {
                if (dbType == DatabaseProviders.SQLite)
                {
                    return new SQLiteConfiguration(connectionString);
                }

                if (dbType == DatabaseProviders.SqlServer)
                {
                    return new SqlServerConfiguration(connectionString, this.secure);
                }
            }

            return new NoneConfiguration();
        }

        private ProtectedConnectionString GetProtectedConnectionString(string encryptedConnectionString)
        {
            return string.IsNullOrWhiteSpace(encryptedConnectionString) ? null : new ProtectedConnectionString(encryptedConnectionString);
        }
    }
}