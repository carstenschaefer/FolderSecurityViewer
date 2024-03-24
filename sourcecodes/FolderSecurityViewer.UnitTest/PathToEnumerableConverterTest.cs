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

namespace FolderSecurityViewer.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;
    using Controls;
    using Xunit;

    public class PathToEnumerableConverterTest
    {
        [Fact]
        public void PathToEnumerableConverter_Convert_root_test()
        {
            // Arrange
            const string path = "root";

            // Act 
            IEnumerable<PathSelectorItem> result = PathToEnumerableConverter.Convert(path);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_path_test()
        {
            // Arrange
            const string path = @"root\path\to\folder";
            PathSelectorItem[] expected =
            {
                new(@"root\", @"root\"),
                new(@"root\path", "path"),
                new(@"root\path\to", "to"),
                new(@"root\path\to\folder", "folder")
            };

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.False(result.First().IsShareServer);
            Assert.Equal(expected, result, new PathSelectorItemEqualityComparer());
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_long_path_prefix_test()
        {
            // Arrange
            const string path = @"\\?\root\path\to\folder";
            PathSelectorItem[] expected =
            {
                new(@"\\?\root\", @"root\"),
                new(@"\\?\root\path", "path"),
                new(@"\\?\root\path\to", "to"),
                new(@"\\?\root\path\to\folder", "folder")
            };

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.False(result.First().IsShareServer);
            Assert.Equal(expected, result, new PathSelectorItemEqualityComparer());
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_unc_server_only_path_test()
        {
            // Arrange
            const string path = @"\\#server";

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_unc_path_test()
        {
            // Arrange
            const string path = @"\\#server\share\path\folder";
            PathSelectorItem[] expected =
            {
                new(@"\\#server", @"\\#server", true),
                new(@"\\#server\share", "share"),
                new(@"\\#server\share\path", "path"),
                new(@"\\#server\share\path\folder", "folder")
            };

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.True(result.First().IsShareServer);
            Assert.Equal(expected, result, new PathSelectorItemEqualityComparer());
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_unc_long_path_prefix_with_server_name_only_test()
        {
            // Arrange
            const string path = @"\\?\UNC\#server";

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void PathToEnumerableConverter_Convert_unc_long_path_prefix_test()
        {
            // Arrange
            const string server = @"\\?\UNC\#server";
            var path = @$"{server}\share\path\folder";
            PathSelectorItem[] expected =
            {
                new(@$"{server}", @"\\#server", true),
                new(@$"{server}\share", "share"),
                new(@$"{server}\share\path", "path"),
                new(@$"{server}\share\path\folder", "folder")
            };

            // Act 
            List<PathSelectorItem> result = PathToEnumerableConverter.Convert(path).ToList();

            // Assert
            Assert.True(result.First().IsShareServer);
            Assert.Equal(expected, result, new PathSelectorItemEqualityComparer());
        }
    }
}