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

namespace FSV.Database
{
    using System;

    public class FsvDbException : ApplicationException
    {
        internal FsvDbException(DbErrors errorCode, Exception innerException) : base(innerException.Message, innerException)
        {
            this.ErrorCode = errorCode;
        }

        public DbErrors ErrorCode { get; }
    }

    public enum DbErrors
    {
        Unknown = 0,

        /// <summary>
        ///     The data source of connection string is invalid.
        /// </summary>
        /// <remarks>SqlException number: -1</remarks>
        InvalidDataSource = -1,

        /// <summary>
        ///     The user id or password is incorrect. Occurs when integrated security is false.
        /// </summary>
        /// <remarks>SqlException number: 18456</remarks>
        LoginFailed = 18456,

        /// <summary>
        ///     Database doesn't exist.
        /// </summary>
        /// <remarks>SqlException number: 911</remarks>
        InvalidDatabase = 911,

        /// <summary>
        ///     Database doesn't exist.
        /// </summary>
        /// ///
        /// <remarks>SqlException number: 4060</remarks>
        DatabaseNotExist = 4060,

        /// <summary>
        ///     Database exists but user doesn't have access to it.
        /// </summary>
        /// ///
        /// <remarks>SqlException number: 916</remarks>
        DatabasePermissionDenied = 916,

        /// <summary>
        ///     User cannot create database or tables.
        /// </summary>
        /// ///
        /// <remarks>SqlException number: 262</remarks>
        ObjectCreationFailed = 262
    }
}