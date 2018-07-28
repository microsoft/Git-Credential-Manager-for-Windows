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

namespace Atlassian.Bitbucket.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedGuiData
    {
        [JsonProperty(PropertyName = "Operations", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedGuiOperation> Operations { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedGuiData)}: {nameof(Operations)}[{Operations?.Count}]"); }
        }

        public static bool TryDeserialize(object serializedData, out CapturedGuiData guiData)
        {
            if (serializedData is JObject jGuiData)
            {
                guiData = jGuiData.ToObject<CapturedGuiData>();

                return true;
            }

            guiData = default(CapturedGuiData);
            return false;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedGuiOperation
    {
        [JsonProperty(PropertyName = "Output", NullValueHandling = NullValueHandling.Ignore)]
        public CapturedGuiOutput Output { get; set; }

        [JsonProperty(PropertyName = "DialogType", NullValueHandling = NullValueHandling.Ignore)]
        public string DialogType { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedGuiOperation)}: {nameof(DialogType)} = {DialogType}"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedGuiOutput
    {
        [JsonProperty(PropertyName = "Login", NullValueHandling = NullValueHandling.Ignore)]
        public string Login { get; set; }

        [JsonProperty(PropertyName = "IsValid", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsValid { get; set; }

        [JsonProperty(PropertyName = "Password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "Result", NullValueHandling = NullValueHandling.Ignore)]
        public int Result { get; set; }

        [JsonProperty(PropertyName = "Success", NullValueHandling = NullValueHandling.Ignore)]
        public bool Success { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedGuiOutput)}: {ToString()}"); }
        }

        public override string ToString()
        {
            return Invariant($"{nameof(Result)} = {Result}, {nameof(Success)} = {Success}");
        }
    }
}
