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
    using System.Windows.Shapes;

    public class Spinner : ProgressBar
    {
        public static readonly DependencyProperty IsSpinningProperty = DependencyProperty.Register(nameof(IsSpinning), typeof(bool), typeof(Spinner), new FrameworkPropertyMetadata(false, OnSpinningChange));

        private Path _innerPath;
        private Path _outerPath;

        public Spinner()
        {
            this.IsIndeterminate = true;
            this.Hide();
        }

        public bool IsSpinning
        {
            get => (bool)this.GetValue(IsSpinningProperty);
            set => this.SetValue(IsSpinningProperty, value);
        }

        private static void OnSpinningChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Spinner spinner)
            {
                if ((bool)e.NewValue)
                {
                    spinner.Show();
                }
                else
                {
                    spinner.Hide();
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this._innerPath = this.GetTemplateChild("PART_InnerIcon") as Path;
            this._outerPath = this.GetTemplateChild("PART_OuterIcon") as Path;

            if (this._innerPath == null || this._outerPath == null)
            {
                return;
            }

            double percent = this.Width * 25 / 100;

            this._innerPath.Width = this.Width - percent;
            this._innerPath.Height = this.Width - percent;
        }

        private void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void Show()
        {
            this.Visibility = Visibility.Visible;
        }
    }
}