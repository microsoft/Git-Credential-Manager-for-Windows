using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.Linq;
using GitConfigValue = LibGit2Sharp.ConfigurationEntry<string>;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    class Program
    {
        private static readonly Guid TenantId = new Guid("72f988bf-86f1-41af-91ab-2d7cd011db47");
        private static readonly string Resource = "499b84ac-1321-427f-aa17-267ca6975798";
        private static readonly Guid ClientId = new Guid("872cd9fa-d31f-45e0-9eab-6e460a02d1f1");

        static void Main(string[] args)
        {
            OperationArguments operationArguments = new OperationArguments(Console.In);
            using (Repository repo = new Repository(Environment.CurrentDirectory))
            {
                GitConfigValue match = null;

                if ((match = GetConfig(repo, operationArguments, "schema")) != null)
                {
                    operationArguments.SetScheme(match.Value);
                }
                if ((match = GetConfig(repo, operationArguments, "clientid")) != null)
                {
                    operationArguments.AuthorityClientId = match.Value;
                }
                if ((match = GetConfig(repo, operationArguments, "resource")) != null)
                {
                    operationArguments.AuthorityResource = match.Value;
                }
                if ((match = GetConfig(repo, operationArguments, "tenantid")) != null)
                {
                    operationArguments.AuthorityTenantId = match.Value;
                }
            }

            foreach (string arg in args)
            {
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

        private static void Erase(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            authentication.DeleteCredentials(operationArguments.TargetUri);
        }

        private static void Get(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credentials credentials = null;

            if (authentication != null && authentication.GetCredentials(operationArguments.TargetUri, out credentials))
            {
                operationArguments.SetCredentials(credentials);
            }

            Console.Out.Write(operationArguments);
        }

        private static void Store(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.Password != null, "The operaionArgument.Password is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credentials credentials = new Credentials(operationArguments.Username, operationArguments.Password);

            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }

        private static BaseAuthentication CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");

            switch (operationArguments.Scheme)
            {
                case CredentialType.AzureDirectory:
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
                                    return new VsoAadAuthentication(tenantId, resource, clientId);
                                }
                            }
                            // return a common tenant AAD backed VSO authentication object
                            return new VsoAadAuthentication(resource, clientId);
                        }
                    }
                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication();

                case CredentialType.Basic:
                default:
                    return new BasicAuthentication();

                case CredentialType.MicrosoftAccount:
                    // if the clientId and resource values exist, use them
                    if (!String.IsNullOrWhiteSpace(operationArguments.AuthorityClientId) && !String.IsNullOrWhiteSpace(operationArguments.AuthorityResource))
                    {
                        Guid clientId = Guid.Empty;
                        string resource = operationArguments.AuthorityResource;

                        if (Guid.TryParse(operationArguments.AuthorityClientId, out clientId))
                        {
                            // return a common tenant MSA backed VSO authentication object
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
    }
}
