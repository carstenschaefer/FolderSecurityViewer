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
    using System.Collections.Generic;
    using System.Data;

    public class LimitDataViewModel : ReportViewModel
    {
        protected DataTable LimitData(DataTable dataTable)
        {
            return dataTable;
        }

        protected IEnumerable<T> LimitData<T>(IEnumerable<T> list)
        {
            return list;
        }

        /// <summary>
        ///     Limit records fetched from database. Use LimitTotalRows method to make final row count.
        /// </summary>
        protected IEnumerable<T> LimitData<T>(IEnumerable<T> list, int pageNo, int pageSize, int totalPages, int totalRows)
        {
            return list;
        }

        protected int LimitTotalRows(int total)
        {
            return total;
        }
    }
}