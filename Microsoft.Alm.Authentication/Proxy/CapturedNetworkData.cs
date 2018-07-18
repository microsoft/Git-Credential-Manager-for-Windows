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
using static System.FormattableString;
using static System.StringComparison;

namespace Microsoft.Alm.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkData
    {
        [JsonProperty(PropertyName = "Operations", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedNetworkOperation> Operations { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedNetworkData)}: {nameof(Operations)}[{Operations?.Count}]"; }
        }

        public static bool TryDeserialize(object serializedObject, out CapturedNetworkData operation)
        {
            if (serializedObject is JObject jNetworkData)
            {
                operation = jNetworkData.ToObject<CapturedNetworkData>();

#if Deserialize_Manually
                if (jNetworkData["Operations"] is JArray jOperations)
                {
                    operation.Operations = new List<CapturedNetworkOperation>();

                    foreach (var jNetworkOperation in jOperations)
                    {
                        var networkOperation = new CapturedNetworkOperation
                        {
                            Methods = new List<CapturedNetworkMethod>(),
                            ProxyUrl = jNetworkOperation["ProxyUrl"]?.Value<string>(),
                            QueryUrl = jNetworkOperation["QueryUrl"]?.Value<string>(),
                        };

                        if (jNetworkOperation["Methods"] is JArray jMethods)
                        {
                            foreach(var jMethod in jMethods)
                            {
                                var networkMethod = new CapturedNetworkMethod
                                {
                                    Method = jMethod["Method"]?.Value<string>(),
                                    Queries = new List<CapturedNetworkQuery>(),
                                };

                                if (jMethod["Queries"] is JArray jQueries)
                                {
                                    foreach(var jQuery in jQueries)
                                    {
                                        var networkQuery = new CapturedNetworkQuery
                                        {
                                            Ordinal = jQuery["Ordinal"].Value<int>(),
                                            Request = new CapturedNetworkRequest
                                            {
                                                Content = jQuery["Content"]?.Value<string>(),
                                                Headers = jQuery["Headers"]?.ToObject<List<string>>(),
                                                OptionFlags = jQuery["OptionFlags"].Value<int>(),
                                            }
                                        };
                                    }
                                }
                            }
                        }

                        operation.Operations.Add(networkOperation);
                    }
                }
#endif

                return true;
            }

            operation = default(CapturedNetworkData);
            return false;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkContent
    {
        [JsonProperty(PropertyName = "AsBytes", NullValueHandling = NullValueHandling.Ignore)]
        public byte[ ] AsBytes { get; set; }

        [JsonProperty(PropertyName = "AsString", NullValueHandling = NullValueHandling.Ignore)]
        public string AsString { get; set; }

        [JsonProperty(PropertyName = "ContentType", NullValueHandling = NullValueHandling.Ignore)]
        public string ContentType { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasContent
        {
            get { return AsBytes != null || AsString != null; }
        }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedNetworkContent)}: {ToString()}."); }
        }

        public override string ToString()
        {
            return (AsBytes is null)
                ? (AsString is null)
                    ? "<Empty>"
                    : Invariant($"{nameof(AsString)} Length = {AsString.Length}")
                : (AsString is null)
                    ? Invariant($"{nameof(AsBytes)} Length = {AsBytes.Length}")
                    : "<Error>";
        }
    }


    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkMethod
    {
        [JsonProperty(PropertyName = "Method", Required = Required.DisallowNull)]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "Queries", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedNetworkQuery> Queries { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedStorageMethod)}: {Method}[{Queries?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkOperation
    {
        [JsonProperty(PropertyName = "ProxyUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ProxyUrl { get; set; }

        [JsonProperty(PropertyName = "Methods", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedNetworkMethod> Methods { get; set; }

        [JsonProperty(PropertyName = "QueryUrl", Required = Required.DisallowNull)]
        public string QueryUrl { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedNetworkOperation)}: [{Methods?.Count}] \"{QueryUrl}\""); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkRequest
    {
        [JsonProperty(PropertyName = "Content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "Headers", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Headers { get; set; }

        [JsonProperty(PropertyName = "Flags")]
        public int OptionFlags { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedNetworkResponse)}: Content[{Content?.Length}], Headers[{Headers?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkResponse
    {
        [JsonProperty(PropertyName = "Content", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedNetworkContent Content { get; set; }

        [JsonProperty(PropertyName = "Headers", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Headers { get; set; }

        [JsonProperty(PropertyName = "StatusCode")]
        public int StatusCode { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedNetworkResponse)}: {StatusCode}, Content[{Content}], Headers[{Headers?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedNetworkQuery
    {
        [JsonProperty(PropertyName = "Ordinal", Required = Required.Always)]
        public int Ordinal { get; set; }

        [JsonProperty(PropertyName = "Request", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedNetworkRequest Request { get; set; }

        [JsonProperty(PropertyName = "Response", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedNetworkResponse Response { get; set; }

        internal string DebuggerDisplay
        {
            get
            {
                var request = Request.Headers is null ? "No" : "Yes";
                var response = Response.Headers is null ? "No" : "Yes";

                return Invariant($"{nameof(CapturedNetworkQuery)}: Request: {request}, Response: {response}");
            }
        }
    }
}
