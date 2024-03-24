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
    using System.ComponentModel;
    using Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SortOrderTest
    {
        [TestMethod]
        [DataRow(ListSortDirection.Ascending)]
        [DataRow(ListSortDirection.Descending)]
        public void SortOrder_From_ToSortDirection_Test(ListSortDirection direction)
        {
            // Arrange
            SortOrder sut = SortOrder.From(direction);

            // Act
            ListSortDirection actual = sut.ToSortDirection();

            // Assert
            Assert.AreEqual(direction, actual);
        }

        [TestMethod]
        [DataRow(ListSortDirection.Ascending, "ASC")]
        [DataRow(ListSortDirection.Descending, "DESC")]
        [DataRow((ListSortDirection)5, "")]
        public void SortOrder_From_ToShortString_Test(ListSortDirection direction, string expected)
        {
            // Arrange
            SortOrder sut = SortOrder.From(direction);

            // Act
            string actual = sut.ToShortString();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(ListSortDirection.Ascending)]
        [DataRow(ListSortDirection.Descending)]
        public void SortOrder_From_Equals_returns_true_if_both_instances_represent_same_direction_Test(ListSortDirection direction)
        {
            // Arrange
            SortOrder sut = SortOrder.From(direction);
            SortOrder other = SortOrder.From(direction);

            // Act
            bool actual = sut.Equals(other);

            // Assert
            Assert.IsTrue(actual);
        }
    }
}