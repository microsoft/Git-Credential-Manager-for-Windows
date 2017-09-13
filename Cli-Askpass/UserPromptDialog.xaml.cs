/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
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

using System;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Alm.Gui
{
    /// <summary>
    /// Interaction logic for PassphraseWindow.xaml
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(false)]
    public partial class UserPromptDialog : Window
    {
        public const string HintTextPassphrase = "SSH Passphrase";
        public const string HintTextPassword = "Password";
        public const string HintTextUsername = "Username";
        public const double WindowDefaultHeight = 150.0d;
        public const double WindowDefaultWidth = 420.0d;
        public const double WindowLargerHeight = 170.0d;
        public const double WindowLargerWidth = 480.0d;

        private const string AdornerStyleName = "FadedLabelStyle";
        private readonly Size DefaultSize = new Size(WindowDefaultWidth, WindowDefaultHeight);
        private readonly Size LargerSize = new Size(WindowLargerWidth, WindowLargerHeight);

        internal UserPromptDialog(UserPromptKind kind, string resource)
        {
            if ((kind & ~(UserPromptKind.AuthenticateHost | UserPromptKind.CredentialsPassword | UserPromptKind.CredentialsUsername | UserPromptKind.SshPassphrase)) != 0)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentNullException(nameof(resource));

            _kind = kind;
            _resource = resource;

            var text = new System.Text.StringBuilder("Enter ");

            switch (kind)
            {
                case UserPromptKind.CredentialsPassword:
                    {
                        text.Append("Password");

                        _title = text.ToString();

                        text.Append(" for ");
                    }
                    break;

                case UserPromptKind.CredentialsUsername:
                    {
                        text.Append("Username");

                        _title = text.ToString();

                        text.Append(" for ");
                    }
                    break;

                case UserPromptKind.SshPassphrase:
                    {
                        text.Append("Passphrase");

                        _title = text.ToString();

                        text.Append(" for key ");
                    }
                    break;
            }

            _isLarger = false;

            text.Append('\'')
                .Append(_resource)
                .Append('\'');

            _promptText = text.ToString(); ;

            InitializeComponent();

            Loaded += OnLoaded;

            DataContext = this;
        }

        internal UserPromptDialog(string hostName, string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                throw new ArgumentNullException(nameof(hostName));
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new ArgumentNullException(nameof(fingerprint));

            _fingerprint = fingerprint;
            _kind = UserPromptKind.AuthenticateHost;
            _title = "Validate Host Fingerprint";
            _isLarger = true;

            _promptText = "Continue connecting to '" + hostName + "'?";
            _additionInfoText = "RSA key fingerprint:";

            InitializeComponent();

            Loaded += OnLoaded;

            DataContext = this;
        }

        private readonly string _additionInfoText;
        private readonly string _fingerprint;
        private readonly bool _isLarger;
        private readonly string _promptText;
        private bool _failed;
        private readonly UserPromptKind _kind;
        private string _response;
        private string _resource;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private PasswordBoxHintAdorner _textboxAdorner;

        private readonly string _title;

        public string AdditionalInfoText
        {
            get { return _additionInfoText; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public double DesiredHeight
        {
            get
            {
                return _isLarger
                  ? LargerSize.Height
                  : DefaultSize.Height;
            }
            set { /* nope */ }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public double DesiredWidth
        {
            get
            {
                return _isLarger
                  ? LargerSize.Width
                  : DefaultSize.Width;
            }
            set { /* nope */ }
        }

        public bool Failed
        {
            get { return _failed; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public string Fingerprint
        {
            get { return _fingerprint; }
            set { /* nope */ }
        }

        public UserPromptKind Kind
        {
            get { return _kind; }
        }

        public string PromptText
        {
            get { return _promptText; }
        }

        public string Response
        {
            get { return _response; }
        }

        protected void OnLoaded(object sender, RoutedEventArgs args)
        {
            string hintText = null;

            Title = _title;

            switch (_kind)
            {
                case UserPromptKind.AuthenticateHost:
                    PositiveButton.Content = "_Yes";
                    NegativeButton.Content = "_No";
                    AdditionalInfoLabel.Visibility = Visibility.Visible;
                    break;

                case UserPromptKind.CredentialsPassword:
                    hintText = HintTextPassword;
                    break;

                case UserPromptKind.CredentialsUsername:
                    hintText = HintTextUsername;
                    break;

                case UserPromptKind.SshPassphrase:
                    hintText = HintTextPassphrase;
                    break;
            }

            LearnMoreLink.Visibility = _kind == UserPromptKind.SshPassphrase
                ? Visibility.Visible
                : Visibility.Collapsed;

            Topmost = true;
            BringIntoView();
            Activate();

            if (hintText != null)
            {
                UserInput.Visibility = Visibility.Visible;

                var style = Resources[AdornerStyleName] as Style;
                if (style != null)
                {
                    _textboxAdorner = new PasswordBoxHintAdorner(PassphrasePasswordBox, hintText, style, IsAdornerVisible);
                }

                PassphrasePasswordBox.Focus();
            }
            else
            {
                UserInput.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (ReferenceEquals(e, null))
                return;

            switch (e.Key)
            {
                case Key.Enter:
                    Success();
                    break;

                case Key.Escape:
                    Failure();
                    break;

                default:
                    base.OnKeyUp(e);
                    break;
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            Keyboard.Focus(PassphrasePasswordBox);
        }

        protected void MoreInfoLabel_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = Cli.Program.DefinitionUrlPassphrase,
                UseShellExecute = true,
            });
        }

        protected void NegativeButton_Click(object sender, RoutedEventArgs e)
        {
            Failure();
            Close();
        }

        protected void PositiveButton_Click(object sender, RoutedEventArgs e)
        {
            Success();
            Close();
        }

        private void Failure()
        {
            _failed = true;
            _response = null;
        }

        private Visibility IsAdornerVisible()
        {
            return (PassphrasePasswordBox.IsFocused || PassphrasePasswordBox.Password.Length > 0)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void Success()
        {
            string passphrase = PassphrasePasswordBox.Password;

            if (string.IsNullOrWhiteSpace(passphrase))
            {
                _failed = false;
                _response = null;
            }
            else
            {
                _failed = false;
                _response = passphrase;
            }
        }
    }
}
