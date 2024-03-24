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
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    public class VerticalPanel : StackPanel
    {
        public VerticalPanel()
        {
            this.Orientation = Orientation.Vertical;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Children == null || this.Children.Count == 0)
            {
                return finalSize;
            }

            var curX = 0;

            var animationStart = 0;
            TimeSpan animationDuration = TimeSpan.FromMilliseconds(500);

            foreach (UIElement child in this.Children)
            {
                if (!(child.RenderTransform is TranslateTransform trans))
                {
                    child.RenderTransformOrigin = new Point(0, 0);
                    trans = new TranslateTransform();
                    child.RenderTransform = trans;
                }

                child.Opacity = 0;

                var animation = new DoubleAnimation(-100, curX, animationDuration)
                {
                    BeginTime = TimeSpan.FromMilliseconds(animationStart),
                    EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Springiness = 10, Oscillations = 2 }
                };

                var opacityAnimation = new DoubleAnimation(0, 1, animationDuration)
                {
                    BeginTime = TimeSpan.FromMilliseconds(animationStart),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                animationStart += 100;

                trans.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.Compose);
                child.BeginAnimation(OpacityProperty, opacityAnimation);
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}