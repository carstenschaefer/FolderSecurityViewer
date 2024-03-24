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

namespace FSV.Business.UnitTest
{
    using System;
    using Xunit;

    public class PathUtilTest
    {
        [Theory]
        [InlineData("/var/lib", "/var/lib", 0)]
        [InlineData("/var/lib", "/var/lib/", 0)]
        [InlineData("/var/lib", "/var/lib/depth-1", 1)]
        [InlineData("/var/lib", "/var/lib/depth-1/depth-2", 2)]
        [InlineData("#:/test", "#:/test/depth-1/depth-2", 2)]
        [InlineData("#:/test", @"#:\test\depth-1\depth-2", 2)]
        [InlineData(@"\\share01\s$\data", @"\\share01\s$\data", 0)]
        [InlineData(@"\\share01\s$\data", @"\\share01\s$\data\depth-1", 1)]
        public void PathUtil_GetPathDepth_Test(string mainDirectory, string subDirectory, int expectedDepth)
        {
            // Arrange

            // Act
            int actual = PathUtil.GetPathDepth(mainDirectory, subDirectory);

            // Assert
            Assert.Equal(expectedDepth, actual);
        }

        [Theory]
        [InlineData("/var/lib", "/usr/lib")]
        [InlineData("/var/lib/depth-1", "/var/lib")]
        [InlineData("/var/lib/depth-1/depth-2", "/var/lib")]
        public void PathUtil_GetPathDepth_throws_on_mismatching_paths_Test(string mainDirectory, string subDirectory)
        {
            // Arrange

            // Act
            void Act()
            {
                PathUtil.GetPathDepth(mainDirectory, subDirectory);
            }

            // Assert
            Assert.Throws<InvalidOperationException>(Act);
        }
    }
}