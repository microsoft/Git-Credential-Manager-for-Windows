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
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class ReplaySettings : ISettings, IReplayService<CapturedSettingsData>
    {
        internal ReplaySettings(RuntimeContext context, Func<string, string> normalizePath)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (normalizePath is null)
                throw new ArgumentNullException(nameof(normalizePath));

            _captured = new CapturedSettingsData { };
            _context = context;
            _expandedVariables = new Dictionary<string, string>(OrdinalIgnoreCase);
            _normalizePath = normalizePath;
            _specialFolders = new Dictionary<Environment.SpecialFolder, string>();
            _syncpoint = new object();
            _variables = new Dictionary<EnvironmentVariableTarget, Dictionary<string, string>>();
        }

        private CapturedSettingsData _captured;
        private readonly RuntimeContext _context;
        private readonly Dictionary<string, string> _expandedVariables;
        private readonly Func<string, string> _normalizePath;
        private readonly Dictionary<Environment.SpecialFolder, string> _specialFolders;
        private readonly object _syncpoint;
        private readonly Dictionary<EnvironmentVariableTarget, Dictionary<string, string>> _variables;

        public string CommandLine
        {
            get
            {
                if (_captured.CommandLine is null)
                    throw new ReplayDataException($"`{nameof(CommandLine)}` not captured.");

                return _captured.CommandLine;
            }
        }

        public string CurrentDirectory
        {
            get
            {
                if (_captured.CurrentDirectory is null)
                    throw new ReplayDataException($"`{nameof(CurrentDirectory)}` not captured.");

                return _captured.CurrentDirectory;
            }
        }

        public bool Is64BitOperatingSystem
        {
            get { return _captured.Is64BitOperatingSystem; }
        }

        public string MachineName
        {
            get
            {
                if (_captured.MachineName is null)
                    throw new ReplayDataException($"`{nameof(MachineName)}` not captured.");

                return _captured.MachineName;
            }
        }

        public string NewLine
        {
            get
            {
                if (_captured.NewLine is null)
                    throw new ReplayDataException($"`{nameof(NewLine)}` not captured.");

                return _captured.NewLine;
            }
        }

        public OperatingSystem OsVersion => throw new NotImplementedException();

        public string ServiceName
            => "Settings";

        public Type ServiceType
            => typeof(ISettings);

        public Version Version
        {
            get
            {
                if (_captured.Version is null)
                    throw new ReplayDataException($"`{nameof(NewLine)}` not captured.");
                if (!Version.TryParse(_captured.Version, out Version output))
                    throw new ReplayOutputTypeException($"Failed to parse `{typeof(Version).FullName}` from \"{_captured.Version}\".");

                return output;
            }
        }

        public void Exit(int exitCode)
        {
            if (exitCode != _captured.ExitCode)
                throw new ReplayInputException($"Unexpected {nameof(exitCode)} value: expected {_captured.ExitCode} vs actual {exitCode}.");
        }

        public string ExpandEnvironmentVariables(string name)
        {
            if (!_expandedVariables.TryGetValue(name, out string value))
                throw new ReplayNotFoundException($"Failed to expand \"{name}\".");

            return value;
        }

        public string[] GetCommandLineArgs()
        {
            if (_captured.CommandLineArgs is null)
                throw new ReplayNotFoundException($"Failed to replay `{nameof(GetCommandLineArgs)}` because `{_captured.CommandLineArgs}` not captured.");

            return _captured.CommandLineArgs;
        }

        public string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (!_variables.TryGetValue(target, out Dictionary<string, string> byTarget))
                throw new ReplayNotFoundException($"Environment target '{target}' not captured.");
            if (!byTarget.TryGetValue(variable, out string output))
                throw new ReplayNotFoundException($"Environment variable \"{variable}\" not captured.");

            if (string.IsNullOrEmpty(output))
                return null;

            return output;
        }

        public string GetEnvironmentVariable(string variable)
            => GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process);

        public IDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            if (!_variables.TryGetValue(target, out Dictionary<string, string> output))
                throw new ReplayNotFoundException($"Environment target '{target}' not captured.");

            return output;
        }

        public IDictionary<string, string> GetEnvironmentVariables()
            => GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            if (!_specialFolders.TryGetValue(folder, out string output))
                throw new ReplayNotFoundException($"Special folder '{folder}' not captured.");

            return output;
        }

        internal void SetReplayData(CapturedSettingsData capturedSettings)
        {
            _captured = capturedSettings;

            if (_captured.ExpandVariables != null)
            {
                foreach (var item in _captured.ExpandVariables)
                {
                    _expandedVariables.Add(item.Original, item.Expanded);
                }
            }

            if (_captured.EnvironmentVariables != null)
            {
                foreach (var item in _captured.EnvironmentVariables)
                {
                    var target = (EnvironmentVariableTarget)item.Target;

                    if (!_variables.TryGetValue(target, out Dictionary<string, string> byTarget))
                    {
                        byTarget = new Dictionary<string, string>(OrdinalIgnoreCase);

                        _variables.Add(target, byTarget);
                    }

                    if (item.Values != null)
                    {
                        foreach (var value in item.Values)
                        {
                            byTarget[value.Name] = value.Variable;
                        }
                    }
                }
            }

            if (_captured.SpecialFolders != null)
            {
                foreach (var item in _captured.SpecialFolders)
                {
                    var specialFolder = (Environment.SpecialFolder)item.SpecialFolder;

                    _specialFolders[specialFolder] = item.Path;
                }
            }
        }

        void IReplayService<CapturedSettingsData>.SetReplayData(CapturedSettingsData replayData)
            => SetReplayData(replayData);

        void IReplayService.SetReplayData(object replayData)
        {
            if (!(replayData is CapturedSettingsData networkData)
                && !CapturedSettingsData.TryDeserialize(replayData, out networkData))
            {
                var inner = new System.IO.InvalidDataException($"Failed to deserialize data into `{nameof(CapturedSettingsData)}`.");
                throw new ArgumentException(inner.Message, nameof(replayData), inner);
            }

            SetReplayData(networkData);
        }
    }
}
