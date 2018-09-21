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

        private static int _requestTimeout = 90 * 1000; // 90 second default limit.
        private static readonly object _syncpoint = new object();
        private static string _useragent = BuildDefaultUserAgent(RuntimeContext.Default);

        /// <summary>
        /// Gets or sets the user-agent string sent as part of the header in any HTTP operations.
        /// <para/>
        /// Defaults to a value contrived based on the executing assembly.
        /// <para/>
        /// Set the value to `<see langword="null"/>` to reset the value to default value.
        /// </summary>
        public static string UserAgent
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_useragent is null)
                    {
                        _useragent = BuildDefaultUserAgent(RuntimeContext.Default);
                    }

                    return _useragent;
                }
            }
            set { lock (_syncpoint) _useragent = value; }

        }

        public static int RequestTimeout
        {
            get { lock (_syncpoint) return _requestTimeout; }
            set { lock (_syncpoint) _requestTimeout = value; }
        }

        private static string BuildDefaultUserAgent(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(Global).Assembly;
            var assemblyName = assembly.GetName();
            var name = assemblyName.Name;
            var version = assemblyName.Version;
            var useragent = string.Format("{0} ({1}; {2}; {3}) CLR/{4} git-tools/{5}",
                                          name,
                                          context.Settings.OsVersion.VersionString,
                                          context.Settings.OsVersion.Platform,
                                          context.Settings.Is64BitOperatingSystem ? "x64" : "x86",
                                          context.Settings.Version.ToString(3),
                                          version.ToString(3));

            return useragent;
        }
    }
}
