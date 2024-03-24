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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Security;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using Configuration.Database;
    using Core;
    using Crypto.Abstractions;
    using Database;
    using Microsoft.Extensions.Logging;
    using Resources;

    public class DatabaseSqlServerViewModel : DatabaseTypeViewModel
    {
        private readonly ILogger<DatabaseSqlServerViewModel> logger;
        private bool _passwordChanged;
        private bool _testedOK;
        private bool authenticationMode;
        private string databaseName;
        private string dataSource;
        private SecureString password;
        private string userName;

        public DatabaseSqlServerViewModel(
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            ISecure secure,
            ILogger<DatabaseSqlServerViewModel> logger) : base(dispatcherService, dialogService, DatabaseProviders.SqlServer)
        {
            if (secure == null)
            {
                throw new ArgumentNullException(nameof(secure));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.DisplayName = SettingDatabaseResource.SqlDatabaseCaption;
            this.Config = new SqlServerConfiguration(secure);

            this.FillAuthenticationModes();

            async Task<bool> CanExecuteTestSqlServerConnection(object p)
            {
                bool result = !string.IsNullOrEmpty(this.DataSource) && !string.IsNullOrEmpty(this.DatabaseName);
                return await Task.FromResult(result);
            }

            this.TestCommand = new AsyncRelayCommand(this.TestSqlServerConnectionAsync, CanExecuteTestSqlServerConnection);
        }

        public IList<KeyValuePair<string, bool>> AuthenticationModes { get; private set; }

        public string DataSource
        {
            get => this.dataSource;
            set
            {
                this.dataSource = value;
                this.RaisePropertyChanged(() => this.DataSource);
            }
        }

        public string DatabaseName
        {
            get => this.databaseName;
            set
            {
                this.databaseName = value;
                this.RaisePropertyChanged(() => this.DatabaseName);
            }
        }

        public bool AuthenticationMode
        {
            get => this.authenticationMode;
            set
            {
                this.authenticationMode = value;
                this.RaisePropertyChanged(() => this.AuthenticationMode);

                if (this.authenticationMode)
                {
                    this.UserName = string.Empty;
                    this.Password?.Clear();
                }
            }
        }

        public string UserName
        {
            get => this.userName;
            set
            {
                this.userName = value;
                this.RaisePropertyChanged(() => this.UserName);
            }
        }

        public SecureString Password
        {
            get => this.password;
            set
            {
                if (this.password != value)
                {
                    this.password = value;
                    this.RaisePropertyChanged(() => this.Password);
                    this._passwordChanged = true;

                    if (!this.password.IsReadOnly())
                    {
                        this.password.MakeReadOnly();
                    }
                }
            }
        }

        public ICommand TestCommand { get; }

        private SqlServerConfiguration Config { get; set; }

        public bool TestedOK
        {
            get => this._testedOK;
            private set => this.Set(ref this._testedOK, value, nameof(this.TestedOK));
        }

        public override void SetConfig(BaseConfiguration config)
        {
            this.Config = config as SqlServerConfiguration;

            SqlServerConfiguration sqlServerConfiguration = this.Config;
            if (sqlServerConfiguration != null)
            {
                this.DataSource = sqlServerConfiguration.DataSource;
                this.DatabaseName = sqlServerConfiguration.Database;
                this.AuthenticationMode = sqlServerConfiguration.IntegratedSecurity;
                this.UserName = sqlServerConfiguration.UserID;
                this.Password = sqlServerConfiguration.Password;
            }
        }

        public override BaseConfiguration GetConfig()
        {
            if (!this.AuthenticationMode && (string.IsNullOrEmpty(this.UserName) || this.Password == null || this.Password.Length == 0))
            {
                this.DialogService.ShowMessage(SettingDatabaseResource.CredentialsRequired);
                return null;
            }

            this.Config.DataSource = this.DataSource;
            this.Config.Database = this.DatabaseName;
            this.Config.IntegratedSecurity = this.AuthenticationMode;
            this.Config.UserID = this.UserName;

            if (this._passwordChanged)
            {
                this.Config.Password = this.Password;
                this._passwordChanged = false;
            }

            return this.Config;
        }

        private async Task TestSqlServerConnectionAsync(object obj)
        {
            if (this.IsWorking)
            {
                return;
            }

            bool result = await this.TestRunAsync();

            if (result)
            {
                this.TestedOK = true;
                await Task.Delay(2000);
                this.TestedOK = false;
            }
        }

        private void FillAuthenticationModes()
        {
            this.AuthenticationModes = new List<KeyValuePair<string, bool>>(2)
            {
                new("Windows", true),
                new("Sql Server", false)
            };
        }

        private async Task<bool> TestRunAsync()
        {
            return await Task.Run(async () =>
            {
                this.DoProgress();

                try
                {
                    var databaseStrings = new Dictionary<string, object>(5)
                    {
                        { "Data Source", this.DataSource },
                        { "Initial Catalog", this.DatabaseName },
                        { "Integrated Security", this.AuthenticationMode },
                        { "User Id", this.UserName },
                        { "Password", this.Password }
                    };

                    this.DatabaseProvider.TestSqlServerConnectivity(databaseStrings);
                    this.StopProgress();

                    return true;
                }
                catch (SqlException ex)
                {
                    this.logger.LogError(ex, "The connection test has failed: {ErrorMessage}.", ex.Message);

                    const int SqlServerErrorLogonFailed = 18456;
                    if (ex.Number == SqlServerErrorLogonFailed)
                    {
                        // https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-18456-database-engine-error?view=sql-server-ver15
                        await this.DispatcherService.InvokeAsync(() => this.DialogService.ShowMessage(string.Format(ErrorResource.LoginFailedOrDbInvalid, this.UserName)));
                    }
                    else
                    {
                        var displayErrorMessage = $"The connection test has failed: {{ErrorMessage}}.{ex.Message}";
                        await this.DispatcherService.InvokeAsync(() => { this.DialogService.ShowMessage(displayErrorMessage); });
                    }
                }
                catch (Exception ex)
                {
                    const string errorMessage = "The connection test has failed due to an unhandled error.";
                    this.logger.LogError(ex, errorMessage);
                    this.DialogService.ShowMessage(errorMessage);
                }
                finally
                {
                    this.StopProgress();
                }

                return false;
            });
        }
    }
}