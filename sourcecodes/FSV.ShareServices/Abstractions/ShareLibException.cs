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

namespace FSV.ShareServices.Abstractions
{
    using System;
    using System.ComponentModel;
    using System.Text;

    public class ShareLibException : Exception
    {
        private readonly string _message;
        private Exception _innerException;

        public ShareLibException(string message)
        {
            this._message = message;

            try
            {
                this.ErrorMessage = new Win32Exception(this.ErrorCode).Message;
            }
            catch
            {
                this.ErrorMessage = "Unknown error";
            }
        }

        public ShareLibException(string message, Exception innerException)
        {
            this._message = message;
            this._innerException = this.InnerException;

            try
            {
                this.ErrorMessage = new Win32Exception(this.ErrorCode).Message;
            }
            catch
            {
                this.ErrorMessage = "Unknown error";
            }
        }

        public ShareLibException(int errorCode, string message)
        {
            this.ErrorCode = errorCode;
            this._message = message;

            try
            {
                this.ErrorMessage = new Win32Exception(this.ErrorCode).Message;
            }
            catch
            {
                this.ErrorMessage = "Unknown error";
            }
        }

        public ShareLibException(int errorCode, string message, Exception innerException)
        {
            this.ErrorCode = errorCode;
            this._message = message;
            this._innerException = this.InnerException;

            try
            {
                this.ErrorMessage = new Win32Exception(this.ErrorCode).Message;
            }
            catch
            {
                this.ErrorMessage = "Unknown error";
            }
        }

        public int ErrorCode { get; }

        public string ErrorMessage { get; }

        public override string Message
        {
            get
            {
                if (this._message == null)
                {
                    return base.Message;
                }

                var sb = new StringBuilder(base.Message);
                sb.AppendFormat("{0}: {1}\n", this.ErrorCode, this.ErrorMessage);
                sb.AppendLine(this._message);
                return sb.ToString();
            }
        }
    }
}