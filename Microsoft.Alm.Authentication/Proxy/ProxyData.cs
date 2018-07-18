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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static System.FormattableString;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class ProxyData
    {
        [JsonProperty(PropertyName = "DisplayName", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "ExtendedData", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, string>> ExtendedData = new Dictionary<string, Dictionary<string, string>>(Ordinal);

        [JsonProperty(PropertyName = "ResultPath", NullValueHandling = NullValueHandling.Ignore)]
        public string ResultPath { get; set; }

        [JsonProperty(PropertyName = "Services", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Services { get; } = new Dictionary<string, object>(Ordinal);

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(ProxyData)}: \"{DisplayName}\", {nameof(Services)}[{Services?.Count}], {nameof(ExtendedData)}[{ExtendedData?.Count}]"); }
        }
    }
}
