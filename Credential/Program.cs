using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitConfigValue = LibGit2Sharp.ConfigurationEntry<string>;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableDebugTrace();

            if (args.Length == 0 || args[0].Contains('?'))
            {
                PrintHelpMessage();
                return;
            }

            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            OperationArguments operationArguments = new OperationArguments(Console.In);

            RepositoryOptions repoOptions = new RepositoryOptions();

            string systemConfigPath = null;
            if (Where.GitSystemConfig(out systemConfigPath))
            {
                repoOptions.SystemConfigurationLocation = systemConfigPath;
            }

            string repoPath = null;
            if ((repoPath = Repository.Discover(Environment.CurrentDirectory)) != null)
            {
                // parse the git config for related values
                using (Repository repo = new Repository(repoPath, repoOptions))
                {
                    ParseConfiguration(repo.Config, operationArguments);
                }
            }
            else
            {
                using (Configuration configuration = new Configuration(systemConfigurationFileLocation: systemConfigPath))
                {
                    ParseConfiguration(configuration, operationArguments);
                }
            }

            foreach (string arg in args)
            {
                Trace.TraceInformation("[GIT: {0}]", arg);
                switch (arg)
                {
                    case "erase":
                        Erase(operationArguments);
                        break;

                    case "get":
                        Get(operationArguments);
                        break;

                    case "store":
                        Store(operationArguments);
                        break;
                }
            }
        }

        private static void ParseConfiguration(Configuration config, OperationArguments operationArguments)
        {
            GitConfigValue match = null;

            if ((match = GetConfig(config, operationArguments, "authority")) != null)
            {
                Trace.TraceInformation("authority = {0}", match.Value);
                operationArguments.SetScheme(match.Value);
            }
            if ((match = GetConfig(config, operationArguments, "clientid")) != null)
            {
                Trace.TraceInformation("clientid = {0}", match.Value);
                operationArguments.AuthorityClientId = match.Value;
            }
            if ((match = GetConfig(config, operationArguments, "resource")) != null)
            {
                Trace.TraceInformation("resource = {0}", match.Value);
                operationArguments.AuthorityResource = match.Value;
            }
            if ((match = GetConfig(config, operationArguments, "validate")) != null)
            {
                Trace.TraceInformation("validate = {0}", match.Value);
                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(match.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
            }
            if ((match = GetConfig(config, operationArguments, "interactive")) != null)
            {
                Trace.TraceInformation("interactive = {0}", match.Value);
                if (String.Equals("always", match.Value, StringComparison.OrdinalIgnoreCase)
                    || String.Equals("true", match.Value, StringComparison.OrdinalIgnoreCase)
                    || String.Equals("force", match.Value, StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Interactivity = Interactivity.Always;
                }
                else if (String.Equals("never", match.Value, StringComparison.OrdinalIgnoreCase)
                   || String.Equals("false", match.Value, StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Interactivity = Interactivity.Never;
                }
            }
            if ((match = GetConfig(config, operationArguments, "validate")) != null)
            {
                Trace.TraceInformation("validate = {0}", match.Value);
                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(match.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
            }
        }

        private static void PrintHelpMessage()
        {
            Console.Out.WriteLine("usage: git-credential-man <command> [<args>]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Commands:");
            Console.Out.WriteLine("   need");
            Console.Out.WriteLine("   to");
            Console.Out.WriteLine("   design");
            Console.Out.WriteLine("   this");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Configuration Options:");
            Console.Out.WriteLine("   authority      Defines the type of authentication to be used.");
            Console.Out.WriteLine("                  Supportd Basic, AAD, and MSA. Default is Basic.");
            Console.Out.WriteLine("   clientid       Defines the client identifier for the authority.");
            Console.Out.WriteLine("                  Defaults to visualstudio.com. Only used by AAD authority.");
            Console.Out.WriteLine("   resource       Defines the resource identifier for the authority.");
            Console.Out.WriteLine("                  Defaults to visualstudio.com. Only used by AAD authority.");
            Console.Out.WriteLine("   interactive    Specifies if user can be prompted for credentials or not.");
            Console.Out.WriteLine("                  Supports Auto, Always, or Never. Defaults to Auto.");
            Console.Out.WriteLine("                  Only used by AAD authority.");
            Console.Out.WriteLine("   validate       Causes validation of credentials before supplying them");
            Console.Out.WriteLine("                  to Git. Invalid credentials are attemped to refreshed");
            Console.Out.WriteLine("                  before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("                  Defaults to TRUE. Ignore by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine(@"   [credential ""microsoft.visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = AAD");
            Console.Out.WriteLine(@"   [credential ""visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = MSA");
            Console.Out.WriteLine(@"   [credential]");
            Console.Out.WriteLine(@"       helper = !'C:\\Tools\\Git\\git-credential-man.exe'");
        }

        private static void Erase(OperationArguments operationArguments)
        {
            Trace.TraceInformation("Program.Erase");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.TraceInformation("targetUri = {0}", operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    BaseVsoAuthentication vsoAuth = authentication as BaseVsoAuthentication;
                    vsoAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;
            }
        }

        private static void Get(OperationArguments operationArguments)
        {
            Trace.TraceInformation("Program.Get");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.TraceInformation("targetUri = {0}", operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = null;

            Trace.TraceInformation("authority is " + operationArguments.Authority);
            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    if (authentication.GetCredentials(operationArguments.TargetUri, out credentials))
                    {
                        Trace.TraceInformation("credentials found");
                        operationArguments.SetCredentials(credentials);
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    VsoAadAuthentication aadAuth = authentication as VsoAadAuthentication;
                    if (aadAuth.GetCredentials(operationArguments.TargetUri, out credentials))
                    {
                        Trace.TraceInformation("credentials found");
                        if (!operationArguments.ValidateCredentials || Task.Run(async () => { return await aadAuth.ValidateCredentials(credentials); }).Result)
                        {
                            operationArguments.SetCredentials(credentials);
                        }
                        else
                        {
                            Trace.TraceWarning("credentials are invalid");
                            credentials = null;
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("credentials not found");
                        credentials = null;
                    }

                    if (credentials == null)
                    {
                        Trace.TraceInformation("attempting non-interactive logon with credential prompt fallback");
                        Task.Run(async () =>
                        {
                            if ((operationArguments.Interactivity != Interactivity.Always
                                && await aadAuth.NoninteractiveLogon(operationArguments.TargetUri, true)
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && aadAuth.InteractiveLogon(operationArguments.TargetUri, true)
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials)))
                            {
                                operationArguments.SetCredentials(credentials);
                            }
                        }).Wait();
                    }
                    break;

                case AuthorityType.MicrosoftAccount:
                    VsoMsaAuthentication msaAuth = authentication as VsoMsaAuthentication;
                    if (msaAuth.GetCredentials(operationArguments.TargetUri, out credentials))
                    {
                        Trace.TraceInformation("credentials found");
                        if (operationArguments.ValidateCredentials)
                        {
                            Trace.TraceInformation("validation requested");
                            Task.Run(async () =>
                            {
                                if (await msaAuth.ValidateCredentials(credentials)
                                    || await msaAuth.RefreshCredentials(operationArguments.TargetUri, true)
                                    || msaAuth.InteractiveLogon(operationArguments.TargetUri, true))
                                {
                                    operationArguments.SetCredentials(credentials);
                                }
                            }).Wait();
                        }
                        else
                        {
                            operationArguments.SetCredentials(credentials);
                        }
                    }
                    else
                    {
                        Trace.TraceInformation("attempting prompted logon");
                        if (msaAuth.InteractiveLogon(operationArguments.TargetUri, true)
                            && msaAuth.GetCredentials(operationArguments.TargetUri, out credentials))
                        {
                            operationArguments.SetCredentials(credentials);
                        }
                    }
                    break;
            }

            Console.Out.Write(operationArguments);
        }

        private static void Store(OperationArguments operationArguments)
        {
            Trace.WriteLine("Program.Store");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    Trace.WriteLine("writing basic authentication values");

                    BaseAuthentication authentication = CreateAuthentication(operationArguments);
                    Credential credentials = new Credential(operationArguments.Username, operationArguments.Password ?? String.Empty);

                    authentication.SetCredentials(operationArguments.TargetUri, credentials);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    // not supported
                    break;
            }
        }

        private static BaseAuthentication CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");

            Trace.TraceInformation("creating authentication for {0}", operationArguments.Authority);

            switch (operationArguments.Authority)
            {
                case AuthorityType.AzureDirectory:
                    // if the clientId and resource values exist, use them
                    if (!String.IsNullOrWhiteSpace(operationArguments.AuthorityClientId) && !String.IsNullOrWhiteSpace(operationArguments.AuthorityResource))
                    {
                        string clientId = operationArguments.AuthorityClientId;
                        string resource = operationArguments.AuthorityResource;

                        Trace.TraceInformation("resource = {0}, clientId = {1}", resource, clientId);
                        return new VsoAadAuthentication(VsoTokenScope.CodeWrite, resource, clientId);
                    }
                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication(VsoTokenScope.CodeWrite);

                case AuthorityType.Basic:
                default:
                    return new BasicAuthentication();

                case AuthorityType.MicrosoftAccount:
                    // if the clientId and resource values exist, use them
                    if (!String.IsNullOrWhiteSpace(operationArguments.AuthorityClientId) && !String.IsNullOrWhiteSpace(operationArguments.AuthorityResource))
                    {
                        string clientId = operationArguments.AuthorityClientId;
                        string resource = operationArguments.AuthorityResource;

                        Trace.TraceInformation("resource = {0}, clientId = {1}", resource, clientId);
                        return new VsoMsaAuthentication(VsoTokenScope.CodeWrite, resource, clientId);
                    }
                    // return a generic MSA backed VSO authentication object
                    return new VsoMsaAuthentication(VsoTokenScope.CodeWrite);
            }
        }

        static readonly char[] HostSplitCharacters = new char[] { '.' };

        private static GitConfigValue GetConfig(Configuration config, OperationArguments operationArguments, string key)
        {
            Debug.Assert(config != null, "The config parameter is null");
            Debug.Assert(operationArguments != null, "The operationArguments parameter is null");
            Debug.Assert(operationArguments.Protocol != null, "The operationArguments.Protocol parameter is null");
            Debug.Assert(operationArguments.Host != null, "The operationArguments.Host parameter is null");
            Debug.Assert(key != null, "The key parameter is null");

            // return match seeking from most specific (credenial.<schema>://<uri>.<key>) to least specific (credential.<key>)
            var result = GetConfig(config, "credential", String.Format("{0}://{1}", operationArguments.Protocol, operationArguments.Host), key)
                      ?? GetConfig(config, "credential", operationArguments.Host, key);
            if (result == null && !String.IsNullOrWhiteSpace(operationArguments.Host))
            {
                string[] fragments = operationArguments.Host.Split(HostSplitCharacters, StringSplitOptions.RemoveEmptyEntries);
                string host = null;

                // look for host matches stripping a single sub-domain at a time off
                // don't match against a top-level domain (aka ".com")
                for (int i = 1; result == null && i < fragments.Length - 1; i++)
                {
                    host = String.Join(".", fragments, i, fragments.Length - i);
                    result = GetConfig(config, "credential", host, key);
                }
            }

            return result ?? GetConfig(config, "credential", String.Empty, key);
        }

        private static GitConfigValue GetConfig(Configuration config, string prefix, string key, string suffix)
        {
            Debug.Assert(config != null, "The config parameter is null");
            Debug.Assert(prefix != null, "The prefix parameter is null");
            Debug.Assert(suffix != null, "The suffic parameter is null");

            string match = String.IsNullOrEmpty(key)
                ? String.Format("{0}.{1}", prefix, suffix)
                : String.Format("{0}.{1}.{2}", prefix, key, suffix);

            Trace.TraceInformation("seeking '{0}'", match);

            var result = config.Where((GitConfigValue entry) =>
                                {                                    
                                    return String.Equals(entry.Key, match, StringComparison.OrdinalIgnoreCase);
                                })
                               .OrderBy((GitConfigValue entry) => { return entry.Key.Length; })
                               .OrderByDescending((GitConfigValue entry) => { return entry.Level; })
                               .FirstOrDefault();
            if (result != null)
            {
                Trace.TraceInformation("matched: {0}.{1} = {2}", prefix, suffix, result.Key);
            }
            return result;
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communcations protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
