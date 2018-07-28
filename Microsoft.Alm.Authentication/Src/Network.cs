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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static System.StringComparer;

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
        internal const string AcceptName = "accept";
        internal const string AcceptValue = "*/*";
        internal const string AcceptEncodingName = "accept-encoding";
        internal const string AcceptEncodingDeflate = "deflate";
        internal const string AcceptEncodingGzip = "gzip";
        internal const string CacheControlName = "cache-control";
        internal const string CacheControlValue = "no-cache";

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
            _headers.Add(AcceptName, AcceptValue);
            _headers.Add(AcceptEncodingName, AcceptEncodingGzip);
            _headers.Add(AcceptEncodingName, AcceptEncodingDeflate);
            _headers.Add(CacheControlName, CacheControlValue);
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

    public interface INetworkResponseContent
    {
        /// <summary>
        /// Gets the HTTP content serialized to a `<see langword="byte"/>[]`.
        /// </summary>
        byte[] AsByteArray { get; }

        /// <summary>
        /// Gets the HTTP content serialized to a `<see langword="string"/>`.
        /// </summary>
        string AsString { get; }

        /// <summary>
        /// Gets `<see langword="true"/>` if the HTTP content is represented as a `<see langword="byte"/>[]` array; otherwise `<see langword="false"/>`.
        /// </summary>
        bool IsByteArray { get; }

        /// <summary>
        /// Gets `<see langword="true"/>` if the HTTP content is represented as a `<see langword="string"/>`; otherwise `<see langword="false"/>`.
        /// </summary>
        bool IsString { get; }

        /// <summary>
        /// Gets the media-type header value associated with the content.
        /// </summary>
        string MediaType { get; }
    }

    public interface INetworkResponseHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        IEnumerable<AuthenticationHeaderValue> WwwAuthenticate { get; }

        bool TryGetValues(string name, out IEnumerable<string> values);
    }

    public interface INetworkResponseMessage : IDisposable
    {
        /// <summary>
        /// Gets or sets the content of a HTTP response message.
        /// </summary>
        INetworkResponseContent Content { get; }

        /// <summary>
        /// Gets or sets the status code of the HTTP response.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the collection of HTTP response headers.
        /// </summary>
        INetworkResponseHeaders Headers { get; }

        /// <summary>
        /// Gets a value that indicates if the HTTP response was successful.
        /// </summary>
        bool IsSuccessStatusCode { get; }
    }

    public interface INetwork : IRuntimeService
    {
        /// <summary>
        /// Send a GET request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options);

        /// <summary>
        /// Send a GET request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the server.</param>
        Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri);

        /// <summary>
        /// Send a HEAD request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the server.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options);

        /// <summary>
        /// Send a GET request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified target as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the target.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri);

        /// <summary>
        /// Send a POST request, using `<paramref name="options"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="content">Content to send, as part of the request, to the server.</param>
        /// <param name="options">Options which govern how the HTTP request is sent.</param>
        Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options);

        /// <summary>
        /// Send a POST request, using `<see cref="NetworkRequestOptions.Default"/>`, to the specified server as an asynchronous operation.
        /// <para/>
        /// Returns a task to get the response from the server.
        /// </summary>
        /// <param name="targetUri">The Uri of the target.</param>
        /// <param name="content">Content to send, as part of the request, to the server.</param>
        Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content);
    }

    internal class Network : Base, INetwork
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private static readonly AuthenticationHeaderValue[] NullResult = new AuthenticationHeaderValue[0];

        public Network(RuntimeContext context)
            : base(context)
        { }

        public Type ServiceType
            => typeof(INetwork);

        public Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            return Task.Run<INetworkResponseMessage>(async () =>
            {
                using (var handler = GetHttpMessageHandler(targetUri, options))
                using (var httpClient = GetHttpClient(targetUri, handler, options))
                {
                    var httpMessage = await httpClient.GetAsync(targetUri);
                    var response = new NetworkResponseMessage(httpMessage);

                    if (httpMessage.Content != null)
                    {
                        await response.SetContent(httpMessage.Content);
                    }

                    return response;
                }
            });
        }

        public Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri)
            => HttpGetAsync(targetUri, NetworkRequestOptions.Default);

        public Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            return Task.Run<INetworkResponseMessage>(async () =>
            {
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

                    var httpMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    var response = new NetworkResponseMessage(httpMessage);

                    if (httpMessage.Content != null)
                    {
                        await response.SetContent(httpMessage.Content);
                    }

                    return response;
                }
            });
        }

        public Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri)
            => HttpHeadAsync(targetUri, NetworkRequestOptions.Default);

        public Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (content is null)
                throw new ArgumentNullException(nameof(content));

            return Task.Run<INetworkResponseMessage>(async () =>
            {
                using (var handler = GetHttpMessageHandler(targetUri, options))
                using (var httpClient = GetHttpClient(targetUri, handler, options))
                {
                    var httpMessage = await httpClient.PostAsync(targetUri, content);
                    var response = new NetworkResponseMessage(httpMessage);

                    if (httpMessage.Content != null)
                    {
                        await response.SetContent(httpMessage.Content);
                    }

                    return response;
                }
            });
        }

        public Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content)
            => HttpPostAsync(targetUri, content, NetworkRequestOptions.Default);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "targetUri")]
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
                                case TokenType.BitbucketAccess:
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
                                Trace.WriteLine("! unsupported token type, not appending an authentication header to the request.");
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

    internal class NetworkResponseContent : INetworkResponseContent
    {
        public const string AcceptEncodingDeflate = NetworkRequestOptions.AcceptEncodingDeflate;
        public const string AcceptEncodingGzip = NetworkRequestOptions.AcceptEncodingGzip;

        private byte[] _byteArray;
        private string _mediaType;
        private string _string;
        private readonly object _syncpoint = new object();

        public byte[] AsByteArray
        {
            get { lock (_syncpoint) return _byteArray; }
        }

        public string AsString
        {
            get { lock (_syncpoint) return _string; }
        }

        public bool IsByteArray
        {
            get { lock (_syncpoint) return _byteArray != null; }
        }

        public bool IsString
        {
            get { lock (_syncpoint) return _string != null; }
        }

        public string MediaType
        {
            get { lock (_syncpoint) return _mediaType; }
        }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(NetworkResponseContent)}: {ToString()}"; }
        }

        public async Task SetContent(HttpContent content)
        {
            if (content is null)
                throw new ArgumentNullException(nameof(content));

            if (content.Headers.ContentType.MediaType != null
                && (content.Headers.ContentType.MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                    || content.Headers.ContentType.MediaType.EndsWith("/json", StringComparison.OrdinalIgnoreCase)))
            {
                string asString = null;

                if (content.Headers.ContentEncoding.Any(e => OrdinalIgnoreCase.Equals(AcceptEncodingGzip, e)))
                {
                    using (var stream = await content.ReadAsStreamAsync())
                    using (var inflate = new GZipStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(inflate, Encoding.UTF8))
                    {
                        asString = reader.ReadToEnd();
                    }
                }
                else if (content.Headers.ContentEncoding.Any(e => OrdinalIgnoreCase.Equals(AcceptEncodingDeflate, e)))
                {
                    using (var stream = await content.ReadAsStreamAsync())
                    using (var inflate = new DeflateStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(inflate, Encoding.UTF8))
                    {
                        asString = reader.ReadToEnd();
                    }
                }
                else
                {
                    asString = await content.ReadAsStringAsync();
                }

                lock (_syncpoint)
                {
                    _mediaType = content.Headers.ContentType.MediaType;
                    _string = asString;
                }
            }
            else
            {
                byte[] asBytes = null;

                if (content.Headers.ContentEncoding.Any(e => OrdinalIgnoreCase.Equals(AcceptEncodingGzip, e)))
                {
                    using (var stream = await content.ReadAsStreamAsync())
                    using (var inflate = new GZipStream(stream, CompressionMode.Decompress))
                    using (var memory = new MemoryStream())
                    {
                        inflate.CopyTo(memory);

                        asBytes = memory.ToArray();
                    }
                }
                else if (content.Headers.ContentEncoding.Any(e => OrdinalIgnoreCase.Equals(AcceptEncodingDeflate, e)))
                {
                    using (var stream = await content.ReadAsStreamAsync())
                    using (var inflate = new DeflateStream(stream, CompressionMode.Decompress))
                    using (var memory = new MemoryStream())
                    {
                        inflate.CopyTo(memory);

                        asBytes = memory.ToArray();
                    }
                }
                else
                {
                    asBytes = await content.ReadAsByteArrayAsync();
                }

                lock (_syncpoint)
                {
                    _mediaType = content.Headers.ContentType.MediaType;
                    _byteArray = asBytes;
                }
            }
        }

        public override string ToString()
        {
            byte[] asBytes = null;
            string asString = null;

            lock (_syncpoint)
            {
                asBytes = AsByteArray;
                asString = AsString;
            }

            return (asBytes is null)
                ? (asString is null)
                    ? "<Empty>"
                    : $"{nameof(AsString)} Length = {asString.Length}"
                : (asString is null)
                    ? $"{nameof(AsByteArray)} Length = {asBytes.Length}"
                    : "<Error>";
        }
    }

    internal class NetworkResponseHeaders : INetworkResponseHeaders
    {
        private readonly Dictionary<string, List<string>> _lookup = new Dictionary<string, List<string>>(OrdinalIgnoreCase);

        public IEnumerable<AuthenticationHeaderValue> WwwAuthenticate
        {
            get
            {
                const string WwwAuthenticate = "WWW-Authenticate";

                if (_lookup.TryGetValue(WwwAuthenticate, out var values))
                {
                    var headers = new List<AuthenticationHeaderValue>(values.Count);

                    foreach (var value in values)
                    {
                        var header = null as AuthenticationHeaderValue;
                        int index = value.IndexOf(' ');

                        if (index > 0)
                        {
                            string scheme = value.Substring(0, index - 1);
                            string parameter = value.Substring(index);

                            header = new AuthenticationHeaderValue(scheme, parameter);
                        }
                        else
                        {
                            header = new AuthenticationHeaderValue(value);
                        }

                        headers.Add(header);
                    }

                    return headers;
                }

                return System.Linq.Enumerable.Empty<AuthenticationHeaderValue>();
            }
        }

        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            foreach (var kvp in _lookup)
            {
                var result = new KeyValuePair<string, IEnumerable<string>>(kvp.Key, kvp.Value);

                yield return result;
            }
        }

        public void SetHeaders(IEnumerable<string> data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            foreach (var item in data)
            {
                int idx = item.IndexOf('=');
                if (idx < 0)
                    continue;

                string name = item.Substring(0, idx);
                string value = item.Substring(idx + 1);

                if (!_lookup.TryGetValue(name, out var list))
                {
                    list = new List<string>();
                    _lookup.Add(name, list);
                }

                list.Add(value);
            }
        }

        public void SetHeaders(HttpResponseHeaders data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            foreach (var item in data)
            {
                var name = item.Key;
                var values = item.Value;

                if (!_lookup.TryGetValue(name, out var list))
                {
                    list = new List<string>();
                    _lookup.Add(name, list);
                }

                list.AddRange(values);
            }
        }

        public bool TryGetValues(string name, out IEnumerable<string> values)
        {
            if (_lookup.TryGetValue(name, out var vs))
            {
                values = vs;
                return true;
            }

            values = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal class NetworkResponseMessage : INetworkResponseMessage
    {
        public NetworkResponseMessage(INetworkResponseContent content, INetworkResponseHeaders headers, HttpStatusCode status)
            : this()
        {
            _content = content;
            _headers = headers;
            _message = null;
            _status = status;
        }

        public NetworkResponseMessage(HttpResponseMessage httpMessage)
            : this()
        {
            if (httpMessage is null)
                throw new ArgumentNullException(nameof(httpMessage));

            _message = httpMessage;

            var headers = new NetworkResponseHeaders();
            headers.SetHeaders(httpMessage.Headers);

            _headers = headers;
            _status = httpMessage.StatusCode;
        }

        private NetworkResponseMessage()
        {
            _syncpoint = new object();
        }

        ~NetworkResponseMessage()
        {
            Dispose(true);
        }

        private INetworkResponseContent _content;
        private readonly INetworkResponseHeaders _headers;
        private HttpResponseMessage _message;
        private readonly HttpStatusCode _status;
        private readonly object _syncpoint;

        public INetworkResponseContent Content
        {
            get { lock (_syncpoint) return _content; }
        }

        public HttpStatusCode StatusCode
        {
            get { lock (_syncpoint) return _status; }
        }

        public INetworkResponseHeaders Headers
        {
            get { lock (_syncpoint) return _headers; }
        }

        public bool IsSuccessStatusCode
        {
            get { lock (_syncpoint) return (_status >= HttpStatusCode.OK && _status < HttpStatusCode.Ambiguous); }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Dispose(false);
        }

        private void Dispose(bool finalizing)
        {
            if (finalizing)
            {
                _message?.Dispose();
            }
            else
            {
                _content = null;

                if (_message != null)
                {
                    _message.Dispose();
                    _message = null;
                }
            }
        }

        public async Task SetContent(HttpContent httpContent)
        {
            if (httpContent is null)
                return;

            var content = new NetworkResponseContent();

            await content.SetContent(httpContent);

            lock (_syncpoint)
            {
                _content = content;
            }
        }
    }
}
