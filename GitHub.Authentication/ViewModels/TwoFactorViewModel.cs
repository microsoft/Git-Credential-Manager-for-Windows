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

using System.Windows;
using System.Windows.Input;
using GitHub.Authentication.Properties;
using GitHub.Shared.Helpers;
using GitHub.Shared.ViewModels;

namespace GitHub.Authentication.ViewModels
{
    /// <summary>
    /// Simple view model for the GitHub Two Factor dialog.
    /// </summary>
    public class TwoFactorViewModel : DialogViewModel
    {
        /// <summary>
        /// This is used by the GitHub.Authentication test application
        /// </summary>
        public TwoFactorViewModel() : this(false, string.Empty) { }

        /// <summary>
        /// This quite obviously creates an instance of a <see cref="TwoFactorViewModel"/>.
        /// </summary>
        /// <param name="isSms">True if the 2fa authentication code is sent via SMS</param>
        public TwoFactorViewModel(bool isSms, string path)
        {
            OkCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Ok);
            CancelCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Cancel);

            IsSms = isSms;
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AuthenticationCode))
                {
                    // We currently rely on the UI to ensure that the authentication code consists of
                    // digits only.
                    IsValid = AuthenticationCode.Length == 6;
                }
            };

            HasPath = Visibility.Hidden;
            if (!string.IsNullOrWhiteSpace(path)
                && path.Contains("/"))
            {
                // bitbucket format should be /org/repo.git
                var parts = path.Split('/');
                if (parts.Length == 3)
                {
                    Organisation = parts[1];
                    if (!string.IsNullOrWhiteSpace(parts[2])
                        && parts[2].EndsWith(".git"))
                    {
                        Repository = parts[2].Substring(0, parts[2].Length - ".git".Length);
                        HasPath = Visibility.Visible;
                    }
                }
            }
        }

        private string _authenticationCode;

        /// <summary>
        /// The Two-factor authentication code the user types in.
        /// </summary>
        public string AuthenticationCode
        {
            get { return _authenticationCode; }
            set
            {
                _authenticationCode = value;
                RaisePropertyChangedEvent(nameof(AuthenticationCode));
            }
        }

        public bool IsSms { get; }

        public string Description
        {
            get
            {
                return IsSms
                    ? Resources.TwoFactorSms
                    : Resources.OpenTwoFactorAuthAppText;
            }
        }
        /// <summary>
        ///     Gets the repository name, as found from the path
        /// </summary>
        public string Repository { get; }

        /// <summary>
        ///     Gets the organisation name, as found from the path
        /// </summary>
        public string Organisation { get; }

        /// <summary>
        ///     Gets a flag indicating if there is a org and repo to show
        /// </summary>
        public Visibility HasPath { get; }

        public ICommand LearnMoreCommand { get; }
            = new HyperLinkCommand();

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
    }
}
