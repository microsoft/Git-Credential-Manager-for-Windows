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
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Test;

namespace AzureDevOps.Authentication.Test
{
    public class CaptureAdal : IAdal, ICaptureService<CapturedAdalData>
    {
        internal const string CapturedAdalAccessToken = "Fake+Token;Fake+Token;Fake+Token;Fake+Token;Fake+Token;Fake+Token;Fake+Token;Fake+Token";

        internal CaptureAdal(RuntimeContext context, IAdal adal)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (adal is null)
                throw new ArgumentNullException(nameof(adal));

            _adal = adal;
            _captures = new List<CapturedAdalOperation>();
            _context = context;
            _syncpoint = new object();
        }

        private readonly IAdal _adal;
        private readonly List<CapturedAdalOperation> _captures;
        private readonly RuntimeContext _context;
        private readonly object _syncpoint;

        public string ServiceName
            => "Adal";

        public Type ServiceType
            => typeof(IAdal);

        public async Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string clientId, Uri redirectUri, string extraQueryParameters)
        {
            object output = null;

            try
            {
                output = await _adal.AcquireTokenAsync(authorityHostUrl, resource, clientId, redirectUri, extraQueryParameters);
            }
            catch (Exception adalException)
            {
                output = adalException;
            }

            var input = new CapturedAdalInput
            {
                ClientId = clientId,
                ExtraQueryParameters = extraQueryParameters,
                RedirectUrl = redirectUri?.ToString(),
                Resource = resource,
            };

            Capture(authorityHostUrl, input, output);

            switch (output)
            {
                case Exception exception:
                throw exception;

                case IAdalResult result:
                return result;
            }

            throw new InvalidOperationException($"Unexpected result from `{nameof(AcquireTokenAsync)}`.");
        }

        public async Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string client)
        {
            object output = null;

            try
            {
                output = await _adal.AcquireTokenAsync(authorityHostUrl, resource, client);
            }
            catch (Exception adalException)
            {
                output = adalException;
            }
            var input = new CapturedAdalInput
            {
                ClientId = client,
                Resource = resource,
            };

            Capture(authorityHostUrl, input, output);

            switch (output)
            {
                case Exception exception:
                throw exception;

                case IAdalResult result:
                return result;
            }

            throw new InvalidOperationException($"Unexpected result from `{nameof(AcquireTokenAsync)}`.");
        }

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedAdalData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            var operations = new List<CapturedAdalOperation>(_captures.Count);

            foreach (var capture in _captures)
            {

                var operation = new CapturedAdalOperation
                {
                    AuthorityUrl = filter.ApplyFilter(capture.AuthorityUrl),
                    Error = new CapturedAdalException
                    {
                        Message = filter.ApplyFilter(capture.Error.Message),
                    },
                    Input = new CapturedAdalInput
                    {
                        ClientId = filter.ApplyFilter(capture.Input.ClientId),
                        ExtraQueryParameters = filter.ApplyFilter(capture.Input.ExtraQueryParameters),
                        RedirectUrl = filter.ApplyFilter(capture.Input.RedirectUrl),
                        Resource = filter.ApplyFilter(capture.Input.Resource),
                    },
                    Result = new CapturedAdalResult
                    {
                        AccessToken = capture.Result.AccessToken is null ? null : CapturedAdalAccessToken,
                        Authority = filter.ApplyFilter(capture.Result.Authority),
                        TenantId = capture.Result.TenantId,
                        TokenType = capture.Result.TokenType,
                    },
                };

                operations.Add(operation);
            }

            capturedData = new CapturedAdalData
            {
                Operations = operations,
            };

            return true;
        }

        private void Capture(string authorityUrl, CapturedAdalInput input, object output)
        {
            if (authorityUrl is null)
                throw new ArgumentNullException(nameof(authorityUrl));

            _context.Trace.WriteLine($"{nameof(CaptureAdal)}: \"{nameof(authorityUrl)}\".");

            lock (_syncpoint)
            {
                var capture = new CapturedAdalOperation
                {
                    AuthorityUrl = authorityUrl,
                    Input = input,
                };

                if (output is Exception error)
                {
                    capture.Error = new CapturedAdalException
                    {
                        Message = error.Message,
                    };
                }
                else if (output is IAdalResult result)
                {
                    capture.Result = new CapturedAdalResult
                    {
                        AccessToken = capture.Result.AccessToken,
                        Authority = result.Authority,
                        TenantId = result.TenantId,
                        TokenType = result.AccessTokenType,
                    };
                }

                _captures.Add(capture);
            }
        }

        bool ICaptureService<CapturedAdalData>.GetCapturedData(ICapturedDataFilter filter, out CapturedAdalData capturedData)
            => GetCapturedData(filter, out capturedData);

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedAdalData adalData))
            {
                capturedData = adalData;
                return true;
            }

            capturedData = null;
            return false;
        }
    }
}
