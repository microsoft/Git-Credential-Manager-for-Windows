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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Atlassian.Shared.Helpers;

namespace Atlassian.Bitbucket.Authentication.OAuth
{
    /// <summary>
    /// Implements a simple HTTP server capable of handling Bitbucket OAuth callback requests.
    /// </summary>
    public class SimpleServer
    {
        /// <summary>
        /// Async wait for an URL with a timeout
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RemoteHostException">Throws when there's a timeout, or other error</exception>
        public static async Task<string> WaitForURLAsync(string url, CancellationToken cancellationToken)
        {
            var listener = new HttpListener { Prefixes = { url } };
            listener.Start();

            string rawUrl = "";
            try
            {
                var context = await listener.GetContextAsync().RunWithCancellation(cancellationToken);
                rawUrl = context.Request.RawUrl;

                // Serve back a simple authentication message.
                var html = GetSuccessString();
                var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                context.Response.Close();
            }
            catch (TimeoutException ex)
            {
                throw new Exception("Timeout awaiting incoming request.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failure awaiting incoming request.", ex);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }

            return rawUrl;
        }

        /// <summary>
        /// Returns a Success HTML page or a simple success message, if the HTML page cannot be
        /// loaded, to be served back to the user.
        /// </summary>
        /// <returns></returns>
        private static string GetSuccessString()
        {
            var result = "Authentication Successful. You may now close this page";

            try
            {
                if (!UriParser.IsKnownScheme("pack"))
                    new System.Windows.Application();

                var html = Application.GetResourceStream(new Uri("pack://application:,,,/Bitbucket.Authentication;Component/Assets/auth.html", UriKind.Absolute));
                if (html != null && html.Stream != null)
                {
                    using (StreamReader reader = new StreamReader(html.Stream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return result;
            }

            return result;
        }
    }
}
