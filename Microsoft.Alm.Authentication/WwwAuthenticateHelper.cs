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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal class WwwAuthenticateHelper
    {
        internal static readonly Credential Credentials = new Credential(string.Empty, string.Empty);
        internal static readonly AuthenticationHeaderValue NtlmHeader = new AuthenticationHeaderValue("NTLM");
        internal static readonly AuthenticationHeaderValue NegotiateHeader = new AuthenticationHeaderValue("Negotiate");

        private static readonly AuthenticationHeaderValue[] NullResult = new AuthenticationHeaderValue[0];

        public static async Task<AuthenticationHeaderValue[]> GetHeaderValues(RuntimeContext context, TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            if (targetUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.Ordinal)
                || targetUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.Ordinal))
            {
                try
                {
                    // Try the query URI
                    string queryUrl = targetUri.QueryUri.ToString();

                    // Send off the HTTP requests and wait for them to complete
                    var result = await RequestAuthenticate(targetUri, queryUrl);

                    // If any of then returned true, then we're looking at a TFS server
                    HashSet<AuthenticationHeaderValue> set = new HashSet<AuthenticationHeaderValue>();

                    // Combine the results into a unique set
                    foreach (var item in result)
                    {
                        set.Add(item);
                    }

                    return set.ToArray();
                }
                catch (Exception exception)
                {
                    context.Trace.WriteLine("error testing targetUri for NTLM: " + exception.Message);
                }
            }

            return NullResult;
        }

        public static bool IsNtlm(AuthenticationHeaderValue value)
        {
            return value?.Scheme != null
                && value.Scheme.Equals(NtlmHeader.Scheme, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<AuthenticationHeaderValue[]> RequestAuthenticate(TargetUri targetUri, string targetUrl)
        {
            using (var httpClientHandler = targetUri.HttpClientHandler)
            {
                // configure the http client handler to not choose an authentication strategy for us
                // because we want to deliver the complete payload to the caller
                httpClientHandler.AllowAutoRedirect = false;
                httpClientHandler.PreAuthenticate = false;
                httpClientHandler.UseDefaultCredentials = false;

                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Global.UserAgent);
                    client.Timeout = TimeSpan.FromMilliseconds(Global.RequestTimeout);

                    using (HttpResponseMessage response = await client.GetAsync(targetUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        // Check for a WWW-Authenticate header with NTLM protocol specified
                        return response.Headers.Contains("WWW-Authenticate")
                            ? response.Headers.WwwAuthenticate.ToArray()
                            : NullResult;
                    }
                }
            }
        }
    }
}
