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

using Atlassian.Bitbucket.Authentication.ViewModels;
using GitHub.Shared.Controls;

namespace Atlassian.Bitbucket.Authentication.Views
{
    /// <summary>
    /// <para>
    /// The Credentials Window is the first UI a users will see when prompted to provide
    /// authentication for a Bitbucket remote URL. Prompts for a valid username and password.
    /// </para>
    /// <para>
    /// If the user's account does NOT have 2FA configured and they submit valid this is also the
    /// last UI they will see.
    /// </para>
    /// <para>
    /// If the user's account DOES have 2FA configured and they submit valid credentials they will be
    /// prompted with a second UI to ask for OAuth authorisation <see cref="OAuthWindow"/>.
    /// </para>
    /// </summary>
    partial class CredentialsWindow : AuthenticationDialogWindow
    {
        public CredentialsWindow()
        {
            InitializeComponent();
        }

        internal CredentialsViewModel ViewModel
        {
            get { return DataContext as CredentialsViewModel; }
        }
    }
}
