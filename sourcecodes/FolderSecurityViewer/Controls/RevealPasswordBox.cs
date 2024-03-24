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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using FSV.Crypto;
    using FSV.Crypto.Abstractions;

    public class RevealPasswordBox : Control
    {
        private const string TEMP_PASSWORD = "TempPassword";
        public static DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(RevealPasswordBox));
        public static DependencyProperty HidePlaceholderProperty = DependencyProperty.Register(nameof(HidePlaceholder), typeof(bool), typeof(RevealPasswordBox));

        private readonly ISecure _secure;

        private PasswordBox _password;
        private SecureString _passwordSecureString;
        private IconButton _revealButton;
        private Image _revealedText;

        private bool _shouldFillTempPasswordOnLoad;
        private bool _usingPasswordForReveal;

        public RevealPasswordBox()
        {
            this.Loaded += this.RevealPasswordBoxLoaded;
            this.Unloaded += this.RevealPasswordBoxUnloaded;

            this._secure = new Secure();
        }

        public bool HidePlaceholder
        {
            get => (bool)this.GetValue(HidePlaceholderProperty);
            private set => this.SetValue(HidePlaceholderProperty, value);
        }

        public string Placeholder
        {
            get => this.GetValue(PlaceholderProperty) as string;
            set => this.SetValue(PlaceholderProperty, value);
        }

        public SecureString Password => this._password?.SecurePassword;

        private void RevealPasswordBoxUnloaded(object sender, RoutedEventArgs e)
        {
            if (this._password != null)
            {
                this._password.PasswordChanged -= this.PasswordChanged;
                this._password.GotKeyboardFocus -= this.PasswordGotFocus;
                this._password.LostKeyboardFocus -= this.PasswordLostFocus;
            }

            if (this._revealButton != null)
            {
                this._revealButton.PreviewMouseDown -= this.RevealButtonPreviewMouseDown;
                this._revealButton.PreviewMouseUp -= this.RevealButtonPreviewMouseUp;
            }
        }

        private void RevealPasswordBoxLoaded(object sender, RoutedEventArgs e)
        {
            this._password = this.GetTemplateChild("PART_ContentHost") as PasswordBox;
            this._revealButton = this.GetTemplateChild("PART_RevealButton") as IconButton;
            this._revealedText = this.GetTemplateChild("PART_Revealed") as Image;

            if (this._password != null)
            {
                this._password.PasswordChanged += this.PasswordChanged;
                this._password.GotFocus += this.PasswordGotFocus;
                this._password.LostFocus += this.PasswordLostFocus;

                if (this._shouldFillTempPasswordOnLoad)
                {
                    this._password.Password = TEMP_PASSWORD;
                    this._shouldFillTempPasswordOnLoad = false;
                }
            }

            if (this._revealButton != null)
            {
                this._revealButton.PreviewMouseDown += this.RevealButtonPreviewMouseDown;
                this._revealButton.PreviewMouseUp += this.RevealButtonPreviewMouseUp;
            }
        }

        private void PasswordLostFocus(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Unfocused", true);
        }

        private void PasswordGotFocus(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Focused", true);
            if (this._usingPasswordForReveal)
            {
                this._password.Password = string.Empty;
                this._passwordSecureString = null;
            }

            this._usingPasswordForReveal = false;
        }

        private void RevealButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this._revealedText.Source = this.GetRevealImageFromPassword();
            this._revealedText.Visibility = Visibility.Visible;
            this._password.Visibility = Visibility.Collapsed;
        }

        private void RevealButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this._revealedText.Source = null;
            this._revealedText.Visibility = Visibility.Collapsed;
            this._password.Visibility = Visibility.Visible;
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!this._usingPasswordForReveal)
            {
                this._passwordSecureString = null;
            }

            this.HidePlaceholder = this._password.Password.Length > 0;
        }

        public void UseTempPassword()
        {
            if (this._password != null)
            {
                this._password.Password = TEMP_PASSWORD;
            }
            else
                // The control is not loaded yet.
            {
                this._shouldFillTempPasswordOnLoad = true;
            }
        }

        public void SetPasswordForReveal(SecureString password)
        {
            this.UseTempPassword();
            this._passwordSecureString = password;
            this._usingPasswordForReveal = true;
        }

        private ImageSource GetRevealImageFromPassword()
        {
            SecureString passwordForReveal = this._passwordSecureString ?? this._password.SecurePassword;
            if (passwordForReveal?.Length == 0)
            {
                return null;
            }

            var visual = new DrawingVisual();

            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                var typeFace = new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var left = 0D;

                IEnumerable<byte> chars = this._secure.GetBytes(passwordForReveal);
                foreach (byte c in chars)
                {
                    var s = ((char)c).ToString();
                    var formattedText = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, this.FontSize + 1, this.Foreground, 96);
                    drawingContext.DrawText(formattedText, new Point(left, 0));
                    left = left + formattedText.WidthIncludingTrailingWhitespace;
                }
            }

            return new DrawingImage(visual.Drawing);
        }
    }
}