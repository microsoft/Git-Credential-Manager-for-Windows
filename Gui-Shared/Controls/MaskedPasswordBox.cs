/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GitHub.Shared.Controls
{
    /// <summary>
    /// A masking TextBox control based on http://blogs.ugidotnet.org/leonardo
    /// </summary>
    /// <remarks>
    /// <para>
    /// You might be wondering why we don't use SecureString. The main reason is that the password
    /// value is passed back to the Git Credential Provider as a normal string. So there's no point
    /// in us using a SecureString here. It needs to be a secure string all the way down to really
    /// make a difference.
    /// </para>
    /// </remarks>
    public class MaskedPasswordBox : PromptTextBox
    {
        // Fake char to display in Visual Tree
        private const char pwdChar = '●';

        // flag used to bypass OnTextChanged
        private bool dirtyBaseText;

        /// <summary>
        /// Only copy of real password
        /// </summary>
        /// <remarks>For more security use System.Security.SecureString type instead</remarks>
        private string password = string.Empty;

        /// <summary>
        /// Provide access to base.Text without call OnTextChanged
        /// </summary>
        protected string BaseText
        {
            get { return base.Text; }
            set
            {
                dirtyBaseText = true;
                base.Text = value;
                dirtyBaseText = false;
            }
        }

        /// <summary>
        /// Clean Password
        /// </summary>
        public new string Text
        {
            get { return password; }
            set
            {
                password = value ?? string.Empty;
                BaseText = new string(pwdChar, password.Length);
            }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(MaskedPasswordBox), new UIPropertyMetadata(null));

        /// <summary>
        /// Copy of real password
        /// </summary>
        [Localizability(LocalizationCategory.Text)]
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        /// <summary>
        /// TextChanged event handler for secure storing of password into Visual Tree, text is
        /// replaced with pwdChar chars, clean text is kept in Text property (CLR property not
        /// snoopable without mod)
        /// </summary>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (dirtyBaseText)
                return;

            string currentText = BaseText;

            int selStart = SelectionStart;
            if (password != null && currentText.Length < password.Length)
            {
                // Remove deleted chars
                password = password.Remove(selStart, password.Length - currentText.Length);
            }
            if (!string.IsNullOrEmpty(currentText))
            {
                for (int i = 0; i < currentText.Length; i++)
                {
                    if (currentText[i] != pwdChar)
                    {
                        Debug.Assert(password != null, "Password can't be null here");
                        // Replace or insert char
                        string currentCharacter = currentText[i].ToString(CultureInfo.InvariantCulture);
                        password = BaseText.Length == password.Length ? password.Remove(i, 1).Insert(i, currentCharacter) : password.Insert(i, currentCharacter);
                    }
                }
                Debug.Assert(password != null, "Password can't be null here");
                BaseText = new string(pwdChar, password.Length);
                SelectionStart = selStart;
            }
            Password = password ?? string.Empty;
            base.OnTextChanged(e);
        }
    }
}
