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
using static System.StringComparer;

namespace AzureDevOps.Authentication.Test
{
    public class ReplayAdal : IAdal, IReplayService<CapturedAdalData>
    {
        internal ReplayAdal(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captures = new Dictionary<string, Queue<CapturedAdalOperation>>(OrdinalIgnoreCase);
            _context = context;
            _replayed = new Dictionary<string, Queue<CapturedAdalOperation>>(OrdinalIgnoreCase);
        }

        private readonly Dictionary<string, Queue<CapturedAdalOperation>> _captures;
        private readonly RuntimeContext _context;
        private readonly Dictionary<string, Queue<CapturedAdalOperation>> _replayed;

        public string ServiceName
            => "Adal";

        public Type ServiceType
            => typeof(IAdal);

        public Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string clientId, Uri redirectUri, string extraQueryParameters)
        {
            if (!TryGetNextOperation(authorityHostUrl, out CapturedAdalOperation operation))
                throw new ReplayNotFoundException($"Failed to find replay data for \"{authorityHostUrl}\".");
            if (operation.Error.Message is null && operation.Result.Authority is null)
                throw new ReplayDataException($"Expected either `{nameof(CapturedAdalOperation.Error)}` or `{nameof(CapturedAdalOperation.Result)}` to be null, but not both.");

            _context.Trace.WriteLine($"{nameof(ReplayAdal)}: \"{authorityHostUrl}\".");

            // Validate inputs are as expected
            if (!Ordinal.Equals(operation.Input.Resource, resource))
                throw new ReplayInputException($"Unexpected `{nameof(resource)}`: expected \"{operation.Input.Resource}\" vs actual \"{resource}\".");
            if (!Ordinal.Equals(operation.Input.ClientId, clientId))
                throw new ReplayInputException($"Unexpected `{nameof(clientId)}`: expected \"{operation.Input.ClientId}\" vs actual \"{clientId}\".");

            string redirectUrl = redirectUri?.ToString();

            if (!Ordinal.Equals(operation.Input.RedirectUrl, redirectUrl))
                throw new ReplayInputException($"Unexpected `{nameof(redirectUri)}`: expected \"{operation.Input.RedirectUrl}\" vs actual \"{redirectUrl}\".");
            if (!Ordinal.Equals(operation.Input.ExtraQueryParameters, extraQueryParameters))
                throw new ReplayInputException($"Unexpected `{nameof(extraQueryParameters)}`: expected \"{operation.Input.ExtraQueryParameters}\" vs actual \"{extraQueryParameters}\".");

            // Throw the exception if there is one.
            if (operation.Error.Message != null)
                throw new AuthenticationException(operation.Error.Message);

            var result = new Adal.Result(operation.Result.AccessToken,
                                         operation.Result.Authority,
                                         operation.Result.TenantId,
                                         operation.Result.TokenType);

            return Task.FromResult<IAdalResult>(result);
        }

        public Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string clientId)
            => AcquireTokenAsync(authorityHostUrl, resource, clientId, null, null);

        internal void SetReplayData(CapturedAdalData replayData)
        {
            if (replayData.Operations is null)
                return;

            foreach (var operation in replayData.Operations)
            {
                if (operation.AuthorityUrl is null)
                    continue;

                if (!_captures.TryGetValue(operation.AuthorityUrl, out var queue))
                {
                    queue = new Queue<CapturedAdalOperation>();

                    _captures.Add(operation.AuthorityUrl, queue);
                }

                queue.Enqueue(operation);
            }

            foreach (var queue in _captures.Values)
            {
                queue.TrimExcess();
            }
        }

        internal bool TryGetNextOperation(string authorityUrl, out CapturedAdalOperation operation)
        {
            if (authorityUrl != null)
            {
                if (_captures.TryGetValue(authorityUrl, out var queue)
                    && queue.Count > 0)
                {
                    if (!_replayed.TryGetValue(authorityUrl, out var replayed))
                    {
                        replayed = new Queue<CapturedAdalOperation>(queue.Count);

                        _replayed.Add(authorityUrl, replayed);
                    }

                    operation = queue.Dequeue();
                    replayed.Enqueue(operation);

                    return true;
                }
            }

            operation = default(CapturedAdalOperation);
            return false;
        }

        void IReplayService<CapturedAdalData>.SetReplayData(CapturedAdalData replayData)
            => SetReplayData(replayData);

        void IReplayService.SetReplayData(object replayData)
        {
            if (!(replayData is CapturedAdalData adalData)
                && !CapturedAdalData.TryDeserialize(replayData, out adalData))
            {
                var inner = new System.IO.InvalidDataException($"Failed to deserialize data into `{nameof(CapturedSettingsData)}`.");
                throw new ArgumentException(inner.Message, nameof(replayData), inner);
            }

            SetReplayData(adalData);
        }
    }
}
