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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Microsoft.Alm.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedStorageData
    {
        [JsonProperty(PropertyName = "Operations", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedStorageOperation> Operations { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedStorageData)}: {nameof(Operations)}[{Operations?.Count}]"; }
        }

        public static bool TryDeserialize(object serializedData, out CapturedStorageData storageData)
        {
            if (serializedData is JObject jStorageData)
            {
                storageData = jStorageData.ToObject<CapturedStorageData>();

                return true;
            }

            storageData = default(CapturedStorageData);
            return false;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedStorageMethod
    {
        [JsonProperty(PropertyName = "Method", Required = Required.DisallowNull)]
        public string MethodName { get; set; }

        [JsonProperty(PropertyName = "Queries", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedStorageQuery> Queries { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedStorageMethod)}: {MethodName}[{Queries?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedStorageOperation
    {
        [JsonProperty(PropertyName = "Path", Required = Required.DisallowNull)]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "Methods", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedStorageMethod> Methods { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedStorageMethod)}: {Path}[{Methods?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedStorageQuery
    {
        [JsonProperty(PropertyName = "Input", NullValueHandling = NullValueHandling.Ignore)]
        public object Input { get; set; }

        [JsonProperty(PropertyName = "Ordinal", Required = Required.Always)]
        public int Oridinal { get; set; }

        [JsonProperty(PropertyName = "Output", NullValueHandling = NullValueHandling.Ignore)]
        public object Output { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"({Oridinal}) {Input?.ToString() ?? "<null>"} -> {Output?.ToString() ?? "<null>"}"); }
        }


        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct CopyInput
        {
            [JsonProperty(PropertyName = "Overwrite")]
            public bool Overwrite { get; set; }

            [JsonProperty(PropertyName = "SourcePath", NullValueHandling = NullValueHandling.Ignore)]
            public string SourcePath { get; set; }

            [JsonProperty(PropertyName = "TargetPath", NullValueHandling = NullValueHandling.Ignore)]
            public string TargetPath { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(CopyInput)}: \"{SourcePath}\" => \"{TargetPath}\" ({nameof(Overwrite)}={Overwrite.ToString()}"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct EnumerateInput
        {
            [JsonProperty(PropertyName = "Options")]
            public int Options { get; set; }

            [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
            public string Path { get; set; }

            [JsonProperty(PropertyName = "Pattern", NullValueHandling = NullValueHandling.Ignore)]
            public string Pattern { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(EnumerateInput)}: \"{Path}\", \"{Pattern}\" '{(SearchOption)Options}'"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct OpenInput
        {
            [JsonProperty(PropertyName = "Access")]
            public int Access { get; set; }

            [JsonProperty(PropertyName = "Mode")]
            public int Mode { get; set; }

            [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
            public string Path { get; set; }

            [JsonProperty(PropertyName = "Share")]
            public int Share { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(OpenInput)}: \"{Path}\", {(FileMode)Mode}, {(FileAccess)Access}, {(FileShare)Share}"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct OpenOutput
        {
            [JsonProperty(PropertyName = "Access")]
            public int Access { get; set; }

            [JsonProperty(PropertyName = "Data")]
            public List<string> Data { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(OpenOutput)}: {Access} [{Data?.Count}]"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct RegistryReadInput
        {
            [JsonProperty(PropertyName = "Hive")]
            public int Hive { get; set; }

            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Path")]
            public string Path { get; set; }

            [JsonProperty(PropertyName = "View")]
            public int View { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(RegistryReadInput)}: \"{ToString()}\""; }
            }

            public override string ToString()
            {
                return CreateStoragePath(View, Hive, Path, Name);
            }

            public static string CreateStoragePath(int view, int hive, string path, string name)
            {
                return CreateStoragePath((Microsoft.Win32.RegistryHive)hive, (Microsoft.Win32.RegistryView)view, path, name);
            }

            public static string CreateStoragePath(Microsoft.Win32.RegistryHive hive, Microsoft.Win32.RegistryView view, string path, string name)
            {
                return $"reg:\\\\{view}:{hive}\\{path}\\{name}";
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq")]
        public struct SecureDataInput
        {
            [JsonProperty(PropertyName = "Data", NullValueHandling = NullValueHandling.Ignore)]
            public byte[] Data { get; set; }

            [JsonProperty(PropertyName = "Key", NullValueHandling = NullValueHandling.Ignore)]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "Name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(SecureDataInput)}: \"{Key}\" => \"{Name}\" : [{Data?.Length}]"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        public struct SecureDataOutput
        {
            [JsonProperty(PropertyName = "Data", NullValueHandling = NullValueHandling.Ignore)]
            public byte[] Data { get; set; }

            [JsonProperty(PropertyName = "Key", NullValueHandling = NullValueHandling.Ignore)]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "Name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Result", NullValueHandling = NullValueHandling.Ignore)]
            public bool Result { get; set; }

            internal string DebuggerDisplay
            {
                get
                {
                    return (Key is null)
                      ? $"{nameof(SecureDataOutput)}: \"{Name}\" = {Result}"
                      : $"{nameof(SecureDataOutput)}: \"{Key}\" => \"{Name}\" ({Data?.Length}";
                }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq")]
        public struct WriteAllBytesInput
        {
            [JsonProperty(PropertyName = "Data", NullValueHandling = NullValueHandling.Ignore)]
            public byte[] Data { get; set; }

            [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
            public string Path { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(WriteAllBytesInput)}: \"{Path}\" [{Data?.Length}]"; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq")]
        public struct WriteAllTextInput
        {
            [JsonProperty(PropertyName = "Contents", NullValueHandling = NullValueHandling.Ignore)]
            public string Contents { get; set; }

            [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
            public string Path { get; set; }

            internal string DebuggerDisplay
            {
                get { return $"{nameof(WriteAllTextInput)}: \"{Path}\" [{Contents?.Length}]"; }
            }
        }
    }
}
