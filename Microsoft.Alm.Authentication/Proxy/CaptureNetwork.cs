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
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class CaptureNetwork : ICaptureService<CapturedNetworkData>, INetwork
    {
        private const string SubstituteEmail = "tester@testing.com";
        private const string SubstituteGuid = "00001111-2222-3333-4444-555566667777";
        private const string SubstituteTokenCompact = "012345689abcdefthisisafaketokenfedcba9876543210";
        private const string SubstituteTokenComplete = SubstituteTokenCompact + "_[0.0]_" + SubstituteTokenCompact;
        private const string SubstituteUser = "Tester";

        internal static readonly IReadOnlyList<Regex> AllowedHeaderNames
            = new List<Regex>
            {
                new Regex(@"^Accept", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^Content\-(Length|Type)$", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^Status", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^User\-Agent", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^Www\-Authenticate", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^X\-GitHub\-", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^X\-TFS\-", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase),
                new Regex(@"^X\-VSS\-", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            };
        internal static readonly IReadOnlyDictionary<string, string> CapturedNetworkHeaderValues
            = new Dictionary<string, string>(OrdinalIgnoreCase)
            {
                { "X-GitHub-Request-Id", "000:1112:2233334:4455566:6777888" },
                { "X-VSS-UserData", SubstituteGuid + ":" + SubstituteEmail },
            };
        internal static readonly IReadOnlyList<(Regex Regex, string Replacement)> DataFilters
            = new List<(Regex, string)>
            {
                (new Regex(@"""id\\"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"id\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""authenticatedUser\\"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"authenticatedUser\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""providerDisplayName"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"providerDisplayName\":\"" + SubstituteUser + "\""),
                (new Regex(@"[^""\\;@]+@[^""\\;]+\.[^""\\;]+", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), SubstituteEmail),
                (new Regex(@"""instanceId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"instanceId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""deploymentId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"deploymentId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""serviceOwner"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"serviceOwner\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""identifier"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"identifier\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""serviceOwner"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"serviceOwner\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""parentIdentifier"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"parentIdentifier\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""accessId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"accessId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""authorizationId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"authorizationId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""hostAuthorizationId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"hostAuthorizationId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""userId"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"userId\":\"" + SubstituteGuid + "\""),
                (new Regex(@"""targetAccounts"":\[[^\]]+\]", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"targetAccounts\":[\"" + SubstituteGuid + "\"]"),
                (new Regex(@"""token"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"token\":\"" + SubstituteTokenCompact + "\""),
                (new Regex(@"""alternateToken"":""[^""]+""", RegexOptions.CultureInvariant| RegexOptions.IgnoreCase), "\"alternateToken\":\"" + SubstituteTokenComplete + "\""),
            };

        public CaptureNetwork(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captured = new Dictionary<StringPair, Dictionary<string, List<CapturedNetworkQuery>>>(StringPair.Comparer);
            _context = context;
            _network = context.Network;
            _queryOridinal = 0;
            _syncpoint = new object();
        }

        private readonly Dictionary<StringPair, Dictionary<string, List<CapturedNetworkQuery>>> _captured;
        private readonly RuntimeContext _context;
        private readonly INetwork _network;
        private int _queryOridinal;
        private readonly object _syncpoint;

        public string ServiceName
            => "Network";

        public Type ServiceType
            => typeof(INetwork);

        public async Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            var result = await _network.HttpGetAsync(targetUri, options);

            await Capture(nameof(HttpGetAsync), targetUri, options, null, result);

            return result;
        }

        public Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri)
            => HttpGetAsync(targetUri, NetworkRequestOptions.Default);

        public async Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            var result = await _network.HttpHeadAsync(targetUri, options);

            await Capture(nameof(HttpHeadAsync), targetUri, options, null, result);

            return result;
        }

        public Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri)
            => HttpHeadAsync(targetUri, NetworkRequestOptions.Default);

        public async Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options)
        {
            HttpContent copiedContent = null;

            using (var stream = new System.IO.MemoryStream())
            {
                await content.CopyToAsync(stream);

                copiedContent = new ByteArrayContent(stream.ToArray());
            }

            var result = await _network.HttpPostAsync(targetUri, content, options);

            await Capture(nameof(HttpPostAsync), targetUri, options, copiedContent, result);

            return result;
        }

        public Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content)
            => HttpPostAsync(targetUri, content, NetworkRequestOptions.Default);

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedNetworkData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            filter = new CapturedDataFilter(filter);

            foreach (var item in DataFilters)
            {
                filter.AddFilter(item.Regex, item.Replacement);
            }

            lock (_syncpoint)
            {
                var operations = new List<CapturedNetworkOperation>(_captured.Count);

                foreach (var capture in _captured)
                {
                    var captureKey = capture.Key;
                    var capturedMethods = capture.Value;

                    var operation = new CapturedNetworkOperation
                    {
                        QueryUrl = filter.ApplyFilter(captureKey.String1),
                        ProxyUrl = filter.ApplyFilter(captureKey.String2),
                        Methods = new List<CapturedNetworkMethod>(capturedMethods.Count),
                    };

                    foreach (var methodItem in capturedMethods)
                    {
                        var methodName = methodItem.Key;
                        var capturedQueries = methodItem.Value;

                        var queries = new List<CapturedNetworkQuery>(capturedQueries.Count);

                        var method = new CapturedNetworkMethod
                        {
                            Method = methodName,
                            Queries = queries,
                        };

                        foreach (var capturedQuery in capturedQueries)
                        {
                            var requestHeaders = null as List<string>;

                            if (capturedQuery.Request.Headers != null)
                            {
                                requestHeaders = new List<string>();

                                foreach (var capturedHeader in capturedQuery.Request.Headers)
                                {
                                    var header = filter.ApplyFilter(capturedHeader);

                                    requestHeaders.Add(header);
                                }
                            }

                            var responseHeaders = new List<string>();

                            if (capturedQuery.Response.Headers != null)
                            {
                                responseHeaders = new List<string>();

                                foreach (var capturedHeader in capturedQuery.Response.Headers)
                                {
                                    var header = filter.ApplyFilter(capturedHeader);

                                    responseHeaders.Add(capturedHeader);
                                }
                            }

                            var query = new CapturedNetworkQuery
                            {
                                Ordinal = capturedQuery.Ordinal,
                                Request = new CapturedNetworkRequest
                                {
                                    Content = filter.ApplyFilter(capturedQuery.Request.Content),
                                    Headers = requestHeaders,
                                    OptionFlags = capturedQuery.Request.OptionFlags,
                                },
                                Response = new CapturedNetworkResponse
                                {
                                    Content = new CapturedNetworkContent
                                    {
                                        AsBytes = capturedQuery.Response.Content.AsBytes,
                                        AsString = filter.ApplyFilter(capturedQuery.Response.Content.AsString),
                                        ContentType = capturedQuery.Response.Content.ContentType,
                                    },
                                    Headers = responseHeaders,
                                    StatusCode = capturedQuery.Response.StatusCode,
                                },
                            };

                            queries.Add(query);
                        }

                        operation.Methods.Add(method);
                    }

                    operations.Add(operation);
                }

                capturedData = new CapturedNetworkData
                {
                    Operations = operations,
                };
                return true;
            }
        }

        private async Task Capture(string methodName, TargetUri targetUri, NetworkRequestOptions options, HttpContent content, INetworkResponseMessage responseMessage)
        {
            var request = new CapturedNetworkRequest { OptionFlags = (int)options.Flags, };

            _context.Trace.WriteLine(Invariant($"{nameof(CaptureNetwork)}: `{methodName}` \"{targetUri}\"."));

            if (content != null)
            {
                request.Content = await content.ReadAsStringAsync();
            }

            if (options.Headers != null)
            {
                request.Headers = new List<string>();

                foreach (var header in options.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        var headerName = header.Key;
                        var headerValue = value;

                        if (CaptureHeader(headerName, ref headerValue))
                        {
                            request.Headers.Add(Invariant($"{headerName}={headerValue}"));
                        }
                    }
                }
            }

            var response = new CapturedNetworkResponse { };

            if (responseMessage != null)
            {
                response.StatusCode = (int)responseMessage.StatusCode;

                if (responseMessage.Headers != null)
                {
                    response.Headers = new List<string>();

                    foreach (var header in responseMessage.Headers)
                    {
                        foreach (var value in header.Value)
                        {
                            var headerName = header.Key;
                            var headerValue = value;

                            if (CaptureHeader(headerName, ref headerValue))
                            {
                                response.Headers.Add(Invariant($"{headerName}={headerValue}"));
                            }
                        }
                    }
                }

                if (responseMessage.Content != null)
                {
                    if (responseMessage.Content.IsString)
                    {
                        response.Content = new CapturedNetworkContent
                        {
                            AsString = responseMessage.Content.AsString,
                            ContentType = responseMessage.Content.MediaType,
                        };
                    }
                    else
                    {
                        response.Content = new CapturedNetworkContent
                        {
                            AsBytes = responseMessage.Content.AsByteArray,
                            ContentType = responseMessage.Content.MediaType,
                        };
                    }
                }
            }

            var queryUrl = targetUri.QueryUri.ToString();
            var proxyUrl = targetUri.ProxyUri?.ToString();
            var key = new StringPair()
            {
                String1 = queryUrl,
                String2 = proxyUrl,
            };

            lock (_syncpoint)
            {
                if (!_captured.TryGetValue(key, out var methods))
                {
                    methods = new Dictionary<string, List<CapturedNetworkQuery>>(Ordinal);

                    _captured.Add(key, methods);
                }

                if (!methods.TryGetValue(methodName, out var queries))
                {
                    queries = new List<CapturedNetworkQuery>();

                    methods.Add(methodName, queries);
                }

                var query = new CapturedNetworkQuery
                {
                    Ordinal = Interlocked.Increment(ref _queryOridinal),
                    Request = request,
                    Response = response,
                };

                queries.Add(query);
            }
        }

        private static bool CaptureHeader(string name, ref string value)
        {
            if (CapturedNetworkHeaderValues.TryGetValue(name, out string replacement))
            {
                value = replacement;
                return true;
            }

            foreach (var allow in AllowedHeaderNames)
            {
                if (allow.IsMatch(name))
                    return true;
            }

            return false;
        }

        string IProxyService.ServiceName
            => ServiceName;

        bool ICaptureService<CapturedNetworkData>.GetCapturedData(ICapturedDataFilter filter, out CapturedNetworkData capturedData)
            => GetCapturedData(filter, out capturedData);

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedNetworkData capturedNetworkData))
            {
                capturedData = capturedNetworkData;
                return true;
            }

            capturedData = null;
            return false;
        }

        internal class StringPairComparer : IComparer<StringPair>, IEqualityComparer<StringPair>
        {
            public int Compare(StringPair lhs, StringPair rhs)
            {
                int cmp = OrdinalIgnoreCase.Compare(lhs.String1, rhs.String1);

                if (cmp == 0)
                    return OrdinalIgnoreCase.Compare(lhs.String2, rhs.String2);

                return cmp;
            }

            public bool Equals(StringPair lhs, StringPair rhs)
            {
                return OrdinalIgnoreCase.Equals(lhs.String1, rhs.String1)
                    && OrdinalIgnoreCase.Equals(lhs.String2, rhs.String2);
            }

            public int GetHashCode(StringPair obj)
            {
                if (obj.String1 is null && obj.String2 is null)
                    return 0;

                return obj.String1.GetHashCode();
            }
        }

        internal struct StringPair : IComparable<StringPair>, IEquatable<StringPair>
        {
            public static readonly StringPairComparer Comparer = new StringPairComparer();

            public string String1;
            public string String2;

            public int CompareTo(StringPair other)
                => Comparer.Compare(this, other);

            public bool Equals(StringPair other)
                => Comparer.Equals(this, other);

            public override bool Equals(object obj)
            {
                return (obj is StringPair other && Equals(other))
                    || base.Equals(obj);
            }

            public override int GetHashCode()
                => Comparer.GetHashCode(this);

            public override string ToString()
            {
                if (String1 is null)
                {
                    if (String2 is null)
                        return null;

                    return String2;
                }

                if (String2 is null)
                    return String1;

                return Invariant($"{{ \"{String1}\" | \"{String2}\" }}");
            }
        }
    }
}
