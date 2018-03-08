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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication.Git
{
    public class Configuration : Base, IEnumerable<Configuration.Entry>
    {
        private const char HostSplitCharacter = '.';

        private static readonly Regex CommentRegex = new Regex(@"^\s*[#;]", RegexOptions.CultureInvariant);
        private static readonly Regex KeyValueRegex = new Regex(@"^\s*(\w+)\s*=\s*(.+)", RegexOptions.CultureInvariant);
        private static readonly Regex SectionRegex = new Regex(@"^\s*\[\s*(\w+)\s*(\""[^\]]+){0,1}\]", RegexOptions.CultureInvariant);

        internal Configuration(RuntimeContext context)
            : base(context)
        {
            _values = new Dictionary<ConfigurationLevel, Dictionary<string, string>>()
            {
                { ConfigurationLevel.Global, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Local, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Portable, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.System, new Dictionary<string, string>(Entry.KeyComparer) },
                { ConfigurationLevel.Xdg, new Dictionary<string, string>(Entry.KeyComparer) },
            };
        }

        internal Configuration(RuntimeContext context, Dictionary<ConfigurationLevel, Dictionary<string, string>> values)
            : base(context)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));

            _values = new Dictionary<ConfigurationLevel, Dictionary<string, string>>(values.Count);

            // Copy the dictionary.
            foreach (KeyValuePair<ConfigurationLevel, Dictionary<string, string>> level in values)
            {
                var levelValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, string> item in level.Value)
                {
                    levelValues.Add(item.Key, item.Value);
                }

                _values.Add(level.Key, levelValues);
            }
        }

        private readonly Dictionary<ConfigurationLevel, Dictionary<string, string>> _values;

        /// <summary>
        /// Gets an enumeration of possible Git configuration levels.
        /// </summary>
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

        public virtual string this[string key]
        {
            get
            {
                foreach (ConfigurationLevel level in Levels)
                {
                    if (_values[level].ContainsKey(key))
                        return _values[level][key];
                }

                return null;
            }
        }

        public virtual int Count
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

        public virtual bool ContainsKey(string key)
        {
            return ContainsKey(ConfigurationLevel.All, key);
        }

        public virtual bool ContainsKey(ConfigurationLevel levels, string key)
        {
            foreach (ConfigurationLevel level in Levels)
            {
                if ((level & levels) != 0
                    && _values[level].ContainsKey(key))
                    return true;
            }

            return false;
        }

        public virtual IEnumerator<Entry> EnumerateEntriesByLevel(ConfigurationLevel desiredLevels)
        {
            ConfigurationLevel[] levels = new[]
            {
                    ConfigurationLevel.Portable,
                    ConfigurationLevel.System,
                    ConfigurationLevel.Xdg,
                    ConfigurationLevel.Global,
                    ConfigurationLevel.Local,
                };

            foreach (ConfigurationLevel level in levels)
            {
                // Skip levels not present in the mask.
                if ((desiredLevels & level) == 0)
                    continue;

                if (_values.TryGetValue(level, out Dictionary<string, string> values))
                {
                    foreach (KeyValuePair<string, string> value in values)
                    {
                        yield return new Entry(value.Key, value.Value, level);
                    }
                }
            }
        }

        public virtual IEnumerator<Entry> GetEnumerator()
            => EnumerateEntriesByLevel(ConfigurationLevel.All);

        public static bool PaserBoolean(string value)
        {
            return value != null
                && ((int.TryParse(value, out int ival) && ival > 0)
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "true")
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "yes")
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "on"));
        }

        public virtual async Task LoadGitConfiguration(string directory, ConfigurationLevel types)
        {
            string portableConfig = null;
            string systemConfig = null;
            string xdgConfig = null;
            string globalConfig = null;
            string localConfig = null;

            // Read Git's five configuration files from lowest priority to highest, overwriting values as higher priority configurations are parsed, storing them in a handy lookup table.

            // Find and parse Git's portable configuration file.
            if ((types & ConfigurationLevel.Portable) != 0
                && Where.GitPortableConfig(out portableConfig))
            {
                await ParseGitConfig(ConfigurationLevel.Portable, portableConfig);
            }

            // Find and parse Git's system configuration file.
            if ((types & ConfigurationLevel.System) != 0
                && Where.GitSystemConfig(null, out systemConfig))
            {
                await ParseGitConfig(ConfigurationLevel.System, systemConfig);
            }

            // Find and parse Git's XDG configuration file.
            if ((types & ConfigurationLevel.Xdg) != 0
                && Where.GitXdgConfig(out xdgConfig))
            {
                await ParseGitConfig(ConfigurationLevel.Xdg, xdgConfig);
            }

            // Find and parse Git's global configuration file.
            if ((types & ConfigurationLevel.Global) != 0
                && Where.GitGlobalConfig(out globalConfig))
            {
                await ParseGitConfig(ConfigurationLevel.Global, globalConfig);
            }

            // Find and parse Git's local configuration file.
            if ((types & ConfigurationLevel.Local) != 0
                && Where.GitLocalConfig(directory, out localConfig))
            {
                await ParseGitConfig(ConfigurationLevel.Local, localConfig);
            }

            Trace.WriteLine($"git {types} config read, {Count} entries.");
        }

        /// <summary>
        /// Reads in Git's configuration files, parses them, and combines them into a single database.
        /// <para/>
        /// Returns the combined database of configuration data.
        /// </summary>
        /// <param name="directory">Optional working directory of a repository from which to read its Git local configuration.</param>
        /// <param name="loadLocal">Read, parse, and include Git local configuration values if `<see langword="true"/>`; otherwise do not.</param>
        /// <param name="loadSystem">Read, parse, and include Git system configuration values if `<see langword="true"/>`; otherwise do not.</param>
        public static async Task<Configuration> ReadConfiuration(
            RuntimeContext context,
            string directory,
            bool loadLocal,
            bool loadSystem)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));
            if (!context.FileSystem.DirectoryExists(directory))
            {
                var inner = new DirectoryNotFoundException(directory);
                throw new ArgumentException(inner.Message, nameof(directory), inner);
            }

            ConfigurationLevel types = ConfigurationLevel.All;

            if (!loadLocal)
            {
                types ^= ConfigurationLevel.Local;
            }

            if (!loadSystem)
            {
                types ^= ConfigurationLevel.System;
            }

            var config = new Configuration(context);
            await config.LoadGitConfiguration(directory, types);

            return config;
        }

        public virtual bool TryGetEntry(string prefix, string key, string suffix, out Entry entry)
        {
            if (prefix is null)
                throw new ArgumentNullException(nameof(prefix));
            if (suffix is null)
                throw new ArgumentNullException(nameof(suffix));

            string match = string.IsNullOrEmpty(key)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", prefix, suffix)
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}", prefix, key, suffix);

            // If there's a match, return it.
            if (ContainsKey(match))
            {
                foreach (ConfigurationLevel level in Levels)
                {
                    if (_values[level].ContainsKey(match))
                    {
                        entry = new Entry(key, _values[level][match], level);
                        return true;
                    }
                }
            }

            // Nothing found.
            entry = default(Entry);
            return false;
        }

        public virtual bool TryGetEntry(string prefix, Uri targetUri, string key, out Entry entry)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (targetUri != null)
            {
                // Return match seeking from most specific (<prefix>.<scheme>://<host>.<key>) to least specific (credential.<key>).
                if (TryGetEntry(prefix, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}", targetUri.Scheme, targetUri.Host), key, out entry)
                    || TryGetEntry(prefix, targetUri.Host, key, out entry))
                    return true;

                if (!string.IsNullOrWhiteSpace(targetUri.Host))
                {
                    string[] fragments = targetUri.Host.Split(HostSplitCharacter);
                    string host = null;

                    // Look for host matches stripping a single sub-domain at a time off don't
                    // match against a top-level domain (aka ".com").
                    for (int i = 1; i < fragments.Length - 1; i++)
                    {
                        host = string.Join(".", fragments, i, fragments.Length - i);
                        if (TryGetEntry(prefix, host, key, out entry))
                            return true;
                    }
                }
            }

            // Try to find an unadorned match as a complete fallback.
            if (TryGetEntry(prefix, string.Empty, key, out entry))
                return true;

            // Nothing found.
            entry = default(Entry);
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        internal static async Task ParseGitConfig(
            RuntimeContext context,
            TextReader reader,
            IDictionary<string, string> destination)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            Match match = null;
            string section = null;

            // Parse each line in the config independently - Git's configuration do not accept multi-line values.
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Skip empty and commented lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (CommentRegex.IsMatch(line))
                    continue;

                // Sections begin with values like [section] or [section "section name"]. All
                // subsequent lines, until a new section is encountered, are children of the section.
                if ((match = SectionRegex.Match(line)).Success)
                {
                    if (match.Groups.Count >= 2 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        // Check if the section is named, if so: process the name.
                        if (match.Groups.Count >= 3 && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

                            // Trim off enclosing quotes to make usage easier, only trim in pairs.
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
                // Section children should be in the format of name = value pairs.
                else if ((match = KeyValueRegex.Match(line)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !string.IsNullOrEmpty(match.Groups[1].Value)
                        && !string.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

                        // Trim off enclosing quotes to make usage easier, only trim in pairs.
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
                                    ? context.Where.Home() + val.Substring(1, val.Length - 1)
                                    : val;

                                includePath = Path.GetFullPath(includePath);

                                using (FileStream includeFile = context.FileSystem.FileOpen(includePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (var includeReader = new StreamReader(includeFile))
                                {
                                    await ParseGitConfig(context, includeReader, destination);
                                }
                            }
                            catch (Exception exception)
                            {
                                context.Trace.WriteLine($"failed to parse config file: {val}. {exception.Message}");
                            }
                        }
                        else
                        {
                            // Add or update the (key, value).
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

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private async Task ParseGitConfig(ConfigurationLevel level, string configPath)
        {
            if (configPath is null)
                throw new ArgumentNullException(nameof(configPath));
            if ((level & ~ConfigurationLevel.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(level));

            if (!FileSystem.FileExists(configPath))
            {
                var inner = new FileNotFoundException(configPath);
                throw new ArgumentException(inner.Message, nameof(configPath), inner);
            }

            if (!_values.ContainsKey(level))
                return;
            if (!FileSystem.FileExists(configPath))
                return;

            using (FileStream stream = FileSystem.FileOpen(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                await ParseGitConfig(Context, reader, _values[level]);
            }
        }

        public struct Entry : IEquatable<Entry>
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
            public static readonly StringComparer ValueComparer = StringComparer.OrdinalIgnoreCase;

            public Entry(string key, string value, ConfigurationLevel level)
            {
                _key = key;
                _level = level;
                _value = value;
            }

            private readonly string _key;
            private readonly ConfigurationLevel _level;
            private readonly string _value;

            /// <summary>
            /// Gets the name, or key, of the configuration entry.
            /// </summary>
            public string Key { get { return _key; } }

            /// <summary>
            /// Gets the configuration level of the entry.
            /// </summary>
            public ConfigurationLevel Level { get { return _level; } }

            /// <summary>
            /// Gets the value of the configuration entry.
            /// </summary>
            public string Value { get { return _value; } }

            public override bool Equals(object obj)
            {
                return (obj is Entry)
                        && Equals((Entry)obj);
            }

            public bool Equals(Entry other)
            {
                return KeyComparer.Equals(_key, other._key)
                    && ValueComparer.Equals(_value, other._value);
            }

            public override int GetHashCode()
            {
                return KeyComparer.GetHashCode(_key);
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} = {1}", _key, _value);
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
