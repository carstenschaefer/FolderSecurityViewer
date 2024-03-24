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
    using System.Threading.Tasks;
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AsyncRelayCommandTest
    {
        [TestMethod]
        public async Task AsyncRelayCommand_ExecuteAsync_Test()
        {
            // Arrange
            var executed = false;

            async Task Execute(object o)
            {
                executed = true;
                await Task.CompletedTask;
            }

            var sut = new AsyncRelayCommand(Execute);

            // Act
            await sut.ExecuteAsync(null);

            // Assert
            Assert.IsTrue(executed);
        }

        [TestMethod]
        public void AsyncRelayCommand_Execute_Test()
        {
            // Arrange
            var executed = false;

            async Task Execute(object o)
            {
                executed = true;
                await Task.CompletedTask;
            }

            var sut = new AsyncRelayCommand(Execute);

            // Act
            sut.Execute(null);

            // Assert
            Assert.IsTrue(executed);
        }
    }
}