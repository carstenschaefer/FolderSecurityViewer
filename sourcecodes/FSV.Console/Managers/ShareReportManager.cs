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

namespace FSV.Console.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using Abstractions;
    using AdServices;
    using Configuration.Abstractions;
    using Crypto.Abstractions;
    using Exporter;
    using Microsoft.Extensions.Logging;
    using Properties;
    using Services;
    using ShareServices;
    using ShareServices.Abstractions;

    public class ShareReportManager : IReportManager
    {
        private readonly IDisplayService displayService;
        private readonly IExportBuilder exportBuilder;
        private readonly ILogger<ShareReportManager> logger;
        private readonly ISecure secure;
        private readonly IShareConfigurationManager shareConfigurationManager;

        private readonly ShareData shareData;

        public ShareReportManager(
            IShareConfigurationManager shareConfigurationManager,
            IExportBuilder exportBuilder,
            IDisplayService displayService,
            ISecure secure,
            ILogger<ShareReportManager> logger,
            ShareData shareData)
        {
            this.shareConfigurationManager = shareConfigurationManager ?? throw new ArgumentNullException(nameof(shareConfigurationManager));
            this.exportBuilder = exportBuilder ?? throw new ArgumentNullException(nameof(exportBuilder));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.secure = secure ?? throw new ArgumentNullException(nameof(secure));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.shareData = shareData ?? throw new ArgumentNullException(nameof(shareData));

            if (shareData.ExportType == ExportBuilder.Csv)
            {
                throw new InvalidExportTypeException(Resources.ShareReportExportCsvException);
            }
        }

        public async Task StartScanAndExportReportAsync()
        {
            this.displayService.ShowText(Resources.ShareReportScanningText, this.shareData.Server);

            try
            {
                using DataTable result = await this.GetShareReportTableAsync(this.shareData.Server);
                if (result == null)
                {
                    this.displayService.ShowError(Resources.ShareReportNotExportedText);
                    return;
                }

                this.displayService.ShowText(Resources.ExportingText);
                string filePath = await this.ExportToFileAsync(this.shareData.Server, this.shareData.ExportType, this.shareData.ExportPath, result);
                this.displayService.ShowInfo(Resources.ShareReportExportedText, filePath);
            }
            catch (ShareLibException ex)
            {
                this.logger.LogError(ex, "Failed to scan and export share report to file.");
                var message = $"{string.Format(Resources.SharedServerNotFoundError, this.shareData.Server)} See inner exception for further details.";
                throw new ReportManagerException(message, ex);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to scan and export share report to file.");
                throw new ReportManagerException($"{Resources.ShareReportException} See inner exception for further details.", ex);
            }
        }

        private async Task<string> ExportToFileAsync(string serverName, string exportType, string exportFile, DataTable shares)
        {
            IExport exporter = this.exportBuilder.Build(exportType, serverName, exportFile, serverName);
            return await exporter.ExportShareReportAsync(shares);
        }

        private async Task<DataTable> GetShareReportTableAsync(string serverName)
        {
            return await Task.Run(() =>
            {
                var adAuthentication = new AdAuthentication(this.secure);

                this.shareConfigurationManager.InitConfig();

                string userName = this.shareConfigurationManager.ConfigRoot.Credentials.UserName;

                Share shareScanner = null;

                if (!string.IsNullOrEmpty(userName))
                {
                    SecureString securedString = this.shareConfigurationManager.ConfigRoot.Credentials.GetPassword();
                    string password = Encoding.UTF8.GetString(this.secure.GetBytes(securedString).ToArray());

                    KeyValuePair<string, string> upn = adAuthentication.GetUserPrincipalName(userName);

                    shareScanner = new Share(upn.Key, upn.Value, password);
                }
                else
                {
                    shareScanner = new Share();
                }

                IList<Share> shares = shareScanner.GetOfServer(serverName);

                var dataTable = new DataTable();

                dataTable.Columns.Add("Name", typeof(string));
                dataTable.Columns.Add("Path", typeof(string));
                dataTable.Columns.Add("Description", typeof(string));
                dataTable.Columns.Add("MaxUsers", typeof(uint));
                dataTable.Columns.Add("ClientConnections", typeof(int));
                dataTable.Columns.Add("Trustees", typeof(DataTable));

                foreach (Share share in shares)
                {
                    Share shareDetail = shareScanner.Get(serverName, share.Name);
                    DataRow row = dataTable.NewRow();

                    row.SetField(0, share.Name);
                    row.SetField(1, shareDetail.Path);
                    row.SetField(2, shareDetail.Description);
                    row.SetField(3, shareDetail.MaxUsers);
                    row.SetField(4, shareDetail.ClientConnections);

                    var trusteesTable = new DataTable();
                    trusteesTable.Columns.Add("Name", typeof(string));
                    trusteesTable.Columns.Add("Permission", typeof(string));
                    trusteesTable.Columns.Add("SidType", typeof(int));
                    trusteesTable.Columns.Add("WellKnownSid", typeof(bool));

                    foreach (ShareTrustee trustee in shareDetail.Trustees)
                    {
                        DataRow trusteeRow = trusteesTable.NewRow();
                        trusteeRow.SetField(0, trustee.Name);
                        trusteeRow.SetField(1, $"{trustee.Permission.Access}, {trustee.Permission.Rights}");
                        trusteeRow.SetField(2, trustee.SidType);
                        trusteeRow.SetField(3, trustee.WellKnownSid);

                        trusteesTable.Rows.Add(trusteeRow);
                    }

                    row.SetField(5, trusteesTable);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            });
        }
    }
}