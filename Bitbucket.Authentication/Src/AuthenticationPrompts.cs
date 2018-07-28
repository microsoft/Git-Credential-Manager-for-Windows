/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Atlassian.Bitbucket.Authentication.ViewModels;
using Atlassian.Bitbucket.Authentication.Views;
using GitHub.Shared.Controls;
using GitHub.Shared.ViewModels;
using Microsoft.Alm.Authentication;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Defines how to call the UI elements to request authentication details from the user.
    /// </summary>
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

        /// <summary>
        /// Utility method used to extract a username from a URL of the form http(s)://username@domain/
        /// </summary>
        /// <param name="targetUri"></param>
        /// <returns></returns>
        public static string GetUserFromTargetUri(TargetUri targetUri)
        {
            var url = targetUri.QueryUri.AbsoluteUri;
            if (!url.Contains("@"))
            {
                return null;
            }

            var match = Regex.Match(url, @"\/\/(.+)@");
            if (!match.Success)
            {
                return null;
            }

            return match.Groups[1].Value;
        }

        /// <summary>
        /// Opens a Modal UI prompting the user for Basic Auth credentials.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="targetUri">contains the URL etc of the Authority</param>
        /// <param name="username">the username entered by the user</param>
        /// <param name="password">the password entered by the user</param>
        /// <returns>
        /// returns true if the user provides credentials which are then successfully validated,
        /// false otherwise
        /// </returns>
        public bool CredentialModalPrompt(string title, TargetUri targetUri, out string username, out string password)
        {
            // if there is a user in the remote URL then prepopulate the UI with it.
            var credentialViewModel = new CredentialsViewModel(GetUserFromTargetUri(targetUri));

            Trace.WriteLine("prompting user for credentials.");

            bool credentialValid = Gui.ShowViewModel(credentialViewModel, () => new CredentialsWindow(Context, _parentHwnd));

            username = credentialViewModel.Login;
            password = credentialViewModel.Password;

            return credentialValid;
        }

        /// <summary>
        /// Opens a modal UI prompting the user to run the OAuth dance.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="targetUri">contains the URL etc of the Authority</param>
        /// <param name="resultType"></param>
        /// <param name="username"></param>
        /// <returns>
        /// returns true if the user successfully completes the OAuth dance and the returned
        /// access_token is validated, false otherwise
        /// </returns>
        public bool AuthenticationOAuthModalPrompt(string title, TargetUri targetUri, AuthenticationResultType resultType, string username)
        {
            var oauthViewModel = new OAuthViewModel(resultType == AuthenticationResultType.TwoFactor);

            Trace.WriteLine("prompting user for authentication code.");

            bool useOAuth = Gui.ShowViewModel(oauthViewModel, () => new OAuthWindow(Context, _parentHwnd));

            return useOAuth;
        }
    }
}
