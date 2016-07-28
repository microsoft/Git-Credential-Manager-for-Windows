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
using System.Diagnostics;

namespace Microsoft.Alm.Authentication
{
    public abstract class Secret
    {
        public static string UriToSimpleName(TargetUri targetUri, string @namespace)
        {
            const string TokenNameBaseFormat = "{0}:{1}://{2}";
            const string TokenNamePortFormat = TokenNameBaseFormat + ":{3}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("Secret::UriToName");

            string targetName = null;
            // trim any trailing slashes and/or whitespace for compatibility with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            Uri resolvedUri = targetUri.ResolvedUri;

            if (resolvedUri.IsDefaultPort)
            {
                targetName = String.Format(TokenNameBaseFormat, @namespace, resolvedUri.Scheme, trimmedHostUrl);
            }
            else
            {
                targetName = String.Format(TokenNamePortFormat, @namespace, resolvedUri.Scheme, trimmedHostUrl, resolvedUri.Port);
            }

            Trace.WriteLine("   target name = " + targetName);

            return targetName;
        }

        public static string UriToPathedName(TargetUri targetUri, string @namespace)
        {
            const string TokenNamePathFormat = "{0}:{1}://{2}{3}";
            const string TokenNamePortFormat = "{0}:{1}://{2}:{3}{4}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("Secret::UriToPathedName");

            string targetName = null;
            // trim any trailing slashes and/or whitespace for compatibility with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            Uri actualUri = targetUri.ActualUri;

            if (actualUri.IsDefaultPort)
            {
                targetName = String.Format(TokenNamePathFormat, @namespace, actualUri.Scheme, trimmedHostUrl, actualUri.AbsolutePath);
            }
            else
            {
                targetName = String.Format(TokenNamePortFormat, @namespace, actualUri.Scheme, trimmedHostUrl, actualUri.Port, actualUri.AbsolutePath);
            }

            Trace.WriteLine("   target name = " + targetName);

            return targetName;
        }

        public delegate string UriNameConversion(TargetUri targetUri, string @namespace);
    }
}
