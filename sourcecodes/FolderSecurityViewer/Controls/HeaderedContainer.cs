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
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    public class HeaderedContainer : HeaderedContentControl
    {
        private Storyboard _contentChangedInStoryboard;
        private Storyboard _contentChangedOutStoryboard;
        private Grid _contentPresenter;
        private Shape _rectangle;

        private Grid _root;
        private Storyboard _storyboardIn;

        private Storyboard _storyboardOut;

        public HeaderedContainer()
        {
            this.InitStoryBoards();
        }

        public ICommand CloseCommand
        {
            get => (ICommand)this.GetValue(CloseCommandProperty);
            set => this.SetValue(CloseCommandProperty, value);
        }

        public bool IsDisplayed
        {
            get => (bool)this.GetValue(IsDisplayedProperty);
            set => this.SetValue(IsDisplayedProperty, value);
        }

        public object Footer
        {
            get => this.GetValue(FooterProperty);
            set => this.SetValue(FooterProperty, value);
        }

        /// <summary>
        ///     This gets called when the template has been applied and we have our visual tree
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._root = this.GetTemplateChild("PART_Root") as Grid;
            this._rectangle = this.GetTemplateChild("PART_PaintArea") as Shape;
            this._contentPresenter = this.GetTemplateChild("PART_MainContent") as Grid;

            if (this.IsDisplayed)
            {
                this.Show();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        ///     This gets called when the content we're displaying has changed
        /// </summary>
        /// <param name="oldContent">The content that was previously displayed</param>
        /// <param name="newContent">The new content that is displayed</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (this._rectangle != null && this._contentPresenter != null && oldContent != newContent)
            {
                this._rectangle.Fill = this.CreateBrushFromVisual(this._contentPresenter);
                this.BeginAnimateContentReplacement();
            }
        }

        private void Show()
        {
            this.Visibility = Visibility.Visible;
            if (this._root == null)
            {
                return;
            }

            this._storyboardIn.Begin(this._root);
        }

        private void Hide()
        {
            if (this._root == null || !this.IsLoaded)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            this._storyboardOut.Begin(this._root);
        }

        private void InitStoryBoards()
        {
            // Storyboards to transition on IsDisplayed changed.
            this._storyboardIn = (Storyboard)this.FindResource("HeaderedContainerInStoryboard");
            this._storyboardOut = ((Storyboard)this.FindResource("HeaderedContainerOutStoryboard")).Clone();
            this._storyboardOut.Completed += (s, e) => this.Visibility = Visibility.Collapsed;

            // Storyboards to transition when Content property changed.
            this._contentChangedInStoryboard = (Storyboard)this.FindResource("ContentInStoryboard");
            this._contentChangedOutStoryboard = ((Storyboard)this.FindResource("ContentOutStoryboard")).Clone();
            this._contentChangedOutStoryboard.Completed += this.OnContentChangeTransitionComplete;

            this._storyboardOut.Freeze();
            this._contentChangedOutStoryboard.Freeze();
        }

        /// <summary>
        ///     Starts the animation for the new content
        /// </summary>
        private void BeginAnimateContentReplacement()
        {
            this._rectangle.Visibility = Visibility.Visible;
            this._contentPresenter.Visibility = Visibility.Collapsed;
            this._contentPresenter.Opacity = 0;

            this._contentChangedOutStoryboard.Begin(this._rectangle);
        }

        private void OnContentChangeTransitionComplete(object sender, EventArgs e)
        {
            this._rectangle.Visibility = Visibility.Collapsed;
            this._contentPresenter.Visibility = Visibility.Visible;

            this._contentChangedInStoryboard.Begin(this._contentPresenter);
        }

        /// <summary>
        ///     Creates a brush based on the current appearnace of a visual element. The brush is an ImageBrush and once created,
        ///     won't update its look
        /// </summary>
        /// <param name="v">The visual element to take a snapshot of</param>
        private Brush CreateBrushFromVisual(Visual visual)
        {
            if (visual == null)
            {
                throw new ArgumentNullException(nameof(visual));
            }

            var actualHeight = (int)(this.ActualHeight == 0 ? 1 : this.ActualHeight);
            var actualWidth = (int)(this.ActualWidth == 0 ? 1 : this.ActualWidth);
            var target = new RenderTargetBitmap(actualWidth, actualHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);
            var brush = new ImageBrush(target);
            brush.Freeze();
            return brush;
        }

        #region Static

        static HeaderedContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedContainer), new FrameworkPropertyMetadata(typeof(HeaderedContainer)));
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer), typeof(object), typeof(HeaderedContainer));
        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register(nameof(IsDisplayed), typeof(bool), typeof(HeaderedContainer), new FrameworkPropertyMetadata(true, OnIsDisplayedChanged));
        public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(HeaderedContainer));

        private static void OnIsDisplayedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is HeaderedContainer control)
            {
                if ((bool)e.NewValue)
                {
                    control.Show();
                }
                else
                {
                    control.Hide();
                }
            }
        }

        #endregion
    }
}