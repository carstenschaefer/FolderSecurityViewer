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

namespace FSV.ViewModel.UnitTest
{
    using Database.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using UserReport;

    [TestClass]
    public class SavedUserReportListItemViewModelTest
    {
        [TestMethod]
        public void SavedUserReportListItemViewModel_Equals_null_test()
        {
            SavedUserReportListItemViewModel reportFirst = this.GetReportViewModel(1, "Test\\Ritesh", "some/first/path");

            bool result = reportFirst.Equals(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SavedUserReportListItemViewModel_Equals_test()
        {
            SavedUserReportListItemViewModel reportFirst = this.GetReportViewModel(1, "Test\\Ritesh", "some/first/path");
            SavedUserReportListItemViewModel reportCompare = this.GetReportViewModel(2, "Test\\Ritesh", "some/first/path");

            bool result = reportFirst.Equals(reportCompare);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SavedUserReportListItemViewModel_Not_Equals_test()
        {
            SavedUserReportListItemViewModel reportFirst = this.GetReportViewModel(1, "Test\\Ritesh", "some/first/path");
            SavedUserReportListItemViewModel reportCompare = this.GetReportViewModel(2, "Test\\Other", "some/first/path");

            bool result = reportFirst.Equals(reportCompare);

            Assert.IsFalse(result);
        }

        private SavedUserReportListItemViewModel GetReportViewModel(int id, string reportUser, string path)
        {
            var report = new UserPermissionReport
            {
                Id = id,
                ReportUser = reportUser,
                Folder = path
            };

            return new SavedUserReportListItemViewModel(report, null);
        }
    }
}