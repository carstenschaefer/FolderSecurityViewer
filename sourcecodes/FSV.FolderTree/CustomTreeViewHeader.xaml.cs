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

namespace FSV.FolderTree
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    /// <summary>
    ///     Interaction logic for CustomTreeViewHeader
    /// </summary>
    public partial class CustomTreeViewHeader : UserControl
    {
        /// <summary>
        ///     The mode
        /// </summary>
        private const string Mode = "WINRT";

        /// <summary>
        ///     The _image
        /// </summary>
        private string image = "drive.png";

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomTreeViewHeader" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="hasChilds">
        ///     if set to <c>true</c> [has childs].
        /// </param>
        public CustomTreeViewHeader(string text, EnumTreeNodeImage icon, bool hasChilds)
        {
            var hasC = string.Empty;

            if (!hasChilds)
            {
                hasC = "nochilds";
            }

            // set the image
            switch (icon)
            {
                case EnumTreeNodeImage.Drive:
                    this.image = "drive.png";
                    break;
                case EnumTreeNodeImage.Folder:
                    this.image = hasC + "folder.png";
                    break;
                case EnumTreeNodeImage.DriveNotReady:
                    this.image = "driveNotReady.png";
                    break;
                case EnumTreeNodeImage.AccessDenied:
                    this.image = "accessDenied.png";
                    break;
            }

            this.InitializeComponent();
            this.Text.Text = text;
        }

        /// <summary>
        ///     Gets the image.
        /// </summary>
        /// <value>
        ///     The image.
        /// </value>
        public string Image => "Images/" + Mode + "/" + this.image;

        /// <summary>
        ///     Set active.
        /// </summary>
        public void SetActive()
        {
            this.image = "active" + this.image;
            var uriSource = new Uri(@"/FSV.FolderTree;component/Images/" + Mode + "/" + this.image, UriKind.Relative);
            this.Icon.Source = new BitmapImage(uriSource);
        }

        /// <summary>
        ///     Sets inactive.
        /// </summary>
        public void SetInActive()
        {
            this.image = this.image.Replace("active", string.Empty);
            var uriSource = new Uri(@"/FSV.FolderTree;component/Images/" + Mode + "/" + this.image, UriKind.Relative);
            this.Icon.Source = new BitmapImage(uriSource);
        }
    }
}