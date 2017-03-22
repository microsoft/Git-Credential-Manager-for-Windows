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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Alm.Git
{
    public sealed class Configuration
    {
        private const char HostSplitCharacter = '.';

        private static readonly Lazy<Regex> CommentRegex = new Lazy<Regex>(() => new Regex(@"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant));
        private static readonly Lazy<Regex> KeyValueRegex = new Lazy<Regex>(() => new Regex(@"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant));
        private static readonly Lazy<Regex> SectionRegex = new Lazy<Regex>(() => new Regex(@"^\s*\[\s*(\w+)\s*(\""[^\]]+){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant));

        public Configuration(string directory, bool loadLocal, bool loadSystem)
        {
            if (String.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException("directory");
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            ConfigurationLevel types = ConfigurationLevel.All;

            if (!loadLocal)
            {
                types ^= ConfigurationLevel.Local;
            }

            if (!loadSystem)
            {
                types ^= ConfigurationLevel.System;
            }

            LoadGitConfiguration(directory, types);
        }

        public Configuration()
            : this(Environment.CurrentDirectory, false, false)
        { }

        internal Configuration(TextReader configReader, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            ParseGitConfig(configReader, _values[level]);
        }

        public static IEnumerable<ConfigurationLevel> Levels
        {
            get
            {
                yield return ConfigurationLevel.Local;
                yield return ConfigurationLevel.Global;
                yield return ConfigurationLevel.Xdg;
                yield return ConfigurationLevel.System;
                yield return ConfigurationLevel.Portable;
            }
        }

        private readonly Dictionary<ConfigurationLevel, Dictionary<string, string>> _values = new Dictionary<ConfigurationLevel, Dictionary<string, string>>()
            {
                { ConfigurationLevel.Global, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Local, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Portable, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.System, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Xdg, new Dictionary<string, string>(Entry.KeyComparer) },
            };

        public string this[string key]
        {
            get
            {
                foreach (var level in Levels)
                {
                    if (_values[level].ContainsKey(key))
                        return _values[level][key];
                }

                return null;
            }
        }

        public int Count
        {
            get
            {
                return _values[ConfigurationLevel.Global].Count
                     + _values[ConfigurationLevel.Local].Count
                     + _values[ConfigurationLevel.Portable].Count
                     + _values[ConfigurationLevel.System].Count
                     + _values[ConfigurationLevel.Xdg].Count;
            }
        }

        public bool ContainsKey(string key)
        {
            return ContainsKey(ConfigurationLevel.All, key);
        }

        public bool ContainsKey(ConfigurationLevel levels, string key)
        {
            foreach (var level in Levels)
            {
                if ((level & levels) != 0
                    && _values[level].ContainsKey(key))
                    return true;
            }

            return false;
        }

        public bool TryGetEntry(string prefix, string key, string suffix, out Entry entry)
        {
            if (ReferenceEquals(prefix, null))
                throw new ArgumentNullException(nameof(prefix));
            if (ReferenceEquals(suffix, null))
                throw new ArgumentNullException(nameof(suffix));

            string match = String.IsNullOrEmpty(key)
                ? String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", prefix, suffix)
                : String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}", prefix, key, suffix);

            // if there's a match, return it
            if (ContainsKey(match))
            {
                entry = new Entry(match, this[match]);
                return true;
            }

            // nothing found
            entry = default(Entry);
            return false;
        }

        public bool TryGetEntry(string prefix, Uri targetUri, string key, out Entry entry)
        {
            if (ReferenceEquals(key, null))
                throw new ArgumentNullException(nameof(key));

            if (targetUri != null)
            {
                // return match seeking from most specific (<prefix>.<scheme>://<host>.<key>) to least specific (credential.<key>)
                if (TryGetEntry(prefix, String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}", targetUri.Scheme, targetUri.Host), key, out entry)
                    || TryGetEntry(prefix, targetUri.Host, key, out entry))
                    return true;

                if (!String.IsNullOrWhiteSpace(targetUri.Host))
                {
                    string[] fragments = targetUri.Host.Split(HostSplitCharacter);
                    string host = null;

                    // look for host matches stripping a single sub-domain at a time off
                    // don't match against a top-level domain (aka ".com")
                    for (int i = 1; i < fragments.Length - 1; i++)
                    {
                        host = String.Join(".", fragments, i, fragments.Length - i);
                        if (TryGetEntry(prefix, host, key, out entry))
                            return true;
                    }
                }
            }

            // try to find an unadorned match as a complete fallback
            if (TryGetEntry(prefix, String.Empty, key, out entry))
                return true;

            // nothing found
            entry = default(Entry);
            return false;
        }

        public void LoadGitConfiguration(string directory, ConfigurationLevel types)
        {
            string portableConfig = null;
            string systemConfig = null;
            string xdgConfig = null;
            string globalConfig = null;
            string localConfig = null;

            // read Git's four configs from lowest priority to highest, overwriting values as
            // higher priority configurations are parsed, storing them in a handy lookup table

            // find and parse Git's portable config
            if ((types & ConfigurationLevel.Portable) != 0
                && Where.GitPortableConfig(out portableConfig))
            {
                ParseGitConfig(ConfigurationLevel.Portable, portableConfig);
            }

            // find and parse Git's system config
            if ((types & ConfigurationLevel.System) != 0
                && Where.GitSystemConfig(null, out systemConfig))
            {
                ParseGitConfig(ConfigurationLevel.System, systemConfig);
            }

            // find and parse Git's Xdg config
            if ((types & ConfigurationLevel.Xdg) != 0
                && Where.GitXdgConfig(out xdgConfig))
            {
                ParseGitConfig(ConfigurationLevel.Xdg, xdgConfig);
            }

            // find and parse Git's global config
            if ((types & ConfigurationLevel.Global) != 0
                && Where.GitGlobalConfig(out globalConfig))
            {
                ParseGitConfig(ConfigurationLevel.Global, globalConfig);
            }

            // find and parse Git's local config
            if ((types & ConfigurationLevel.Local) != 0
                && Where.GitLocalConfig(directory, out localConfig))
            {
                ParseGitConfig(ConfigurationLevel.Local, localConfig);
            }

            Git.Trace.WriteLine($"git {types} config read, {Count} entries.");
        }

        private void ParseGitConfig(ConfigurationLevel level, string configPath)
        {
            Debug.Assert(Enum.IsDefined(typeof(ConfigurationLevel), level), $"The `{nameof(level)}` parameter is not defined.");
            Debug.Assert(!String.IsNullOrWhiteSpace(configPath), $"The `{nameof(configPath)}` parameter is null or invalid.");
            Debug.Assert(File.Exists(configPath), $"The `{nameof(configPath)}` parameter references a non-existent file.");

            if (!_values.ContainsKey(level))
                return;
            if (!File.Exists(configPath))
                return;

            using (var stream = File.OpenRead(configPath))
            using (var reader = new StreamReader(stream))
            {
                ParseGitConfig(reader, _values[level]);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        internal static void ParseGitConfig(TextReader reader, IDictionary<string, string> destination)
        {
            Debug.Assert(reader != null, $"The `{nameof(reader)}` parameter is null.");
            Debug.Assert(destination != null, $"The `{nameof(destination)}` parameter is null.");

            Match match = null;
            string section = null;

            // parse each line in the config independently - Git's configs do not accept multi-line values
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // skip empty and commented lines
                if (String.IsNullOrWhiteSpace(line))
                    continue;
                if (CommentRegex.Value.IsMatch(line))
                    continue;

                // sections begin with values like [section] or [section "section name"]. All subsequent lines,
                // until a new section is encountered, are children of the section
                if ((match = SectionRegex.Value.Match(line)).Success)
                {
                    if (match.Groups.Count >= 2 && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        // check if the section is named, if so: process the name
                        if (match.Groups.Count >= 3 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

                            // triming off enclosing quotes makes usage easier, only trim in pairs
                            if (val.Length > 0 && val[0] == '"')
                            {
                                if (val[val.Length - 1] == '"' && val.Length > 1)
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }
                                else
                                {
                                    val = val.Substring(1, val.Length - 1);
                                }
                            }

                            section += HostSplitCharacter + val;
                        }
                    }
                }
                // section children should be in the format of name = value pairs
                else if ((match = KeyValueRegex.Value.Match(line)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !String.IsNullOrEmpty(match.Groups[1].Value)
                        && !String.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

                        // triming off enclosing quotes makes usage easier, only trim in pairs
                        if (val.Length > 0 && val[0] == '"')
                        {
                            if (val[val.Length - 1] == '"' && val.Length > 1)
                            {
                                val = val.Substring(1, val.Length - 2);
                            }
                            else
                            {
                                val = val.Substring(1, val.Length - 1);
                            }
                        }
                        
                        // Test for and handle include directives
                        if ("include.path".Equals(key))
                        {
                            try
                            {
                                // This is an include directive, import the configuration values from the included file
                                string includePath = (val.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                                    ? Where.Home() + val.Substring(1, val.Length - 1)
                                    : val;

                                includePath = Path.GetFullPath(includePath);

                                using (var includeFile = File.Open(includePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (var includeReader = new StreamReader(includeFile))
                                {
                                    ParseGitConfig(includeReader, destination);
                                }
                            }
                            catch (Exception exception)
                            {
                                Trace.WriteLine($"failed to parse config file: {val}. {exception.Message}");
                            }
                        }
                        else
                        {
                            // Add or update the (key, value)
                            if (destination.ContainsKey(key))
                            {
                                destination[key] = val;
                            }
                            else
                            {
                                destination.Add(key, val);
                            }
                        }
                    }
                }
            }
        }

        public struct Entry : IEquatable<Entry>
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly StringComparer ValueComparer = StringComparer.OrdinalIgnoreCase;

            public Entry(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public readonly string Key;
            public readonly string Value;

            public override bool Equals(object obj)
            {
                return (obj is Entry)
                        && Equals((Entry)obj);
            }

            public bool Equals(Entry other)
            {
                return KeyComparer.Equals(Key, other.Key)
                    && ValueComparer.Equals(Value, other.Value);
            }

            public override int GetHashCode()
            {
                return KeyComparer.GetHashCode(Key);
            }

            public override string ToString()
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} = {1}", Key, Value);
            }

            public static bool operator ==(Entry left, Entry right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Entry left, Entry right)
                => !(left == right);
        }
    }
}
