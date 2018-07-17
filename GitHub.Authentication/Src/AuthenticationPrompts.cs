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

using System;
using GitHub.Authentication.ViewModels;
using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    public class AuthenticationPrompts : Base
    {
        public AuthenticationPrompts(RuntimeContext context, IntPtr parentHwnd)
            : base(context)
        {
            _parentHwnd = parentHwnd;
        }

        public AuthenticationPrompts(RuntimeContext context)
            : this(context, IntPtr.Zero)
        { }

        private IntPtr _parentHwnd;

        public bool CredentialModalPrompt(TargetUri targetUri, out string username, out string password)
        {
            var credentialViewModel = new CredentialsViewModel();

            Trace.WriteLine($"prompting user for credentials for '{targetUri}'.");

            bool credentialValid = Gui.ShowViewModel(credentialViewModel, () => new CredentialsWindow(Context, _parentHwnd));

            username = credentialViewModel.Login;
            password = credentialViewModel.Password;

            return credentialValid;
        }

        public bool AuthenticationCodeModalPrompt(TargetUri targetUri, GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            var twoFactorViewModel = new TwoFactorViewModel(resultType == GitHubAuthenticationResultType.TwoFactorSms);

            Trace.WriteLine($"prompting user for authentication code for '{targetUri}'.");

            bool authenticationCodeValid = Gui.ShowViewModel(twoFactorViewModel, () => new TwoFactorWindow(Context, _parentHwnd));

            authenticationCode = authenticationCodeValid
                ? twoFactorViewModel.AuthenticationCode
                : null;

            return authenticationCodeValid;
        }
    }
}
