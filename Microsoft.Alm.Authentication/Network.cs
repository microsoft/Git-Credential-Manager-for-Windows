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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    [Flags]
    public enum NetworkRequestOptionFlags
    {
        None = 0,

        /// <summary>
        /// The handler should follow redirection responses.
        /// <para/>
        /// Requests a coupled `<see cref="NetworkRequestOptions.MaxRedirections"/>` is greater than zero.
        /// </summary>
        AllowRedirections = 1 << 0,

        /// <summary>
        /// The handler to send an HTTP Authorization header with requests after authentication has taken place.
        /// </summary>
        PreAuthenticate = 1 << 1,

        /// <summary>
        /// The handler should use these cookies when sending requests.
        /// <para/>
        /// Allocates a `<see cref="CookieContainer"/>` for the request, unless a coupled `<see cref="NetworkRequestOptions.CookieContainer"/>` is not `<see langword="null"/>`.
        /// </summary>
        UseCookies = 1 << 2,

        /// <summary>
        /// The handler should use credentials when authenticating.
        /// <para/>
        /// Requires a coupled `<see cref="NetworkRequestOptions.Authorization"/>` is not `<see langword="null"/>`.
        /// </summary>
        UseCredentials = 1 << 3,

        /// <summary>
        /// The handler should use a proxy for requests.
        /// <para/>
        /// Ignored without a coupled `<see cref="TargetUri.ProxyUri"/>` is not `<see langword="null"/>`.
        /// </summary>
        UseProxy = 1 << 4,
    }

    public class NetworkRequestOptions
    {
        public NetworkRequestOptions(bool setDefaults)
            : this()
        {
            if (setDefaults)
            {
                _authentication = null;
                _cookieContainer = null;
                _flags = NetworkRequestOptionFlags.AllowRedirections
                       | NetworkRequestOptionFlags.PreAuthenticate
                       | NetworkRequestOptionFlags.UseProxy;
                _headers.Add("User-Agent", Global.UserAgent);
                _maxRedirections = Global.MaxAutomaticRedirections;
                Timeout = TimeSpan.FromMilliseconds(Global.RequestTimeout);
            }
        }

        private NetworkRequestOptions()
        {
            // Terrible, evil, do not want to do, why does NetFx force this?
            var type = typeof(HttpRequestHeaders);
            var assembly = type.Assembly;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            _headers = (HttpRequestHeaders)assembly.CreateInstance(type.FullName, false, flags, null, new object[0], null, null);
        }

        private Secret _authentication;
        private CookieContainer _cookieContainer;
        private NetworkRequestOptionFlags _flags;
        private readonly HttpRequestHeaders _headers;
        private int _maxRedirections;
        private TimeSpan _timeout;

        /// <summary>
        /// The authentication credentials associated with the handler.
        /// <para/>
        /// This property is ignored unless `<see cref="Flags"/>` contains `<see cref="NetworkRequestOptionFlags.UseCredentials"/>`.
        /// </summary>
        public Secret Authorization
        {
            get { return _authentication; }
            set
            {
                _authentication = value;
                _flags = (value is null)
                    ? (_flags & ~NetworkRequestOptionFlags.UseCredentials)
                    : (_flags | NetworkRequestOptionFlags.UseCredentials);
            }
        }

        /// <summary>
        /// The cookie container used to store server cookies by the handler.
        /// <para/>
        /// When `<see langword="null"/>`, a new container could be allocated by the calling method.
        /// </summary>
        public CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set
            {
                _cookieContainer = value;
                _flags = (value is null)
                    ? (_flags & ~NetworkRequestOptionFlags.UseCookies)
                    : (_flags | NetworkRequestOptionFlags.UseCookies);
            }
        }

        /// <summary>
        /// Gets an instance of `<see cref="NetworkRequestOptions"/>` with all default property values set.
        /// </summary>
        public static NetworkRequestOptions Default
        {
            get
            {
                return new NetworkRequestOptions(true);
            }
        }

        /// <summary>
        /// Flags related to network request options.
        /// </summary>
        public NetworkRequestOptionFlags Flags
        {
            get { return _flags; }
            set
            {
                _flags = value;

                if ((_flags & NetworkRequestOptionFlags.UseCredentials) == 0)
                {
                    _authentication = null;
                }
                if ((_flags & NetworkRequestOptionFlags.UseCookies) == 0)
                {
                    _cookieContainer = null;
                }
                if ((_flags & NetworkRequestOptionFlags.AllowRedirections) == 0)
                {
                    _maxRedirections = 0;
                }
            }
        }

        /// <summary>
        /// The headers which should be sent with the request.
        /// <para/>
        /// This property is ignored when `<see langword="null"/>`.
        /// </summary>
        public HttpRequestHeaders Headers
        {
            get { return _headers; }
        }

        /// <summary>
        /// The maximum number of redirection responses that the handler follows.
        /// <para/>
        /// This property is ignored if `<see cref="Flags"/>` does not contain `<see cref="NetworkRequestOptionFlags.AllowRedirections"/>`.
        /// </summary>
        public int MaxRedirections
        {
            get { return _maxRedirections; }
            set
            {
                _maxRedirections = value;
                _flags = (_maxRedirections > 0)
                    ? (_flags | NetworkRequestOptionFlags.AllowRedirections)
                    : (_flags & ~NetworkRequestOptionFlags.AllowRedirections);

            }
        }

        /// <summary>
        /// The amount of time to wait before the request times out.
        /// <para/>
        /// This property is ignored if the value is less than `<see cref="TimeSpan.Zero"/>`.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
    }

    public interface INetwork
    {
        /// <summary>
        /// Send a GET request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<HttpResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options);

        /// <summary>
        /// Send a GET request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the server.</param>
        Task<HttpResponseMessage> HttpGetAsync(TargetUri targetUri);

        /// <summary>
        /// Send a HEAD request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the server.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<HttpResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options);

        /// <summary>
        /// Send a GET request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified target as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the target.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        Task<HttpResponseMessage> HttpHeadAsync(TargetUri targetUri);

        /// <summary>
        /// Send a POST request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="content">Content to send, as part of the request, to the server.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<HttpResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options);

        /// <summary>
        /// Send a POST request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="content">Content to send, as part of the request, to the server.</param>
        Task<HttpResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content);
    }

    internal class Network : Base, INetwork
    {
        private const string BearerPrefix = "Bearer ";
        private static readonly AuthenticationHeaderValue[] NullResult = new AuthenticationHeaderValue[0];

        public Network(RuntimeContext context)
            : base(context)
        { }

        public async Task<HttpResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            // Craft the request header for the GitHub v3 API w/ credentials;
            using (var handler = GetHttpMessageHandler(targetUri, options))
            using (var httpClient = GetHttpClient(targetUri, handler, options))
            {
                return await httpClient.GetAsync(targetUri);
            }
        }

        public Task<HttpResponseMessage> HttpGetAsync(TargetUri targetUri)
            => HttpGetAsync(targetUri, NetworkRequestOptions.Default);

        public async Task<HttpResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            using (var httpMessageHandler = GetHttpMessageHandler(targetUri, options))
            using (var httpClient = GetHttpClient(targetUri, httpMessageHandler, options))
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Head, targetUri))
            {
                // Copy the headers from the client into the message because the framework
                // will not do this when using `SendAsync`.
                foreach (var header in httpClient.DefaultRequestHeaders)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }

                return await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            }
        }

        public Task<HttpResponseMessage> HttpHeadAsync(TargetUri targetUri)
            => HttpHeadAsync(targetUri, NetworkRequestOptions.Default);

        public async Task<HttpResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (content is null)
                throw new ArgumentNullException(nameof(content));

            using (var handler = GetHttpMessageHandler(targetUri, options))
            using (var httpClient = GetHttpClient(targetUri, handler, options))
            {
                return await httpClient.PostAsync(targetUri, content);
            }
        }

        public Task<HttpResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content)
            => HttpPostAsync(targetUri, content, NetworkRequestOptions.Default);

        private static HttpMessageHandler GetHttpMessageHandler(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            var handler = new HttpClientHandler();

            if (options != null)
            {
                if ((options.Flags & NetworkRequestOptionFlags.AllowRedirections) != 0 && options.MaxRedirections <= 0)
                {
                    var inner = new ArgumentOutOfRangeException(nameof(options.MaxRedirections));
                    throw new ArgumentException("`NetworkRequestOption.MaxRedirections` must be greater than zero with `NetworkRequestOptionFlags.AllowAutoRedirect`.", nameof(options), inner);
                }

                if (options.CookieContainer is null && (options.Flags & NetworkRequestOptionFlags.UseCookies) != 0)
                {
                    var inner = new ArgumentNullException(nameof(options.CookieContainer));
                    throw new ArgumentException("`NetworkRequestOption.CookieContainer` cannot be null with `NetworkRequestOptionFlags.UseCookies`.", nameof(options), inner);
                }

                if ((options.Flags & NetworkRequestOptionFlags.UseCredentials) != 0 && options.Authorization is null)
                {
                    var inner = new ArgumentNullException(nameof(options.Authorization));
                    throw new ArgumentException("`NetworkRequestOption.Authentication` cannot be null with `NetworkRequestOptionFlags.UseCredentials`.", nameof(options), inner);
                }

                if ((options.Flags & NetworkRequestOptionFlags.AllowRedirections) == 0
                    || options.MaxRedirections <= 0)
                {
                    handler.AllowAutoRedirect = false;
                }
                else
                {
                    handler.AllowAutoRedirect = true;
                    handler.MaxAutomaticRedirections = options.MaxRedirections;
                }

                if ((options.Flags & NetworkRequestOptionFlags.UseCookies) != 0
                    && options.CookieContainer != null)
                {
                    handler.CookieContainer = options.CookieContainer;
                    handler.UseCookies = true;
                }

                if ((options.Flags & NetworkRequestOptionFlags.UseCredentials) != 0
                    && options.Authorization != null)
                {
                    handler.UseDefaultCredentials = false;
                }

                handler.PreAuthenticate = (options.Flags & NetworkRequestOptionFlags.PreAuthenticate) != 0;

                if ((options.Flags & NetworkRequestOptionFlags.UseProxy) != 0
                    && targetUri.ProxyUri != null)
                {
                    var proxy = GetHttpWebProxy(targetUri);

                    if (proxy != null)
                    {
                        handler.Proxy = proxy;
                        handler.UseProxy = true;
                    }
                }
            }

            return handler;
        }

        private HttpClient GetHttpClient(TargetUri targetUri, HttpMessageHandler handler, NetworkRequestOptions options)
        {
            var httpClient = new HttpClient(handler);

            if (options != null)
            {
                if (options.Timeout > TimeSpan.Zero)
                {
                    httpClient.Timeout = options.Timeout;
                }

                if (options.Headers != null)
                {
                    foreach (var header in options.Headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                // Manually add the correct headers for the type of authentication that is happening because if
                // we rely on the framework to correctly write the headers neither GitHub nor VSTS authentication
                // works reliably.
                if (options.Authorization != null)
                {
                    switch (options.Authorization)
                    {
                        case Token token:
                            {
                                // Different types of tokens are packed differently.
                                switch (token.Type)
                                {
                                    case TokenType.AzureAccess:
                                        {
                                            // ADAL access tokens are packed into the Authorization header.
                                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
                                        }
                                        break;

                                    case TokenType.AzureFederated:
                                        {
                                            // Federated authentication tokens are sent as cookie(s).
                                            httpClient.DefaultRequestHeaders.Add("Cookie", token.Value);
                                        }
                                        break;

                                    default:
                                        Trace.WriteLine("! unsupported token type, not appeding an authentication header to the request.");
                                        break;
                                }
                            }
                            break;

                        case Credential credentials:
                            {
                                // Credentials are packed into the 'Authorization' header as a base64 encoded pair.
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials.ToBase64String());
                            }
                            break;
                    }
                }
            }

            // Ensure that the user-agent string is set.
            if (httpClient.DefaultRequestHeaders.UserAgent is null
                || httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Global.UserAgent);
            }

            return httpClient;
        }

        private static IWebProxy GetHttpWebProxy(TargetUri targetUri)
        {
            if (targetUri is null || targetUri.ProxyUri is null)
                return null;

            var proxyUri = targetUri.ProxyUri;

            if (proxyUri != null)
            {
                WebProxy proxy = new WebProxy(proxyUri) { UseDefaultCredentials = true };

                // check if the user has specified authentications (comes as UserInfo)
                if (!string.IsNullOrWhiteSpace(proxyUri.UserInfo) && proxyUri.UserInfo.Length > 1)
                {
                    int tokenIndex = proxyUri.UserInfo.IndexOf(':');
                    bool hasUserNameAndPassword = tokenIndex != -1;

                    if (hasUserNameAndPassword)
                    {
                        string userName = proxyUri.UserInfo.Substring(0, tokenIndex);
                        string password = proxyUri.UserInfo.Substring(tokenIndex + 1);

                        NetworkCredential proxyCreds = new NetworkCredential(userName, password);

                        proxy.UseDefaultCredentials = false;
                        proxy.Credentials = proxyCreds;
                    }
                }

                return proxy;
            }
            else
            {
                return new WebProxy();
            }
        }
    }
}
