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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Abstractions;
    using Home;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Xunit;

    public class FolderTreeItemSelectorTest
    {
        [Fact]
        public async Task FolderTreeItemSelector_GetNextAsync_returns_expected_folder_test()
        {
            // Arrange
            string[] keys = { "s", "s", "e", "a" };
            string[] expectedFolders = { "C\\Softwares", "C\\Shares", null, "C\\Shares\\ABC" };

            List<string> actualFolders = new();

            List<FolderTreeItemViewModel> folders = this.GetFolders().ToList();
            FolderTreeItemViewModel selectedFolder = this.GetSelectedFolder(folders);

            IFolderTreeItemSelector sut = this.GetFolderTreeItemSelector(folders);

            // Act
            foreach (string key in keys)
            {
                FolderTreeItemViewModel resultFolder = await sut.GetNextAsync(key, selectedFolder);
                if (resultFolder is not null)
                {
                    selectedFolder = resultFolder;
                }

                actualFolders.Add(resultFolder?.Path);
            }

            Assert.Equal(expectedFolders, actualFolders, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FolderTreeItemSelector_GetNextAsync_null_is_returned_from_folderFactory_test()
        {
            // Arrange
            List<FolderTreeItemViewModel> folders = this.GetFolders().ToList();
            FolderTreeItemViewModel selectedFolder = this.GetSelectedFolder(folders);

            IFolderTreeItemSelector sut = this.GetFolderTreeItemSelector(null);

            // Act
            FolderTreeItemViewModel result = await sut.GetNextAsync("s", selectedFolder);

            Assert.Equal(selectedFolder, result);
        }

        [Fact]
        public async Task FolderTreeItemSelector_GetNextAsync_null_is_returned_when_key_not_found_test()
        {
            // Arrange
            List<FolderTreeItemViewModel> folders = this.GetFolders().ToList();
            FolderTreeItemViewModel selectedFolder = this.GetSelectedFolder(folders);

            IFolderTreeItemSelector sut = this.GetFolderTreeItemSelector(folders);

            // Act
            FolderTreeItemViewModel result = await sut.GetNextAsync("master", selectedFolder);

            Assert.Null(result);
        }

        private IFolderTreeItemSelector GetFolderTreeItemSelector(IEnumerable<FolderTreeItemViewModel> folders)
        {
            var services = new ServiceCollection();

            IServiceProvider serviceProvider = services.UseViewModels().BuildServiceProvider();

            var factory = serviceProvider.GetRequiredService<Func<Func<IEnumerable<FolderTreeItemViewModel>>, IFolderTreeItemSelector>>();
            return factory(() => folders);
        }

        private IEnumerable<FolderTreeItemViewModel> GetFolders()
        {
            string content = this.GetJsonResource("Folders");
            var models = JsonConvert.DeserializeObject<IEnumerable<ImmutableFolderModel>>(content);

            return this.GetFolderTreeItems(models);
        }

        private IEnumerable<FolderTreeItemViewModel> GetFolderTreeItems(IEnumerable<ImmutableFolderModel> models)
        {
            static FolderTreeItemViewModel GetFolderTreeItem(ImmutableFolderModel immutableFolder)
            {
                FolderTreeItemViewModel treeItem = new(immutableFolder.AsFolderModel())
                {
                    Selected = immutableFolder.Selected,
                    Expanded = immutableFolder.Expanded
                };

                foreach (ImmutableFolderModel item in immutableFolder.Items)
                {
                    treeItem.Items.Add(GetFolderTreeItem(item));
                }

                return treeItem;
            }

            return models.Select(m => GetFolderTreeItem(m));
        }

        private FolderTreeItemViewModel GetSelectedFolder(IEnumerable<FolderTreeItemViewModel> folders)
        {
            foreach (FolderTreeItemViewModel item in folders)
            {
                if (item.Selected)
                {
                    return item;
                }

                if (item.Items.Count > 0)
                {
                    return this.GetSelectedFolder(item.Items);
                }
            }

            return null;
        }

        private string GetJsonResource(string name)
        {
            string namespaceName = this.GetType().Namespace + ".Json.";
            var assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(namespaceName + name + ".json");
            using StreamReader reader = new(stream);

            return reader.ReadToEnd();
        }
    }
}