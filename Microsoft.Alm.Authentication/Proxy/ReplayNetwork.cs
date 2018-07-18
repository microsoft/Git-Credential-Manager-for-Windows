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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class ReplayNetwork : INetwork, IReplayService<CapturedNetworkData>
    {
        internal ReplayNetwork(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captured = new Dictionary<CaptureNetwork.StringPair, Dictionary<string, Queue<CapturedNetworkQuery>>>(CaptureNetwork.StringPair.Comparer);
            _context = context;
        }

        private readonly Dictionary<CaptureNetwork.StringPair, Dictionary<string, Queue<CapturedNetworkQuery>>> _captured;
        private readonly RuntimeContext _context;

        public string ServiceName
            => "Network";

        public Type ServiceType
            => typeof(INetwork);

        public Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (!TryReadNext(nameof(HttpGetAsync), targetUri, out CapturedNetworkQuery query))
                throw new ReplayNotFoundException($"Failed to find replay data for \"{targetUri}\".");
            if (query.Request.OptionFlags != (int)options.Flags)
                throw new ReplayDataException($"Unexpected `{nameof(query.Request.OptionFlags)}`, expected {query.Request.OptionFlags} vs. actual {(int)options.Flags}.");

            var content = (query.Response.Content.HasContent)
                ? new ReplayContent(query.Response.Content)
                : null;
            var headers = new NetworkResponseHeaders();
            headers.SetHeaders(query.Response.Headers);

            var response = new NetworkResponseMessage(content, headers, (HttpStatusCode)query.Response.StatusCode);

            return Task.FromResult<INetworkResponseMessage>(response);
        }

        public Task<INetworkResponseMessage> HttpGetAsync(TargetUri targetUri)
            => HttpGetAsync(targetUri, NetworkRequestOptions.Default);

        public Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri, NetworkRequestOptions options)
        {
            if (!TryReadNext(nameof(HttpHeadAsync), targetUri, out CapturedNetworkQuery query))
                throw new ReplayNotFoundException($"Failed to find replay data for \"{targetUri}\".");
            if (query.Request.OptionFlags != (int)options.Flags)
                throw new ReplayDataException($"Unexpected `{nameof(query.Request.OptionFlags)}`, expected {query.Request.OptionFlags} vs. actual {(int)options.Flags}.");

            var headers = new NetworkResponseHeaders();
            headers.SetHeaders(query.Response.Headers);

            var response = new NetworkResponseMessage(null, headers, (HttpStatusCode)query.Response.StatusCode);

            return Task.FromResult<INetworkResponseMessage>(response);
        }

        public Task<INetworkResponseMessage> HttpHeadAsync(TargetUri targetUri)
        => HttpHeadAsync(targetUri, NetworkRequestOptions.Default);

        public Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, HttpContent content, NetworkRequestOptions options)
        {
            if (!TryReadNext(nameof(HttpPostAsync), targetUri, out CapturedNetworkQuery query))
                throw new ReplayNotFoundException($"Failed to find replay data for \"{targetUri}\".");
            if (query.Request.OptionFlags != (int)options.Flags)
                throw new ReplayDataException($"Unexpected `{nameof(query.Request.OptionFlags)}`, expected {query.Request.OptionFlags} vs. actual {(int)options.Flags}.");

            var responseContent = (query.Response.Content.HasContent)
                ? new ReplayContent(query.Response.Content)
                : null;
            var headers = new NetworkResponseHeaders();
            headers.SetHeaders(query.Response.Headers);

            var response = new NetworkResponseMessage(responseContent, headers, (HttpStatusCode)query.Response.StatusCode);

            return Task.FromResult<INetworkResponseMessage>(response);
        }

        public Task<INetworkResponseMessage> HttpPostAsync(TargetUri targetUri, StringContent content)
            => HttpPostAsync(targetUri, content, NetworkRequestOptions.Default);

        internal void SetReplayData(CapturedNetworkData data)
        {
            if (data.Operations is null)
                return;

            foreach (var operation in data.Operations)
            {
                if (operation.Methods is null)
                    continue;

                var urlPair = new CaptureNetwork.StringPair
                {
                    String1 = operation.QueryUrl,
                    String2 = operation.ProxyUrl,
                };

                if (!_captured.TryGetValue(urlPair, out var byMethod))
                {
                    byMethod = new Dictionary<string, Queue<CapturedNetworkQuery>>(Ordinal);

                    _captured.Add(urlPair, byMethod);
                }

                foreach (var method in operation.Methods)
                {
                    if (method.Queries is null)
                        continue;

                    if (!byMethod.TryGetValue(method.Method, out var queries))
                    {
                        queries = new Queue<CapturedNetworkQuery>();

                        byMethod.Add(method.Method, queries);
                    }

                    foreach (var query in method.Queries)
                    {
                        queries.Enqueue(query);
                    }
                }
            }
        }

        private bool TryReadNext(string method, TargetUri targetUri, out CapturedNetworkQuery query)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            var urlPair = new CaptureNetwork.StringPair
            {
                String1 = targetUri?.QueryUri?.ToString(),
                String2 = targetUri?.ProxyUri?.ToString(),
            };

            if (!_captured.TryGetValue(urlPair, out var byMethod)
                || !byMethod.TryGetValue(method, out var queries)
                || queries.Count == 0)
            {
                query = default(CapturedNetworkQuery);
                return false;
            }

            query = queries.Dequeue();
            return true;
        }

        string IProxyService.ServiceName
            => ServiceName;

        void IReplayService<CapturedNetworkData>.SetReplayData(CapturedNetworkData replayData)
            => SetReplayData(replayData);

        void IReplayService.SetReplayData(object replayData)
        {
            if (!(replayData is CapturedNetworkData networkData)
                && !CapturedNetworkData.TryDeserialize(replayData, out networkData))
            {
                var inner = new InvalidDataException($"Failed to deserialize data into `{nameof(CapturedNetworkData)}`.");
                throw new ArgumentException(inner.Message, nameof(replayData), inner);
            }

            SetReplayData(networkData);
        }

        private class ReplayContent : INetworkResponseContent
        {
            public ReplayContent(CapturedNetworkContent content)
            {
                _content = content;
            }

            private readonly CapturedNetworkContent _content;

            public byte[] AsByteArray
            {
                get { return _content.AsBytes; }
            }

            public string AsString
            {
                get { return _content.AsString; }
            }

            public bool IsByteArray
            {
                get { return _content.AsBytes != null; }
            }

            public bool IsString
            {
                get { return _content.AsString != null; }
            }

            public string MediaType
            {
                get { return _content.ContentType; }
            }
        }
    }
}
