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
    using System.Windows.Media.Animation;

    public enum FlyoutDirection
    {
        Left = 1,
        Right
    }

    public class Flyout : HeaderedContentControl
    {
        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register("Footer", typeof(object), typeof(Flyout));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool), typeof(Flyout), new FrameworkPropertyMetadata(OnIsOpenChanged)
        {
            BindsTwoWayByDefault = true
        });

        public static readonly DependencyProperty ContentPanelWidthProperty = DependencyProperty.Register("ContentPanelWidth", typeof(double), typeof(Flyout), new PropertyMetadata(400D));
        public static readonly DependencyProperty FlyoutDirectionProperty = DependencyProperty.Register(nameof(FlyoutDirection), typeof(FlyoutDirection), typeof(Flyout), new PropertyMetadata(FlyoutDirection.Right));
        public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register("CloseCommand", typeof(ICommand), typeof(Flyout));
        private Border _backBorder;

        private Border _container;
        private Storyboard _contentChangedIn;
        private ContentPresenter _contentPresenter;

        private DoubleAnimation _opacityAnimationIn;
        private DoubleAnimation _opacityAnimationOut;

        private ThicknessAnimation _thicknessAnimationIn;
        private ThicknessAnimation _thicknessAnimationOut;

        public Flyout()
        {
            this.SetAnimations();
        }

        public ICommand CloseCommand
        {
            get => this.GetValue(CloseCommandProperty) as ICommand;
            set => this.SetValue(CloseCommandProperty, value);
        }

        public object Footer
        {
            get => this.GetValue(FooterProperty);
            set => this.SetValue(FooterProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)this.GetValue(IsOpenProperty);
            set => this.SetValue(IsOpenProperty, value);
        }

        public double ContentPanelWidth
        {
            get => (double)this.GetValue(ContentPanelWidthProperty);
            set => this.SetValue(ContentPanelWidthProperty, value);
        }

        public FlyoutDirection FlyoutDirection
        {
            get => (FlyoutDirection)this.GetValue(FlyoutDirectionProperty);
            set => this.SetValue(FlyoutDirectionProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Flyout flyout)
            {
                if ((bool)e.NewValue)
                {
                    flyout.Show();
                }
                else
                {
                    flyout.Hide();
                }
            }
        }

        public void Show()
        {
            if (this._container == null)
            {
                return;
            }

            if (this._thicknessAnimationIn.From == null)
            {
                this._thicknessAnimationIn.From = this.FlyoutDirection == FlyoutDirection.Left ? new Thickness(-this.ContentPanelWidth, 0, 0, 0) : new Thickness(0, 0, -this.ContentPanelWidth, 0);
            }

            this.Visibility = Visibility.Visible;
            this.BeginAnimation(OpacityProperty, this._opacityAnimationIn);
            this._container.BeginAnimation(MarginProperty, this._thicknessAnimationIn);
        }

        public void Hide()
        {
            if (this._container == null)
            {
                return;
            }

            if (this._thicknessAnimationOut.To == null)
            {
                this._thicknessAnimationOut.To = this.FlyoutDirection == FlyoutDirection.Left ? new Thickness(-this.ContentPanelWidth, 0, 0, 0) : new Thickness(0, 0, -this.ContentPanelWidth, 0);
            }

            this._container.BeginAnimation(MarginProperty, this._thicknessAnimationOut);
            this.BeginAnimation(OpacityProperty, this._opacityAnimationOut);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._container = this.GetTemplateChild("PART_Container") as Border;
            this._backBorder = this.GetTemplateChild("PART_BackBorder") as Border;
            this._contentPresenter = this.GetTemplateChild("PART_Content") as ContentPresenter;

            if (this._container != null)
            {
                Panel.SetZIndex(this._container, int.MaxValue);
            }

            if (this.IsOpen)
            {
                this.Show();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (oldContent != newContent && this._contentPresenter != null)
            {
                this._contentPresenter.Opacity = 0;
                this._contentChangedIn.Begin(this._contentPresenter);
            }
        }

        private void SetAnimations()
        {
            this._thicknessAnimationIn = new ThicknessAnimation
            {
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            this._thicknessAnimationOut = new ThicknessAnimation
            {
                From = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            this._opacityAnimationIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            this._opacityAnimationOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));

            this._thicknessAnimationOut.Completed += (s, e) => this.Visibility = Visibility.Collapsed;

            this._contentChangedIn = this.FindResource("ContentInStoryboard") as Storyboard;
        }
    }
}