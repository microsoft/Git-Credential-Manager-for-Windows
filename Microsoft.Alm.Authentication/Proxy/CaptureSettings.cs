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
using System.Text.RegularExpressions;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class CaptureSettings : ICaptureService<CapturedSettingsData>, ISettings
    {
        internal static IReadOnlyList<Regex> AllowedCapturedPathValues = CaptureStorage.AllowedCapturedPathValues;
        internal static IReadOnlyList<Regex> AllowedCapturedVariableNames
            = new List<Regex>
            {
                new Regex("^GCM_", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex("^GIT_", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex("^XDG_CONFIG_HOME", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
            };
        internal static readonly IReadOnlyDictionary<string, string> AllowedCapturedVariables
            = new Dictionary<string, string>(OrdinalIgnoreCase)
            {
                { "ALLUSERSPROFILE", "C:\\ProgramData" },
                { "APPDATA", "C:\\Users\\Tester\\AppData\\Roaming" },
                { "CommonProgramFiles", "C:\\Program Files (x86)\\Common File" },
                { "CommonProgramFiles(x86)", "C:\\Program Files (x86)\\Common Files" },
                { "CommonProgramW6432", "C:\\Program Files\\Common Files" },
                { "HOME", "C:\\Users\\Tester" },
                { "HOMEDRIVE", "C:" },
                { "HOMEPATH", "\\Users\\Tester" },
                { "LOCALAPPDATA", "C:\\Users\\Tester\\AppData\\Local" },
                { "PATHEXT", ".COM;.EXE;.BAT;.CMD" },
                { "ProgramData", "C:\\ProgramData" },
                { "ProgramFiles(x86)", "C:\\Program Files (x86)" },
                { "ProgramFiles", "C:\\Program Files" },
                { "ProgramW6432", "C:\\Program Files" },
                { "SystemDrive", "C:" },
                { "SystemRoot", "C:\\WINDOWS" },
                { "TEMP", "C:\\Users\\Tester\\AppData\\Local\\Temp" },
                { "TMP", "C:\\Users\\Tester\\AppData\\Local\\Temp" },
                { "USERNAME", "Tester" },
                { "USERPROFILE", "C:\\Users\\Tester" },
                { "WINDIR", "C:\\Windows" },
            };

        public CaptureSettings(RuntimeContext context, Func<string, string> normalizePath)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (normalizePath is null)
                throw new ArgumentNullException(nameof(normalizePath));

            _captured = new CapturedSettingsData { };
            _context = context;
            _expandedVariables = new Dictionary<string, string>(OrdinalIgnoreCase);
            _normalizePath = normalizePath;
            _settings = context.Settings;
            _specialFolders = new Dictionary<Environment.SpecialFolder, string>();
            _syncpoint = new object();
            _variables = new Dictionary<EnvironmentVariableTarget, Dictionary<string, string>>();
        }

        private CapturedSettingsData _captured;
        private readonly RuntimeContext _context;
        private readonly Dictionary<string, string> _expandedVariables;
        private readonly Func<string, string> _normalizePath;
        private readonly ISettings _settings;
        private readonly Dictionary<Environment.SpecialFolder, string> _specialFolders;
        private readonly object _syncpoint;
        private readonly Dictionary<EnvironmentVariableTarget, Dictionary<string, string>> _variables;

        public string ServiceName
            => "Settings";

        public Type ServiceType
            => typeof(ISettings);

        public string CommandLine
        {
            get
            {
                var result = Environment.CommandLine;

                lock (_syncpoint)
                {
                    _captured.CommandLine = result;
                }

                return result;
            }
        }

        public string CurrentDirectory
        {
            get
            {
                var result = Environment.CurrentDirectory;

                lock (_syncpoint)
                {
                    _captured.CurrentDirectory = result;
                }

                return result;
            }
        }

        public bool Is64BitOperatingSystem
        {
            get
            {
                var result = Environment.Is64BitOperatingSystem;

                lock (_syncpoint)
                {
                    _captured.Is64BitOperatingSystem = result;
                }

                return result;
            }
        }

        public string MachineName
        {
            get
            {
                var result = Environment.MachineName;

                lock (_syncpoint)
                {
                    _captured.MachineName = result;
                }

                return result;
            }
        }

        public string NewLine
        {
            get
            {
                var result = Environment.NewLine;

                lock (_syncpoint)
                {
                    _captured.NewLine = result;
                }

                return result;
            }
        }

        public OperatingSystem OsVersion
        {
            get
            {
                var result = Environment.OSVersion;

                //_captured.OsVersion = (int)result;

                return result;
            }
        }

        public Version Version
        {
            get
            {
                var result = Environment.Version;

                lock (_syncpoint)
                {
                    _captured.Version = result.ToString(4);
                }

                return result;
            }
        }

        public string ExpandEnvironmentVariables(string name)
        {
            var result = Environment.ExpandEnvironmentVariables(name);

            lock (_syncpoint)
            {
                _expandedVariables[name] = result;
            }

            return result;
        }

        public void Exit(int exitCode)
        {
            lock (_syncpoint)
            {
                _captured.ExitCode = exitCode;
            }
        }

        public string[] GetCommandLineArgs()
        {
            var result = Environment.GetCommandLineArgs();

            lock (_syncpoint)
            {
                _captured.CommandLineArgs = result;
            }

            return result;
        }

        public IDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            var result = Environment.GetEnvironmentVariables(target);

            lock (_syncpoint)
            {
                if (!_variables.TryGetValue(target, out Dictionary<string, string> variables))
                {
                    variables = new Dictionary<string, string>(OrdinalIgnoreCase);

                    _variables.Add(target, variables);
                }

                foreach (var item in result.Keys)
                {
                    var key = item as string;

                    variables[key] = result[key] as string;
                }

                return variables;
            }
        }

        public IDictionary<string, string> GetEnvironmentVariables()
            => GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        public string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            var result = Environment.GetEnvironmentVariable(variable, target);

            lock (_syncpoint)
            {
                if (!_variables.TryGetValue(target, out Dictionary<string, string> variables))
                {
                    variables = new Dictionary<string, string>(OrdinalIgnoreCase);

                    _variables.Add(target, variables);
                }

                _variables[target][variable] = result;
            }

            return result;
        }

        public string GetEnvironmentVariable(string variable)
            => GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process);

        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            lock (_syncpoint)
            {
                if (!_specialFolders.TryGetValue(folder, out string result))
                {
                    result = Environment.GetFolderPath(folder);

                    _specialFolders.Add(folder, result);
                }

                return result;
            }
        }

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedSettingsData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            lock (_syncpoint)
            {
                _captured.EnvironmentVariables = new List<CapturedSettingsEnvironmentVariables>(3);

                // Ensure the special GCM_DEBUG environment variable is set.
                if (!_variables[EnvironmentVariableTarget.Process].ContainsKey("GCM_DEBUG"))
                {
                    _variables[EnvironmentVariableTarget.Process].Add("GCM_DEBUG", null);
                }

                foreach (var target in _variables.Keys)
                {
                    var variables = new CapturedSettingsEnvironmentVariables
                    {
                        Target = (int)target,
                        Values = new List<CapturedSettingsEnvironmentVariable>(),
                    };

                    foreach (var kvp in _variables[target])
                    {
                        if (kvp.Key is null)
                            continue;

                        if (OrdinalIgnoreCase.Equals("PATH", kvp.Key))
                        {
                            var items = kvp.Value?.Split(';');
                            var keeps = new List<string>(items.Length);

                            for (int i = 0; i < items.Length; i += 1)
                            {
                                foreach (var legal in AllowedCapturedPathValues)
                                {
                                    if (legal.IsMatch(items[i]))
                                    {
                                        keeps.Add(items[i]);
                                        break;
                                    }
                                }
                            }

                            if (keeps.Count > 0)
                            {
                                var name = kvp.Key;
                                var variable = string.Join(";", keeps);

                                var entry = new CapturedSettingsEnvironmentVariable
                                {
                                    Name = name,
                                    Variable = variable,
                                };

                                variables.Values.Add(entry);
                            }
                        }
                        else if (AllowedCapturedVariables.TryGetValue(kvp.Key, out string variable))
                        {
                            var name = kvp.Key;

                            var entry = new CapturedSettingsEnvironmentVariable
                            {
                                Name = name,
                                Variable = variable,
                            };

                            variables.Values.Add(entry);
                        }
                        else
                        {
                            var name = kvp.Key;
                            variable = kvp.Value;

                            foreach (var allowed in AllowedCapturedVariableNames)
                            {
                                if (allowed.IsMatch(name))
                                {
                                    variable = filter.ApplyFilter(variable);

                                    var entry = new CapturedSettingsEnvironmentVariable
                                    {
                                        Name = name,
                                        Variable = variable,
                                    };

                                    variables.Values.Add(entry);

                                    break;
                                }
                            }
                        }
                    }

                    _captured.EnvironmentVariables.Add(variables);
                }

                _captured.ExpandVariables = new List<CapturedSettingsExpandVariable>();

                foreach (var kvp in _expandedVariables)
                {
                    if (kvp.Key is null)
                        continue;

                    var expanded = kvp.Value;
                    var original = kvp.Key;

                    expanded = filter.ApplyFilter(expanded);
                    original = filter.ApplyFilter(original);

                    var query = new CapturedSettingsExpandVariable
                    {
                        Expanded = expanded,
                        Original = original,
                    };

                    _captured.ExpandVariables.Add(query);
                }

                _captured.SpecialFolders = new List<CapturedSettingsSpecialFolder>();

                foreach (var kvp in _specialFolders)
                {
                    if (kvp.Value is null)
                        continue;

                    var path = kvp.Value;

                    path = filter.ApplyFilter(path);

                    var folder = new CapturedSettingsSpecialFolder
                    {
                        SpecialFolder = (int)kvp.Key,
                        Path = path,
                    };

                    _captured.SpecialFolders.Add(folder);
                }

                _captured.CurrentDirectory = filter.ApplyFilter(_captured.CurrentDirectory);

                capturedData = _captured;
                return true;
            }
        }

        string IProxyService.ServiceName
            => ServiceName;

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedSettingsData capturedSettingsData))
            {
                capturedData = capturedSettingsData;
                return true;
            }

            capturedData = null;
            return false;
        }

        bool ICaptureService<CapturedSettingsData>.GetCapturedData(ICapturedDataFilter filter, out CapturedSettingsData capturedData)
            => GetCapturedData(filter, out capturedData);
    }
}
