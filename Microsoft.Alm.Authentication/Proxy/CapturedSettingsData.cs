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

namespace Microsoft.Alm.Authentication.Test
{
    public struct CapturedSettingsData
    {
        [JsonProperty(PropertyName = "EnvironmentVariables", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedSettingsEnvironmentVariables> EnvironmentVariables { get; set; }

        [JsonProperty(PropertyName = "ExitCode")]
        public int ExitCode { get; set; }

        [JsonProperty(PropertyName = "ExpandVariables", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedSettingsExpandVariable> ExpandVariables { get; set; }

        [JsonProperty(PropertyName = "CommandLine", NullValueHandling = NullValueHandling.Ignore)]
        public string CommandLine { get; set; }

        [JsonProperty(PropertyName = "CommandLineArgs", NullValueHandling = NullValueHandling.Ignore)]
        public string[] CommandLineArgs { get; set; }

        [JsonProperty(PropertyName = "CurrentDirectory", NullValueHandling = NullValueHandling.Ignore)]
        public string CurrentDirectory { get; set; }

        [JsonProperty(PropertyName = "Is64BitOperatingSystem", NullValueHandling = NullValueHandling.Ignore)]
        public bool Is64BitOperatingSystem { get; set; }

        [JsonProperty(PropertyName = "MachineName", NullValueHandling = NullValueHandling.Ignore)]
        public string MachineName { get; set; }

        [JsonProperty(PropertyName = "NewLine", NullValueHandling = NullValueHandling.Ignore)]
        public string NewLine { get; set; }

        [JsonProperty(PropertyName = "OsVersion", NullValueHandling = NullValueHandling.Ignore)]
        public int OsVersion { get; set; }

        [JsonProperty(PropertyName = "SpecialFolders", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedSettingsSpecialFolder> SpecialFolders { get; set; }

        [JsonProperty(PropertyName = "Version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        public static bool TryDeserialize(object serializedData, out CapturedSettingsData settingsData)
        {
            if (serializedData is JObject jSettingsData)
            {
                settingsData = jSettingsData.ToObject<CapturedSettingsData>();

#if Deserialize_Manually
                if (jsettings["EnvironmentVariables"] is JArray jEnvironmentVariables)
                {
                    settingsData.EnvironmentVariables = new List<CapturedSettingsEnvironmentVariables>();

                    foreach (var token in jEnvironmentVariables)
                    {
                        var item = new CapturedSettingsEnvironmentVariables
                        {
                            Target = token["Target"].Value<int>(),
                            Values = new List<CapturedSettingsEnvironmentVariable>(),
                        };

                        if (token["Values"] is JArray jValues)
                        {
                            foreach (var jitem in jValues)
                            {
                                var envvar = new CapturedSettingsEnvironmentVariable
                                {
                                    Name = jitem["Name"].Value<string>(),
                                    Variable = jitem["Variable"].Value<string>(),
                                };

                                item.Values.Add(envvar);
                            }
                        }
                    }
                }

                if (jsettings["CommandLineArgs"] is JArray jCommandLineArgs)
                {
                    settingsData.CommandLineArgs = jCommandLineArgs.ToObject<string[]>();
                }

                if (jsettings["SpecialFolders"] is JArray jSpecialFolders)
                {
                    settingsData.SpecialFolders = new List<CapturedSettingsSpecialFolder>();

                    foreach(var jSpecialFolder in jSpecialFolders)
                    {
                        var specialFolder = new CapturedSettingsSpecialFolder
                        {
                            Path = jSpecialFolder["Path"].Value<string>(),
                            SpecialFolder = jSpecialFolder["SpecialFolder"].Value<int>(),
                        };

                        settingsData.SpecialFolders.Add(specialFolder);
                    }
                }
#endif

                return true;
            }

            settingsData = default(CapturedSettingsData);
            return false;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedSettingsEnvironmentVariable
    {
        [JsonProperty(PropertyName = "Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "Variable", NullValueHandling = NullValueHandling.Ignore)]
        public string Variable { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedSettingsEnvironmentVariable)}: \"{Name}\" => \"{Variable}\""; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedSettingsEnvironmentVariables
    {
        [JsonProperty(PropertyName = "Target")]
        public int Target { get; set; }

        [JsonProperty(PropertyName = "Values", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedSettingsEnvironmentVariable> Values { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedSettingsEnvironmentVariables)}: Target = {Target}, Values[{Values?.Count}]"; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedSettingsExpandVariable
    {
        [JsonProperty(PropertyName = "Expanded", NullValueHandling = NullValueHandling.Ignore)]
        public string Expanded { get; set; }

        [JsonProperty(PropertyName = "Original", Required = Required.Always)]
        public string Original { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedSettingsExpandVariable)}: \"{Original}\" => \"{Expanded}\""; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedSettingsSpecialFolder
    {
        [JsonProperty(PropertyName = "Path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "SpecialFolder", Required = Required.Always)]
        public int SpecialFolder { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(CapturedSettingsSpecialFolder)}: {SpecialFolder} => \"{Path}\""; }
        }
    }
}
