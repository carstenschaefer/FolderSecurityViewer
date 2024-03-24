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
    using System;
    using FolderTree;
    using Home;
    using JetBrains.Annotations;
    using Xunit;

    public class FolderTreeItemViewModelTest
    {
        [Fact]
        public void FolderTreeItemViewModel_null_equality_test()
        {
            // Arrange
            FolderModel model = this.GetFolerModel("Folder", @"path\to\some\folder");

            var sut = new FolderTreeItemViewModel(model);

            // Act
            bool result = sut.Equals(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FolderTreeItemViewModel_same_reference_equality_test()
        {
            // Arrange
            FolderModel model = this.GetFolerModel("Folder", @"path\to\some\folder");

            var sut = new FolderTreeItemViewModel(model);
            FolderTreeItemViewModel reference = sut;

            // Act
            bool result = sut.Equals(reference);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FolderTreeItemViewModel_path_equality_test()
        {
            // Arrange
            FolderModel modelOne = this.GetFolerModel("Folder", @"path\to\some\folder");
            FolderModel modelTwo = this.GetFolerModel("Folder", @"path\to\some\folder");

            var sut = new FolderTreeItemViewModel(modelOne);
            var other = new FolderTreeItemViewModel(modelTwo);

            // Act
            bool result = sut.Equals(other);

            // Assert
            Assert.True(result);
        }

        private FolderModel GetFolerModel([NotNull] string name, [NotNull] string path)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            return new FolderModel
            {
                Name = name,
                Path = path
            };
        }
    }
}