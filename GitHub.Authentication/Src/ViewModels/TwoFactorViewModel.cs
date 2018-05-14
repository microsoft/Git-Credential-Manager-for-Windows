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
        public TwoFactorViewModel() : this(false) { }

        /// <summary>
        /// This quite obviously creates an instance of a <see cref="TwoFactorViewModel"/>.
        /// </summary>
        /// <param name="isSms">True if the 2fa authentication code is sent via SMS</param>
        public TwoFactorViewModel(bool isSms)
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

        public ICommand LearnMoreCommand { get; }
            = new HyperLinkCommand();

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
    }
}
