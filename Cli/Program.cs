using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            // parse the git config for related values
            using (Repository repo = new Repository(Environment.CurrentDirectory))
            {
                GitConfigValue match = null;

                if ((match = GetConfig(repo, operationArguments, "authority")) != null)
                {
                    Trace.TraceInformation("authority = {0}", match.Value);
                    operationArguments.SetScheme(match.Value);
                }
                if ((match = GetConfig(repo, operationArguments, "clientid")) != null)
                {
                    Trace.TraceInformation("clientid = {0}", match.Value);
                    operationArguments.AuthorityClientId = match.Value;
                }
                if ((match = GetConfig(repo, operationArguments, "resource")) != null)
                {
                    Trace.TraceInformation("resource = {0}", match.Value);
                    operationArguments.AuthorityResource = match.Value;
                }
                if ((match = GetConfig(repo, operationArguments, "tenantid")) != null)
                {
                    Trace.TraceInformation("tenantid = {0}", match.Value);
                    operationArguments.AuthorityTenantId = match.Value;
                }
                if ((match = GetConfig(repo, operationArguments, "validate")) != null)
                {
                    Trace.TraceInformation("validate = {0}", match.Value);
                    bool validate = operationArguments.ValidateCredentials;
                    if (Boolean.TryParse(match.Value, out validate))
                    {
                        operationArguments.ValidateCredentials = validate;
                    }
                }
                if ((match = GetConfig(repo, operationArguments, "interactive")) != null)
                {
                    Trace.TraceInformation("interactive = {0}", match.Value);
                    bool interactive = operationArguments.UseInteractiveFlows;
                    if (Boolean.TryParse(match.Value, out interactive))
                    {
                        operationArguments.UseInteractiveFlows = interactive;
                    }
                }
                if ((match = GetConfig(repo, operationArguments, "validate")) != null)
                {
                    Trace.TraceInformation("validate = {0}", match.Value);
                    bool validate = operationArguments.ValidateCredentials;
                    if (Boolean.TryParse(match.Value, out validate))
                    {
                        operationArguments.ValidateCredentials = validate;
                    }
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
            Console.Out.WriteLine("                  Support Basic, AAD, and MSA. Default is Basic.");
            Console.Out.WriteLine("   clientid       Defines the client identifier for the authority.");
            Console.Out.WriteLine("                  Defaults to visualstudio.com. Ignore by Basic authority.");
            Console.Out.WriteLine("   resource       Defines the resource identifier for the authority.");
            Console.Out.WriteLine("                  Defaults to visualstudio.com. Ignore by Basic authority.");
            Console.Out.WriteLine("   tenantid       Defines the tenant identifier for the authority.");
            Console.Out.WriteLine("                  Defaults to Visual Studio. Ignore by Basic authority.");
            Console.Out.WriteLine("   interactive    Forces the helper to only authenticte interactive.");
            Console.Out.WriteLine("                  or non-interative flows. Defaulst to TRUE.");
            Console.Out.WriteLine("                  Ignore by Basic authority.");
            Console.Out.WriteLine("   validate       Causes validation of credentials before supplying them");
            Console.Out.WriteLine("                  to Git. Invalid credentials are attemped to refreshed");
            Console.Out.WriteLine("                  before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("                  Defaults to TRUE. Ignore by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine("   [credential]");
            Console.Out.WriteLine(@"       helper = !'C:\\Program Files (x86)\\Git\\libexec\\git-core\\git-credential-man.exe'");
            Console.Out.WriteLine("   [credential \"microsoft.visualstudio.com\"]");
            Console.Out.WriteLine(@"       helper = !'C:\\Program Files (x86)\\Git\\libexec\\git-core\\git-credential-man.exe'");
            Console.Out.WriteLine(@"       authority = AAD");
            Console.Out.WriteLine(@"       validate = true");
        }

        private static void Erase(OperationArguments operationArguments)
        {
            Trace.TraceInformation("Program.Erase");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.TraceInformation("targetUri = {0}", operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            authentication.DeleteCredentials(operationArguments.TargetUri);
        }

        private static void Get(OperationArguments operationArguments)
        {
            Trace.TraceInformation("Program.Get");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.TraceInformation("targetUri = {0}", operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credentials credentials = null;

            if (authentication != null && authentication.GetCredentials(operationArguments.TargetUri, out credentials))
            {
                Trace.TraceInformation("credentials found");
                if (operationArguments.Authority == AuthorityType.AzureDirectory || operationArguments.Authority == AuthorityType.MicrosoftAccount)
                {
                    Trace.TraceInformation("authority is " + operationArguments.Authority);
                    BaseVsoAuthentication vsoAuthentiaction = authentication as BaseVsoAuthentication;
                    if (operationArguments.ValidateCredentials)
                    {
                        Trace.TraceInformation("credential validation requested");
                        Task.Run(async () =>
                        {
                            if (await vsoAuthentiaction.ValidateCredentials(credentials))
                            {
                                Trace.TraceInformation("credential validation success");
                                operationArguments.SetCredentials(credentials);
                            }
                            else
                            {
                                Trace.TraceInformation("requesting token refresh");
                                if (await vsoAuthentiaction.RefreshCredentials(operationArguments.TargetUri) && vsoAuthentiaction.GetCredentials(operationArguments.TargetUri, out credentials))
                                {
                                    Trace.TraceInformation("token refesh successful");
                                    operationArguments.SetCredentials(credentials);
                                }
                                else
                                {
                                    Trace.TraceInformation("token refesh failure");
                                    credentials = null;
                                }
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
                    operationArguments.SetCredentials(credentials);
                }
            }

            // if the authority supports VSO personal access tokens, the username + password promp git provides will be insufficient
            // instead of relying on Git then failing, open a modal dialog and request credentials from the user
            // and use those credentials to logon to the service and generate a personal access token
            // then return the personal access token to the user
            if (credentials == null
                && (operationArguments.Authority == AuthorityType.AzureDirectory || operationArguments.Authority == AuthorityType.MicrosoftAccount))
            {
                if (operationArguments.UseInteractiveFlows)
                {
                    Trace.TraceInformation("authority is {0}, launching credential dialog", operationArguments.Authority);
                    if ((authentication as BaseVsoAuthentication).RequestUserCredentials(operationArguments.TargetUri, out credentials))
                    {
                        Trace.TraceInformation("credentials collected from user");
                        Task.Run(async () =>
                        {
                            if (await (authentication as BaseVsoAuthentication).InteractiveLogon(operationArguments.TargetUri, credentials)
                            && authentication.GetCredentials(operationArguments.TargetUri, out credentials))
                            {
                                Trace.TraceInformation("credentials captured and stored");
                                operationArguments.SetCredentials(credentials);
                            }
                        }).Wait();
                    }
                }
                else if (operationArguments.Authority == AuthorityType.AzureDirectory)
                {
                    Trace.TraceInformation("attempting non-interactive logon");
                    Task.Run(async () =>
                    {
                        try
                        {
                            // logon to the service via non-interactive logon and return the personal access token
                            if (await (authentication as VsoAadAuthentication).NoninteractiveLogon(operationArguments.TargetUri)
                                && authentication.GetCredentials(operationArguments.TargetUri, out credentials))
                            {
                                Trace.TraceInformation("credentials captured and stored");
                                operationArguments.SetCredentials(credentials);
                            }
                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError(exception.ToString());
                        }
                    })
                    .Wait();
                }
            }

            Console.Out.Write(operationArguments);
        }

        private static void Store(OperationArguments operationArguments)
        {
            Trace.WriteLine("Program.Store");

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            if (operationArguments.Authority == AuthorityType.Basic)
            {
                Trace.WriteLine("writing basic authentication values");

                BaseAuthentication authentication = CreateAuthentication(operationArguments);
                Credentials credentials = new Credentials(operationArguments.Username, operationArguments.Password ?? String.Empty);

                authentication.SetCredentials(operationArguments.TargetUri, credentials);
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
                        Guid clientId = Guid.Empty;
                        string resource = operationArguments.AuthorityResource;

                        if (Guid.TryParse(operationArguments.AuthorityClientId, out clientId))
                        {
                            // if the tenant value use it
                            if (!String.IsNullOrWhiteSpace(operationArguments.AuthorityTenantId))
                            {
                                Guid tenantId = Guid.Empty;

                                if (Guid.TryParse(operationArguments.AuthorityTenantId, out tenantId))
                                {
                                    // return a custom AAD backed VSO authentication objects
                                    Trace.TraceInformation("resource = {0}, clientId = {1}, tenantId = {2}", resource, clientId, tenantId);
                                    return new VsoAadAuthentication(tenantId, resource, clientId);
                                }
                            }
                            // return a common tenant AAD backed VSO authentication object
                            Trace.TraceInformation("resource = {0}, clientId = {1}", resource, clientId);
                            return new VsoAadAuthentication(resource, clientId);
                        }
                    }
                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication();

                case AuthorityType.Basic:
                default:
                    return new BasicAuthentication();

                case AuthorityType.MicrosoftAccount:
                    // if the clientId and resource values exist, use them
                    if (!String.IsNullOrWhiteSpace(operationArguments.AuthorityClientId) && !String.IsNullOrWhiteSpace(operationArguments.AuthorityResource))
                    {
                        Guid clientId = Guid.Empty;
                        string resource = operationArguments.AuthorityResource;

                        if (Guid.TryParse(operationArguments.AuthorityClientId, out clientId))
                        {
                            // return a common tenant MSA backed VSO authentication object
                            Trace.TraceInformation("resource = {0}, clientId = {1}", resource, clientId);
                            return new VsoMsaAuthentation(resource, clientId);
                        }
                    }
                    // return a generic MSA backed VSO authentication object
                    return new VsoMsaAuthentation();
            }
        }

        private static GitConfigValue GetConfig(Repository repo, OperationArguments operationArguments, string key)
        {
            Debug.Assert(repo != null, "The repo parameter is null");
            Debug.Assert(repo.Config != null, "The repo.Config parameter is null");
            Debug.Assert(operationArguments != null, "The operationArguments parameter is null");
            Debug.Assert(operationArguments.Protocol != null, "The operationArguments.Protocol parameter is null");
            Debug.Assert(operationArguments.Host != null, "The operationArguments.Host parameter is null");
            Debug.Assert(key != null, "The key parameter is null");

            // return match seeksing from most specific (credenial.<schema>://<uri>.<key>) to least specific (credential.<key>)
            return GetConfig(repo, String.Format("credential.{0}://{1}", operationArguments.Protocol, operationArguments.Host), key)
                   ?? GetConfig(repo, String.Format("credential.{0}", operationArguments.Host), key)
                   ?? GetConfig(repo, "credential", key);
        }

        private static GitConfigValue GetConfig(Repository repo, string prefix, string suffix)
        {
            Debug.Assert(repo != null, "The repo parameter is null");
            Debug.Assert(repo.Config != null, "The repo.Config parameter is null");
            Debug.Assert(prefix != null, "The prefix parameter is null");
            Debug.Assert(suffix != null, "The suffic parameter is null");

            return repo.Config.Where((GitConfigValue entry) =>
                                     {
                                         return entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                                             && entry.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
                                     })
                              .OrderByDescending((GitConfigValue entry) => { return entry.Level; })
                              .FirstOrDefault();
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communcations protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
