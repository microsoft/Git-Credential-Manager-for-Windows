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

namespace Microsoft.Alm.Authentication
{
    internal static class Global
    {
        private static string _useragent = null;

        /// <summary>
        /// Creates the correct user-agent string for HTTP calls.
        /// </summary>
        /// <returns>The `user-agent` string for "git-tools".</returns>
        public static string GetUserAgent()
        {
            if (_useragent == null)
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                _useragent = String.Format("git-credential-manager ({0}; {1}; {2}) CLR/{3} git-tools/{4}",
                                           Environment.OSVersion.VersionString,
                                           Environment.OSVersion.Platform,
                                           Environment.Is64BitOperatingSystem ? "x64" : "x86",
                                           Environment.Version.ToString(3),
                                           version.ToString(3));
            }
            return _useragent;
        }
    }
}
