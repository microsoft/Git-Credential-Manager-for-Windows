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
        public static readonly string[] LegalConfigNames = { "local", "global", "system" };

        private const char HostSplitCharacter = '.';

        private static readonly Lazy<Regex> CommentRegex = new Lazy<Regex>(() => new Regex(@"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant));
        private static readonly Lazy<Regex> KeyValueRegex = new Lazy<Regex>(() => new Regex(@"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant));
        private static readonly Lazy<Regex> SectionRegex = new Lazy<Regex>(() => new Regex(@"^\s*\[\s*(\w+)\s*(\""[^\]]+){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant));

        public Configuration(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException("directory");
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            LoadGitConfiguration(directory);
        }

        public Configuration()
            : this(Environment.CurrentDirectory)
        { }

        internal Configuration(TextReader configReader)
        {
            ParseGitConfig(configReader, _values);
        }

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string key]
        {
            get { return _values[key]; }
        }

        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public bool TryGetEntry(string prefix, string key, string suffix, out Entry entry)
        {
            if (ReferenceEquals(prefix, null))
                throw new ArgumentNullException(nameof(prefix));
            if (ReferenceEquals(suffix, null))
                throw new ArgumentNullException(nameof(suffix));

            string match = String.IsNullOrEmpty(key)
                ? String.Format("{0}.{1}", prefix, suffix)
                : String.Format("{0}.{1}.{2}", prefix, key, suffix);

            // if there's a match, return it
            if (_values.ContainsKey(match))
            {
                entry = new Entry(match, _values[match]);
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
                if (TryGetEntry(prefix, String.Format("{0}://{1}", targetUri.Scheme, targetUri.Host), key, out entry)
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

        public void LoadGitConfiguration(string directory)
        {
            string portableConfig = null;
            string systemConfig = null;
            string globalConfig = null;
            string localConfig = null;

            Trace.WriteLine("Configuration::LoadGitConfiguration");

            // read Git's four configs from lowest priority to highest, overwriting values as
            // higher priority configurations are parsed, storing them in a handy lookup table

            // find and parse Git's portable config
            if (Where.GitPortableConfig(out portableConfig))
            {
                ParseGitConfig(portableConfig);
            }

            // find and parse Git's system config
            if (Where.GitSystemConfig(out systemConfig))
            {
                ParseGitConfig(systemConfig);
            }

            // find and parse Git's global config
            if (Where.GitGlobalConfig(out globalConfig))
            {
                ParseGitConfig(globalConfig);
            }

            // find and parse Git's local config
            if (Where.GitLocalConfig(directory, out localConfig))
            {
                ParseGitConfig(localConfig);
            }

            foreach (var pair in _values)
            {
                Trace.WriteLine(String.Format("   {0} = {1}", pair.Key, pair.Value));
            }
        }

        private void ParseGitConfig(string configPath)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(configPath), "The configPath parameter is null or invalid.");
            Debug.Assert(File.Exists(configPath), "The configPath parameter references a non-existent file.");
            Debug.Assert(_values != null, "The configPath parameter is null or invalid.");

            Trace.WriteLine("Configuration::ParseGitConfig");

            if (!File.Exists(configPath))
                return;

            using (var sr = new StreamReader(File.OpenRead(configPath)))
            {
                ParseGitConfig(sr, _values);
            }
        }

        internal static void ParseGitConfig(TextReader tr, IDictionary<string, string> destination)
        {
            Match match = null;
            string section = null;

            // parse each line in the config independently - Git's configs do not accept multi-line values
            string line;
            while ((line = tr.ReadLine()) != null)
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

                        // add or update the (key, value)
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

        public struct Entry
        {
            public Entry(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public readonly string Key;
            public readonly string Value;
        }

        [Flags]
        public enum Type
        {
            None = 0,
            Local = 1 << 0,
            Global = 1 << 1,
            Xdg = 1 << 2,
            System = 1 << 3,
            Portable = 1 << 4,
        }
    }
}
