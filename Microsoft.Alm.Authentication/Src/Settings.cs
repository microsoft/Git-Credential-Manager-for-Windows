using System;
using System.Collections;
using System.Collections.Generic;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication
{
    public interface ISettings : IRuntimeService
    {
        string CommandLine { get; }

        string CurrentDirectory { get; }

        bool Is64BitOperatingSystem { get; }

        string MachineName { get; }

        string NewLine { get; }

        OperatingSystem OsVersion { get; }

        Version Version { get; }

        string ExpandEnvironmentVariables(string name);

        void Exit(int exitCode);

        string[] GetCommandLineArgs();

        IDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target);

        IDictionary<string, string> GetEnvironmentVariables();

        string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target);

        string GetEnvironmentVariable(string variable);

        string GetFolderPath(Environment.SpecialFolder folder);
    }

    internal class Settings : Base, ISettings
    {
        public Settings(RuntimeContext context)
            : base(context)
        { }

        public string CommandLine
            => Environment.CommandLine;

        public string CurrentDirectory
            => Environment.CurrentDirectory;

        public Type ServiceType
            => typeof(ISettings);

        public bool Is64BitOperatingSystem
            => Environment.Is64BitOperatingSystem;

        public string MachineName
            => Environment.MachineName;

        public string NewLine
            => Environment.NewLine;

        public OperatingSystem OsVersion
            => Environment.OSVersion;

        public Version Version
            => Environment.Version;

        public void Exit(int exitCode)
            => Environment.Exit(exitCode);

        public string ExpandEnvironmentVariables(string name)
            => Environment.ExpandEnvironmentVariables(name);

        public string[] GetCommandLineArgs()
            => Environment.GetCommandLineArgs();

        public IDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            var variables = Environment.GetEnvironmentVariables(target);
            return DeduplicateStringDictionary(variables);
        }

        internal IDictionary<string, string> DeduplicateStringDictionary(IDictionary variables)
        {
            var result = new Dictionary<string, string>(variables.Count, OrdinalIgnoreCase);

            foreach (var key in variables.Keys)
            {
                if (key is string name && variables[key] is string value)
                {
                    // avoid trying to add duplicates, e.g. different case names, last entry wins
                    result[name] = value;
                }
            }

            return result;
        }

        public IDictionary<string, string> GetEnvironmentVariables()
            => GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        public string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
            => Environment.GetEnvironmentVariable(variable, target);

        public string GetEnvironmentVariable(string variable)
            => Environment.GetEnvironmentVariable(variable);

        public string GetFolderPath(Environment.SpecialFolder folder)
            => Environment.GetFolderPath(folder);
    }
}
