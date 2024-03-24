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

namespace FSV.ViewModel
{
    using System.Threading.Tasks;
    using Resources;

    public abstract class ReportViewModel : WorkspaceViewModel
    {
        public static readonly string PermissionReport = HomeResource.PermissionReportCaption;
        public static readonly string FolderReport = HomeResource.FolderReportCaption;
        public static readonly string OwnerReport = HomeResource.OwnerReportCaption;
        public static readonly string ShareReport = HomeResource.ShareReportCaption;
        public static readonly string UserReport = HomeResource.UserReportCaption;
        public static readonly string Templates = HomeResource.TemplatesCaption;

        private long rowCount;
        private string tipMessage;

        protected ReportViewModel()
        {
            this.Closable = true;
            this.Exportable = true;
        }

        public string ReportTypeCaption { get; protected set; }
        public string FolderPath { get; protected set; }
        public string Title { get; protected set; }

        public ReportType ReportType { get; protected set; }

        public bool Closable { get; protected set; }

        public bool Exportable { get; protected set; }

        public long RowCount
        {
            get => this.rowCount;
            protected set => this.Set(ref this.rowCount, value, nameof(this.RowCount));
        }

        public string TipMessage
        {
            get => this.tipMessage;
            private set => this.Set(ref this.tipMessage, value, nameof(this.TipMessage));
        }

        protected async void SetTipMessageAsync(string value)
        {
            if (this.tipMessage != value)
            {
                this.TipMessage = value;
                await Task.Delay(5000);
                this.TipMessage = null;
            }
        }
    }
}