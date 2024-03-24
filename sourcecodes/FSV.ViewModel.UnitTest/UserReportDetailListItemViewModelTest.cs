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
    public class UserReportDetailListItemViewModelTest
    {
        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_true_if_the_wrapped_report_details_is_same_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail();

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var other = new UserReportDetailListItemViewModel(reportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_false_if_the_wrapped_report_details_has_different_domain_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail
            {
                Domain = "domain"
            };

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var otherReportDetail = new UserPermissionReportDetail
            {
                Domain = "other-domain"
            };

            var other = new UserReportDetailListItemViewModel(otherReportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_false_if_the_wrapped_report_details_has_different_permissions_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail
            {
                Permissions = "permission"
            };

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var otherReportDetail = new UserPermissionReportDetail
            {
                Permissions = "different-permissions"
            };

            var other = new UserReportDetailListItemViewModel(otherReportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_false_if_the_wrapped_report_details_has_different_complete_name_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail
            {
                CompleteName = "complete-name"
            };

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var otherReportDetail = new UserPermissionReportDetail
            {
                CompleteName = "different-complete-name"
            };

            var other = new UserReportDetailListItemViewModel(otherReportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_false_if_the_wrapped_report_details_has_different_folder_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail
            {
                SubFolder = "folder"
            };

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var otherReportDetail = new UserPermissionReportDetail
            {
                SubFolder = "other-folder"
            };

            var other = new UserReportDetailListItemViewModel(otherReportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void UserReportDetailListItemViewModel_Equals_returns_false_if_the_wrapped_report_details_has_different_originating_group_Test()
        {
            // Arrange
            var reportDetail = new UserPermissionReportDetail
            {
                SubFolder = "originating-group"
            };

            const bool encrypted = false;

            var sut = new UserReportDetailListItemViewModel(reportDetail, encrypted);
            var otherReportDetail = new UserPermissionReportDetail
            {
                SubFolder = "other-originating-group"
            };

            var other = new UserReportDetailListItemViewModel(otherReportDetail, encrypted);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsFalse(actual);
        }
    }
}