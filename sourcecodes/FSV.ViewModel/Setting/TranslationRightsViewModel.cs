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

namespace FSV.ViewModel.Setting
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Windows.Input;
    using Configuration;
    using Configuration.Sections.ConfigXml;
    using Core;
    using Resources;

    public class TranslationRightsViewModel : WorkspaceViewModel
    {
        private string _setName;

        public TranslationRightsViewModel()
        {
            this.Initialize(null, null);
        }

        public TranslationRightsViewModel(string rightName, string rightDisplayName)
        {
            if (string.IsNullOrEmpty(rightName))
            {
                throw new ArgumentNullException(nameof(rightName));
            }

            if (string.IsNullOrEmpty(rightDisplayName))
            {
                throw new ArgumentNullException(nameof(rightDisplayName));
            }

            this.Initialize(rightName, rightDisplayName);
        }

        public ICommand SaveCommand { get; private set; }

        public string SetName
        {
            get => this._setName;
            set
            {
                this._setName = value;
                this.RaisePropertyChanged(() => this.SetName);
            }
        }

        public ObservableCollection<TranslationItem> FileSystemRights { get; private set; }

        public ObservableCollection<TranslationItem> Inheritances { get; private set; }

        public ObservableCollection<TranslationItem> AccessControlTypes { get; private set; }

        public ConfigItem NewTranslatedItem { get; private set; }

        private void Initialize(string rightName, string rightDisplayName)
        {
            this.DisplayName = ConfigurationResource.EditTranslationCaption;

            this.FileSystemRights = new ObservableCollection<TranslationItem>();
            this.Inheritances = new ObservableCollection<TranslationItem>();
            this.AccessControlTypes = new ObservableCollection<TranslationItem>();

            this.AddItems();

            if (!string.IsNullOrEmpty(rightName) && !string.IsNullOrEmpty(rightDisplayName))
            {
                this.SelectItems(rightName, rightDisplayName);
            }

            this.SaveCommand = new RelayCommand(this.CreateItem, this.CanCreateItem);
        }

        public event EventHandler ItemChanged;

        private bool CanCreateItem(object obj)
        {
            return !string.IsNullOrEmpty(this.SetName) && this.AccessControlTypes.Count(m => m.Selected) > 0 && this.FileSystemRights.Count(m => m.Selected) > 0 && this.Inheritances.Count(m => m.Selected) > 0;
        }

        private void CreateItem(object obj)
        {
            var builder = new StringBuilder();

            // Joins all options into a single string.
            builder.AppendFormat("{0}: ", this.AccessControlTypes.Where(m => m.Selected).Select(m => m.Name).First());
            builder.Append(string.Join(", ", this.FileSystemRights.Where(m => m.Selected).Select(m => m.Name)));
            builder.Append(", ");
            builder.Append(this.Inheritances.Where(m => m.Selected).Select(m => m.Name).First());

            // Initialises object of ConfigItem and assign required properties with appropriate data.
            this.NewTranslatedItem = new RightsTranslationItem
            {
                DisplayName = this.SetName,
                Name = builder.ToString()
            };

            // Fires a notification that a new translation item has been created.
            this.ItemChanged?.Invoke(this, new EventArgs());

            // Triggers Closing event so that a view could be closed.
            this.CancelCommand.Execute(null);
        }

        private void AddItems()
        {
            this.AccessControlTypes.Add(new TranslationItem { Name = "Allow" });
            this.AccessControlTypes.Add(new TranslationItem { Name = "Deny" });

            this.FileSystemRights.Add(new TranslationItem { Name = "ReadAndExecute" });
            this.FileSystemRights.Add(new TranslationItem { Name = "FullControl" });
            this.FileSystemRights.Add(new TranslationItem { Name = "Read" });
            this.FileSystemRights.Add(new TranslationItem { Name = "Write" });
            this.FileSystemRights.Add(new TranslationItem { Name = "Modify" });
            this.FileSystemRights.Add(new TranslationItem { Name = "Synchronize" });

            this.Inheritances.Add(new TranslationItem { Name = "Subfolders and Files only" });
            this.Inheritances.Add(new TranslationItem { Name = "This Folder, Subfolders and Files" });
            this.Inheritances.Add(new TranslationItem { Name = "This folder and Subfolders" });
            this.Inheritances.Add(new TranslationItem { Name = "Subfolders only" });
            this.Inheritances.Add(new TranslationItem { Name = "This Folder and Files" });
            this.Inheritances.Add(new TranslationItem { Name = "This Folder only" });
        }

        private void SelectItems(string name, string displayName)
        {
            this.SetName = displayName;

            var startIndex = 0; // A position to start a loop from

            // Splits the name into an array. The array will contain Allow/Deny, FileSystem Rights, and Inheritance in sequence.
            string[] translationItems = name.Split(new[] { ", ", ": ", ",", ":" }, StringSplitOptions.RemoveEmptyEntries);

            // The first item in array is most probably Allow/Deny.
            string accessTypeName = translationItems[0].ToLower();

            // If item is loaded from older configuration Allow/Deny will not be available in Name.
            if (accessTypeName != "allow" && accessTypeName != "deny")
            {
                // None of Allow or Deny is available in name.
                this.AccessControlTypes[0].Selected = true; // Allow is selected as default.
            }
            else
            {
                // Item does contain Allow/Deny. Select appropriate item.
                this.AccessControlTypes.FirstOrDefault(m => m.Name.ToLower() == accessTypeName).Selected = true;
                startIndex = 1; // Since the "translateItems" array contains Allow/Deny, the loop to read further information should start from next index.
            }

            TranslationItem transItem = null;

            for (int index = startIndex; index < translationItems.Length; index++)
            {
                string fsrName = translationItems[index].ToLower();

                // Searches FileSystem Right name such as Read, Write, Modify etc. in the FileSystemRights collection.
                transItem = this.FileSystemRights.FirstOrDefault(m => m.Name.ToLower() == fsrName);

                if (transItem == null)
                {
                    // If reference is null, it means the Right is not available in the collection, and continue further in the
                    // loop is not required as now the sequence will contain name of Inheritance.
                    // Now the startIndex marks the start position from where the name of Inheritance can be retrieved.
                    startIndex = index;

                    break;
                }

                // Reference is not null, and the Right has to be selected.
                transItem.Selected = true;
            }

            // Join the inheritance text.
            // ** translationItems is an array that is split from colon (':') and comma (',') character. 
            // ** The name of inheritance may also contain comma, such as This Folder, Subfolders and Files. The Split method will also split
            // ** the inheritance name. So join them from the position where FileSystem Right ended.
            string inheritance = string.Join(", ", translationItems.Skip(startIndex));

            transItem = this.Inheritances.FirstOrDefault(m => m.Name.ToLower() == inheritance.ToLower());
            if (transItem != null)
            {
                transItem.Selected = true;
            }
        }
    }
}