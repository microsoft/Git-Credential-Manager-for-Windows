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
    public static class Global
    {
        /// <summary>
        /// The maximum number of redirects that the request follows.
        /// </summary>
        public const int MaxAutomaticRedirections = 16;

        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        /// <summary>
        /// <para>Gets or sets the user-agent string sent as part of the header in any HTTP operations.</para>
        /// <para>Defaults to a value contrived based on the executing assembly.</para>
        /// </summary>
        public static string UserAgent
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_useragent == null)
                    {
                        _useragent = DefaultUserAgent;
                    }
                    return _useragent;
                }
            }
            set
            {
                lock (_syncpoint) _useragent = value;
            }
        }

        private static string _useragent = null;

        private static readonly object _syncpoint = new object();

        /// <summary>
        /// Creates the correct user-agent string for HTTP calls.
        /// </summary>
        /// <returns>The `user-agent` string for "git-tools".</returns>
        private static string DefaultUserAgent
        {
            get
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(Global).Assembly;
                var assemblyName = assembly.GetName();
                var name = assemblyName.Name;
                var version = assemblyName.Version;
                var useragent = string.Format("{0} ({1}; {2}; {3}) CLR/{4} git-tools/{5}",
                                              name,
                                              Environment.OSVersion.VersionString,
                                              Environment.OSVersion.Platform,
                                              Environment.Is64BitOperatingSystem ? "x64" : "x86",
                                              Environment.Version.ToString(3),
                                              version.ToString(3));

                return useragent;
            }
        }
    }
}
