using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.TeamFoundation.Authentication
{
    public sealed class Configuration
    {
        private const char HostSplitCharacter = '.';

        public Configuration(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException("currentDirectory");
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            LoadGitConfiguation(directory);
        }

        public Configuration()
            : this(Environment.CurrentDirectory)
        { }

        private readonly Dictionary<string, string> _values;

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
            Debug.Assert(prefix != null, "The prefix parameter is null");
            Debug.Assert(suffix != null, "The suffic parameter is null");

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
            Debug.Assert(key != null, "The key parameter is null");

            Trace.WriteLine("Configuration::GetGitConfigEntry");

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

        public void LoadGitConfiguation(string directory)
        {
            string systemConfig = null;
            string globalConfig = null;
            string localConfig = null;

            Trace.WriteLine("Configuration::LoadGitConfiguation");

            // read Git's three configs from lowest priority to highest, overwriting values as
            // higher prirority configurations are parsed, storing them in a handy lookup table

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
            Debug.Assert(File.Exists(configPath), "The configPath parameter references a non-existant file.");
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
                if (Regex.IsMatch(line, @"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant))
                    continue;

                // sections begin with values like [section] or [section "section name"]. All subsequent lines,
                // until a new section is encountered, are children of the section
                if ((match = Regex.Match(line, @"^\s*\[\s*(\w+)\s*(\""[^\""]+\""){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 2 && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        // check if the section is named, if so: process the name
                        if (match.Groups.Count >= 3 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

                            // triming off enclosing quotes makes usage easier, only trim in pairs
                            if (val[0] == '"')
                            {
                                if (val[val.Length - 1] == '"')
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
                else if ((match = Regex.Match(line, @"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !String.IsNullOrEmpty(match.Groups[1].Value)
                        && !String.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

                        // triming off enclosing quotes makes usage easier, only trim in pairs
                        if (val[0] == '"')
                        {
                            if (val[val.Length - 1] == '"')
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
    }
}
