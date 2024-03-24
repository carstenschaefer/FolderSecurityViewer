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

namespace FolderSecurityViewer.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    public class ContentContainer : ContentControl
    {
        private Border _mainBorder;

        private ContentPresenter _mainPresenter;
        private Border _oldBorder;
        private ContentPresenter _oldPresenter;
        private Storyboard _storyboardIn;

        private Storyboard _storyboardOut;

        public ContentContainer()
        {
            this.InitStoryBoards();
        }

        public bool UseEffect
        {
            get => (bool)this.GetValue(UseEffectProperty);
            set => this.SetValue(UseEffectProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._oldPresenter = this.GetTemplateChild("PART_OldContent") as ContentPresenter;
            this._mainPresenter = this.GetTemplateChild("PART_MainContent") as ContentPresenter;
            this._oldBorder = this.GetTemplateChild("PART_BorderOldContent") as Border;
            this._mainBorder = this.GetTemplateChild("PART_BorderMainContent") as Border;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (this._mainPresenter == null || this._oldPresenter == null)
            {
                return;
            }

            this._mainBorder.Visibility = Visibility.Collapsed;
            this._mainBorder.Opacity = 0;

            this._mainPresenter.SetCurrentValue(ContentPresenter.ContentProperty, newContent);
            this._oldPresenter.SetCurrentValue(ContentPresenter.ContentProperty, oldContent);

            this.BeginAnimateContentReplacement();
        }

        private void BeginAnimateContentReplacement()
        {
            this._oldBorder.Visibility = Visibility.Visible;
            this._storyboardOut.Begin(this._oldBorder);
        }

        private void InitStoryBoards()
        {
            this._storyboardIn = (Storyboard)this.FindResource("ContentInStoryboard");
            this._storyboardOut = ((Storyboard)this.FindResource("ContentOutStoryboard")).Clone();

            this._storyboardOut.Completed += (s, e) =>
            {
                this._oldPresenter.Visibility = Visibility.Collapsed;
                this._mainBorder.Visibility = Visibility.Visible;
                this._storyboardIn.Begin(this._mainBorder);
            };

            this._storyboardOut.Freeze();
        }

        #region Static Props

        static ContentContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentContainer), new FrameworkPropertyMetadata(typeof(ContentContainer)));
        }

        public static readonly DependencyProperty UseEffectProperty = DependencyProperty.Register(nameof(UseEffect), typeof(bool), typeof(ContentContainer), new PropertyMetadata(true));

        #endregion
    }
}