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
    using System.Data.Entity;
    using Abstractions;
    using Repositories;

    public class UnitOfWork : IUnitOfWork
    {
        private readonly FsvDataContext _context;
        private readonly DbContextTransaction _transaction;
        private PermissionDetailReportRepository _permissionDetailReportRepository;
        private PermissionReportRepository _permissionReportRepository;
        private UserPermissionDetailReportRepository _userPermissionDetailReportRepository;
        private UserPermissionReportRepository _userPermissionReportRepository;

        internal UnitOfWork(FsvDataContext fsvDataContext)
        {
            this._context = fsvDataContext ?? throw new ArgumentNullException(nameof(fsvDataContext));
            this._transaction = this._context?.Database.BeginTransaction();
        }

        public IPermissionReportRepository PermissionReportRepository
            =>
                this._permissionReportRepository ??= new PermissionReportRepository(this._context);

        public IPermissionDetailReportRepository PermissionDetailReportRepository
            =>
                this._permissionDetailReportRepository ??= new PermissionDetailReportRepository(this._context);

        public IUserPermissionReportRepository UserPermissionReportRepository
            =>
                this._userPermissionReportRepository ??= new UserPermissionReportRepository(this._context);

        public IUserPermissionDetailReportRepository UserPermissionDetailReportRepository
            =>
                this._userPermissionDetailReportRepository ??= new UserPermissionDetailReportRepository(this._context);

        public void SaveChanges()
        {
            this._context?.SaveChanges();
        }

        public void Commit()
        {
            this._transaction?.Commit();
        }

        public void Rollback()
        {
            this._transaction?.Rollback();
        }

        public void Dispose()
        {
            if (this._context == null)
            {
                return;
            }

            this._context.Database.CurrentTransaction?.Dispose();
            this._context.Dispose();
        }
    }
}