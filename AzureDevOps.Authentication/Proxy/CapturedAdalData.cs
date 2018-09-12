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

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureDevOps.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedAdalData
    {
        [JsonProperty(PropertyName = "Operations", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedAdalOperation> Operations { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedAdalData)}: {nameof(Operations)}[{Operations?.Count}]"; }
        }

        public static bool TryDeserialize(object serializedData, out CapturedAdalData adalData)
        {
            if (serializedData is JObject jAdalData)
            {
                adalData = jAdalData.ToObject<CapturedAdalData>();

                return true;
            }

            adalData = default(CapturedAdalData);
            return false;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedAdalOperation
    {
        [JsonProperty(PropertyName = "AuthorityUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string AuthorityUrl { get; set; }

        [JsonProperty(PropertyName = "Error", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedAdalException Error { get; set; }

        [JsonProperty(PropertyName = "Input", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedAdalInput Input { get; set; }

        [JsonProperty(PropertyName = "Result", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedAdalResult Result { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedAdalOperation)}: \"{AuthorityUrl}\" ({(Error.Message is null ? nameof(Result) : nameof(Error))})"; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedAdalInput
    {
        [JsonProperty(PropertyName = "ClientId", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientId { get; set; }

        [JsonProperty(PropertyName = "ExtraQueryParameters", NullValueHandling = NullValueHandling.Ignore)]
        public string ExtraQueryParameters { get; set; }

        [JsonProperty(PropertyName = "Resource", NullValueHandling = NullValueHandling.Ignore)]
        public string Resource { get; set; }

        [JsonProperty(PropertyName = "RedirectUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string RedirectUrl { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedAdalInput)}"; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedAdalResult
    {
        [JsonProperty(PropertyName = "AccessToken", NullValueHandling = NullValueHandling.Ignore)]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "Authority", NullValueHandling = NullValueHandling.Ignore)]
        public string Authority { get; set; }

        [JsonProperty(PropertyName = "TokenType", NullValueHandling = NullValueHandling.Ignore)]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "TenantId", NullValueHandling = NullValueHandling.Ignore)]
        public string TenantId { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedAdalResult)}: {nameof(Authority)} = \"{Authority}\", {nameof(TenantId)} = {TenantId}, {nameof(TokenType)} = {TokenType}"; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedAdalException
    {
        [JsonProperty(PropertyName = "Message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedAdalException)}: \"{Message}\""; }
        }
    }
}
