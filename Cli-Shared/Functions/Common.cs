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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Git = Microsoft.Alm.Authentication.Git;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    internal static class CommonFunctions
    {
        public const string TokenScopeSeparatorCharacters = ",; ";

        public static async Task<BaseAuthentication> CreateAuthentication(Program program, OperationArguments operationArguments)
        {
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));
            if (operationArguments.TargetUri is null)
            {
                var innerException = new NullReferenceException($"`{operationArguments.TargetUri}` cannot be null.");
                throw new ArgumentException(innerException.Message, nameof(operationArguments), innerException);
            }

            var trace = program.Context.Trace;
            var secretsNamespace = operationArguments.CustomNamespace ?? Program.SecretsNamespace;
            BaseAuthentication authority = null;

            var basicCredentialCallback = (operationArguments.UseModalUi)
                    ? new AcquireCredentialsDelegate(program.ModalPromptForCredentials)
                    : new AcquireCredentialsDelegate(program.BasicCredentialPrompt);

            var bitbucketPrompts = new Bitbucket.AuthenticationPrompts(program.Context);

            var bitbucketCredentialCallback = (operationArguments.UseModalUi)
                    ? bitbucketPrompts.CredentialModalPrompt
                    : new Bitbucket.Authentication.AcquireCredentialsDelegate(program.BitbucketCredentialPrompt);

            var bitbucketOauthCallback = (operationArguments.UseModalUi)
                    ? bitbucketPrompts.AuthenticationOAuthModalPrompt
                    : new Bitbucket.Authentication.AcquireAuthenticationOAuthDelegate(program.BitbucketOAuthPrompt);

            var githubPrompts = new Github.AuthenticationPrompts(program.Context);

            var githubCredentialCallback = (operationArguments.UseModalUi)
                    ? new Github.Authentication.AcquireCredentialsDelegate(githubPrompts.CredentialModalPrompt)
                    : new Github.Authentication.AcquireCredentialsDelegate(program.GitHubCredentialPrompt);

            var githubAuthcodeCallback = (operationArguments.UseModalUi)
                    ? new Github.Authentication.AcquireAuthenticationCodeDelegate(githubPrompts.AuthenticationCodeModalPrompt)
                    : new Github.Authentication.AcquireAuthenticationCodeDelegate(program.GitHubAuthCodePrompt);

            NtlmSupport basicNtlmSupport = NtlmSupport.Auto;

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    trace.WriteLine($"detecting authority type for '{operationArguments.TargetUri}'.");

                    // Detect the authority.
                    authority = await BaseVstsAuthentication.GetAuthentication(program.Context,
                                                                               operationArguments.TargetUri,
                                                                               Program.VstsCredentialScope,
                                                                               new SecretStore(program.Context, secretsNamespace, BaseVstsAuthentication.UriNameConversion))
                             ?? Github.Authentication.GetAuthentication(program.Context, 
                                                                        operationArguments.TargetUri,
                                                                        Program.GitHubCredentialScope,
                                                                        new SecretStore(program.Context, secretsNamespace, Secret.UriToName),
                                                                        githubCredentialCallback,
                                                                        githubAuthcodeCallback,
                                                                        null)
                            ?? Bitbucket.Authentication.GetAuthentication(program.Context,
                                                                          operationArguments.TargetUri,
                                                                          new SecretStore(program.Context, secretsNamespace, Secret.UriToIdentityUrl),
                                                                          bitbucketCredentialCallback,
                                                                          bitbucketOauthCallback);

                    if (authority != null)
                    {
                        // Set the authority type based on the returned value.
                        if (authority is VstsMsaAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.MicrosoftAccount;
                            goto case AuthorityType.MicrosoftAccount;
                        }
                        else if (authority is VstsAadAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.AzureDirectory;
                            goto case AuthorityType.AzureDirectory;
                        }
                        else if (authority is Github.Authentication)
                        {
                            operationArguments.Authority = AuthorityType.GitHub;
                            goto case AuthorityType.GitHub;
                        }
                        else if (authority is Bitbucket.Authentication)
                        {
                            operationArguments.Authority = AuthorityType.Bitbucket;
                            goto case AuthorityType.Bitbucket;
                        }
                    }
                    goto default;

                case AuthorityType.AzureDirectory:
                    trace.WriteLine($"authority for '{operationArguments.TargetUri}' is Azure Directory.");

                    Guid tenantId = Guid.Empty;

                    // Get the identity of the tenant.
                    var result = await BaseVstsAuthentication.DetectAuthority(program.Context, operationArguments.TargetUri);

                    if (result.HasValue)
                    {
                        tenantId = result.Value;
                    }

                    // Return the allocated authority or a generic AAD backed VSTS authentication object.
                    return authority ?? new VstsAadAuthentication(program.Context,
                                                                  tenantId,
                                                                  operationArguments.VstsTokenScope,
                                                                  new SecretStore(program.Context, secretsNamespace, VstsAadAuthentication.UriNameConversion));

                case AuthorityType.Basic:
                    // Enforce basic authentication only.
                    basicNtlmSupport = NtlmSupport.Never;
                    goto default;

                case AuthorityType.GitHub:
                    trace.WriteLine($"authority for '{operationArguments.TargetUri}' is GitHub.");

                    // Return a GitHub authentication object.
                    return authority ?? new Github.Authentication(program.Context,
                                                                  operationArguments.TargetUri,
                                                                  Program.GitHubCredentialScope,
                                                                  new SecretStore(program.Context, secretsNamespace, Secret.UriToName),
                                                                  githubCredentialCallback,
                                                                  githubAuthcodeCallback,
                                                                  null);

                case AuthorityType.Bitbucket:
                    trace.WriteLine($"authority for '{operationArguments.TargetUri}'  is Bitbucket");

                    // Return a Bitbucket authentication object.
                    return authority ?? new Bitbucket.Authentication(program.Context,
                                                                     new SecretStore(program.Context, secretsNamespace, Secret.UriToIdentityUrl),
                                                                     bitbucketCredentialCallback,
                                                                     bitbucketOauthCallback);

                case AuthorityType.MicrosoftAccount:
                    trace.WriteLine($"authority for '{operationArguments.TargetUri}' is Microsoft Live.");

                    // Return the allocated authority or a generic MSA backed VSTS authentication object.
                    return authority ?? new VstsMsaAuthentication(program.Context,
                                                                  operationArguments.VstsTokenScope,
                                                                  new SecretStore(program.Context, secretsNamespace, VstsMsaAuthentication.UriNameConversion));

                case AuthorityType.Ntlm:
                    // Enforce NTLM authentication only.
                    basicNtlmSupport = NtlmSupport.Always;
                    goto default;

                default:
                    trace.WriteLine($"authority for '{operationArguments.TargetUri}' is basic with NTLM={basicNtlmSupport}.");

                    // Return a generic username + password authentication object.
                    return authority ?? new BasicAuthentication(program.Context, 
                                                                new SecretStore(program.Context, secretsNamespace, Secret.UriToIdentityUrl),
                                                                basicNtlmSupport,
                                                                basicCredentialCallback,
                                                                null);
            }
        }

        public static async Task<bool> DeleteCredentials(Program program, OperationArguments operationArguments)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));

            var trace = program.Context.Trace;
            BaseAuthentication authentication = await program.CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    trace.WriteLine($"deleting basic credentials for '{operationArguments.TargetUri}'.");
                    return await authentication.DeleteCredentials(operationArguments.TargetUri);

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    trace.WriteLine($"deleting VSTS credentials for '{operationArguments.TargetUri}'.");
                    var vstsAuth = authentication as BaseVstsAuthentication;
                    return await vstsAuth.DeleteCredentials(operationArguments.TargetUri);

                case AuthorityType.GitHub:
                    trace.WriteLine($"deleting GitHub credentials for '{operationArguments.TargetUri}'.");
                    var ghAuth = authentication as Github.Authentication;
                    return await ghAuth.DeleteCredentials(operationArguments.TargetUri);

                case AuthorityType.Bitbucket:
                    trace.WriteLine($"deleting Bitbucket credentials for '{operationArguments.TargetUri}'.");
                    var bbAuth = authentication as Bitbucket.Authentication;
                    return await bbAuth.DeleteCredentials(operationArguments.TargetUri, operationArguments.Username);
            }
        }

        public static void DieException(Program program, Exception exception, string path, int line, string name)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));

            program.Context.Trace.WriteLine(exception.ToString(), path, line, name);
            program.LogEvent(exception.ToString(), EventLogEntryType.Error);

            string message;
            if (!string.IsNullOrWhiteSpace(exception.Message))
            {
                message = $"{exception.GetType().Name} encountered.\n   {exception.Message}";
            }
            else
            {
                message = $"{exception.GetType().Name} encountered.";
            }

            program.Die(message, path, line, name);
        }

        public static void DieMessage(Program program, string message, string path, int line, string name)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            message = $"fatal: {message}";

            program.Exit(-1, message, path, line, name);
        }

        public static void EnableTraceLogging(Program program, OperationArguments operationArguments)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));

            var trace = program.Context.Trace;
            var where = program.Context.Where;

            if (operationArguments.WriteLog)
            {
                trace.WriteLine("trace logging enabled.");

                string gitConfigPath;
                if (where.GitLocalConfig(out gitConfigPath))
                {
                    trace.WriteLine($"git local config found at '{gitConfigPath}'.");

                    string gitDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (program.FileSystem.DirectoryExists(gitDirPath))
                    {
                        program.EnableTraceLogging(operationArguments, gitDirPath);
                    }
                }
                else if (where.GitGlobalConfig(out gitConfigPath))
                {
                    trace.WriteLine($"git global config found at '{gitConfigPath}'.");

                    string homeDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (program.FileSystem.DirectoryExists(homeDirPath))
                    {
                        program.EnableTraceLogging(operationArguments, homeDirPath);
                    }
                }
            }
#if DEBUG
            trace.WriteLine($"GCM arguments:{Environment.NewLine}{operationArguments}");
#endif
        }

        public static void EnableTraceLoggingFile(Program program, OperationArguments operationArguments, string logFilePath)
        {
            const int LogFileMaxLength = 8 * 1024 * 1024; // 8 MB

            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));
            if (logFilePath is null)
                throw new ArgumentNullException(nameof(logFilePath));

            var fs = program.FileSystem;
            var trace = program.Context.Trace;
            string logFileName = Path.Combine(logFilePath, Path.ChangeExtension(Program.ConfigPrefix, ".log"));

            var logFileInfo = new FileInfo(logFileName);
            if (logFileInfo.Exists && logFileInfo.Length > LogFileMaxLength)
            {
                for (int i = 1; i < int.MaxValue; i++)
                {
                    string moveName = string.Format("{0}{1:000}.log", Program.ConfigPrefix, i);
                    string movePath = Path.Combine(logFilePath, moveName);

                    if (!fs.FileExists(movePath))
                    {
                        logFileInfo.MoveTo(movePath);
                        break;
                    }
                }
            }

            trace.WriteLine($"trace log destination is '{logFilePath}'.");

            using (var fileStream = fs.FileOpen(logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                var listener = new StreamWriter(fileStream, Encoding.UTF8);
                trace.AddListener(listener);

                // write a small header to help with identifying new log entries
                listener.Write('\n');
                listener.Write($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} Microsoft {program.Title} version {program.Version.ToString(3)}\n");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "Microsoft.Alm.Cli.CommonFunctions.#LoadOperationArguments(Microsoft.Alm.Cli.Program,Microsoft.Alm.Cli.OperationArguments)")]
        public static async Task LoadOperationArguments(Program program, OperationArguments operationArguments)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));

            if (operationArguments.TargetUri == null)
            {
                program.Die("No host information, unable to continue.");
            }

            var trace = program.Context.Trace;

            string value;
            bool? yesno;

            if (program.TryReadBoolean(operationArguments, KeyType.ConfigNoLocal, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.ConfigNoLocal)} = '{yesno}'.");

                operationArguments.UseConfigLocal = yesno.Value;
            }

            if (program.TryReadBoolean(operationArguments, KeyType.ConfigNoSystem, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.ConfigNoSystem)} = '{yesno}'.");

                operationArguments.UseConfigSystem = yesno.Value;
            }

            // Load/re-load the Git configuration after setting the use local/system config values.
            await operationArguments.LoadConfiguration();

            // If a user-agent has been specified in the environment, set it globally.
            if (program.TryReadString(operationArguments, KeyType.HttpUserAgent, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.HttpUserAgent)} = '{value}'.");

                Global.UserAgent = value;
            }

            // Look for authority settings.
            if (program.TryReadString(operationArguments, KeyType.Authority, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Authority)} = '{value}'.");

                if (Program.ConfigKeyComparer.Equals(value, "MSA")
                    || Program.ConfigKeyComparer.Equals(value, "Microsoft")
                    || Program.ConfigKeyComparer.Equals(value, "MicrosoftAccount")
                    || Program.ConfigKeyComparer.Equals(value, "Live")
                    || Program.ConfigKeyComparer.Equals(value, "LiveConnect")
                    || Program.ConfigKeyComparer.Equals(value, "LiveID"))
                {
                    operationArguments.Authority = AuthorityType.MicrosoftAccount;
                }
                else if (Program.ConfigKeyComparer.Equals(value, "AAD")
                         || Program.ConfigKeyComparer.Equals(value, "Azure")
                         || Program.ConfigKeyComparer.Equals(value, "AzureDirectory"))
                {
                    operationArguments.Authority = AuthorityType.AzureDirectory;
                }
                else if (Program.ConfigKeyComparer.Equals(value, "Integrated")
                         || Program.ConfigKeyComparer.Equals(value, "Windows")
                         || Program.ConfigKeyComparer.Equals(value, "TFS")
                         || Program.ConfigKeyComparer.Equals(value, "Kerberos")
                         || Program.ConfigKeyComparer.Equals(value, "NTLM")
                         || Program.ConfigKeyComparer.Equals(value, "SSO"))
                {
                    operationArguments.Authority = AuthorityType.Ntlm;
                }
                else if (Program.ConfigKeyComparer.Equals(value, "GitHub"))
                {
                    operationArguments.Authority = AuthorityType.GitHub;
                }
                else if (Program.ConfigKeyComparer.Equals(value, "Atlassian")
                    || Program.ConfigKeyComparer.Equals(value, "Bitbucket"))
                {
                    operationArguments.Authority = AuthorityType.Bitbucket;
                }
                else
                {
                    operationArguments.Authority = AuthorityType.Basic;
                }
            }

            // Look for interactivity config settings.
            if (program.TryReadString(operationArguments, KeyType.Interactive, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Interactive)} = '{value}'.");

                if (Program.ConfigKeyComparer.Equals(value, "always")
                    || Program.ConfigKeyComparer.Equals(value, "true")
                    || Program.ConfigKeyComparer.Equals(value, "force"))
                {
                    operationArguments.Interactivity = Interactivity.Always;
                }
                else if (Program.ConfigKeyComparer.Equals(value, "never")
                         || Program.ConfigKeyComparer.Equals(value, "false"))
                {
                    operationArguments.Interactivity = Interactivity.Never;
                }
            }

            // Look for credential validation config settings.
            if (program.TryReadBoolean(operationArguments, KeyType.Validate, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Validate)} = '{yesno}'.");

                operationArguments.ValidateCredentials = yesno.Value;
            }

            // Look for write log config settings.
            if (program.TryReadBoolean(operationArguments, KeyType.Writelog, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Writelog)} = '{yesno}'.");

                operationArguments.WriteLog = yesno.Value;
            }

            // Look for modal prompt config settings.
            if (program.TryReadBoolean(operationArguments, KeyType.ModalPrompt, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.ModalPrompt)} = '{yesno}'.");

                operationArguments.UseModalUi = yesno.Value;
            }

            // Look for credential preservation config settings.
            if (program.TryReadBoolean(operationArguments, KeyType.PreserveCredentials, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.PreserveCredentials)} = '{yesno}'.");

                operationArguments.PreserveCredentials = yesno.Value;
            }
            else if (operationArguments.EnvironmentVariables.TryGetValue("GCM_PRESERVE_CREDS", out value))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(value, "true")
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "yes")
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "1")
                    || StringComparer.OrdinalIgnoreCase.Equals(value, "on"))
                {
                    trace.WriteLine($"GCM_PRESERVE_CREDS = '{yesno}'.");

                    operationArguments.PreserveCredentials = true;

                    trace.WriteLine($"WARNING: the 'GCM_PRESERVE_CREDS' variable has been deprecated, use '{ program.KeyTypeName(KeyType.PreserveCredentials) }' instead.");
                }
            }

            // Look for HTTP path usage config settings.
            if (program.TryReadBoolean(operationArguments, KeyType.HttpPath, out yesno))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.HttpPath)} = '{value}'.");

                operationArguments.UseHttpPath = yesno.Value;
            }

            // Look for HTTP proxy config settings.
            if ((operationArguments.TargetUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    && program.TryReadString(operationArguments, KeyType.HttpsProxy, out value))
                || program.TryReadString(operationArguments, KeyType.HttpProxy, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.HttpProxy)} = '{value}'.");

                operationArguments.SetProxy(value);
            }
            // Check environment variables just-in-case.
            else if ((operationArguments.EnvironmentVariables.TryGetValue("GCM_HTTP_PROXY", out value)
                    && !string.IsNullOrWhiteSpace(value)))
            {
                trace.WriteLine($"GCM_HTTP_PROXY = '{value}'.");

                var keyName = (operationArguments.TargetUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    ? "HTTPS_PROXY"
                    : "HTTP_PROXY";
                var warning = $"WARNING: the 'GCM_HTTP_PROXY' variable has been deprecated, use '{ keyName }' instead.";

                trace.WriteLine(warning);
                program.WriteLine(warning);

                operationArguments.SetProxy(value);
            }
            // Check the git-config http.proxy setting just-in-case.
            else
            {
                Git.Configuration.Entry entry;
                if (operationArguments.GitConfiguration.TryGetEntry("http", operationArguments.QueryUri, "proxy", out entry)
                    && !string.IsNullOrWhiteSpace(entry.Value))
                {
                    trace.WriteLine($"http.proxy = '{entry.Value}'.");

                    operationArguments.SetProxy(entry.Value);
                }
            }

            // Look for custom namespace config settings.
            if (program.TryReadString(operationArguments, KeyType.Namespace, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Namespace)} = '{value}'.");

                operationArguments.CustomNamespace = value;
            }

            // Look for custom token duration settings.
            if (program.TryReadString(operationArguments, KeyType.TokenDuration, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.TokenDuration)} = '{value}'.");

                int hours;
                if (int.TryParse(value, out hours))
                {
                    operationArguments.TokenDuration = TimeSpan.FromHours(hours);
                }
            }

            // Look for custom VSTS scope settings.
            if (program.TryReadString(operationArguments, KeyType.VstsScope, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.VstsScope)} = '{value}'.");

                VstsTokenScope vstsTokenScope = VstsTokenScope.None;

                var scopes = value.Split(TokenScopeSeparatorCharacters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < scopes.Length; i += 1)
                {
                    scopes[i] = scopes[i].Trim();

                    if (VstsTokenScope.Find(scopes[i], out VstsTokenScope scope))
                    {
                        vstsTokenScope = vstsTokenScope | scope;
                    }
                    else
                    {
                        trace.WriteLine($"Unknown VSTS Token scope: '{scopes[i]}'.");
                    }
                }

                operationArguments.VstsTokenScope = vstsTokenScope;
            }

            // Check for configuration supplied user-info.
            if (program.TryReadString(operationArguments, KeyType.Username, out value))
            {
                trace.WriteLine($"{program.KeyTypeName(KeyType.Username)} = '{value}'.");

                operationArguments.Username = value;
            }
        }

        public static void LogEvent(Program program, string message, EventLogEntryType eventType)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            /*** try-squelch due to UAC issues which require a proper installer to work around ***/

            program.Context.Trace.WriteLine(message);

            try
            {
                EventLog.WriteEntry(Program.EventSource, message, eventType);
            }
            catch { /* squelch */ }
        }

        public static void PrintArgs(Program program, string[] args)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            var builder = new StringBuilder();
            builder.Append(program.Name)
                   .Append(" (v")
                   .Append(program.Version.ToString(3))
                   .Append(")");

            for (int i = 0; i < args.Length; i += 1)
            {
                builder.Append(" '")
                       .Append(args[i])
                       .Append("'");

                if (i + 1 < args.Length)
                {
                    builder.Append(",");
                }
            }

            // Fake being part of the Main method for clarity.
            program.Context.Trace.WriteLine(builder.ToString(), memberName: "Main");
            builder = null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "Microsoft.Alm.Cli.CommonFunctions.#QueryCredentials(Microsoft.Alm.Cli.Program,Microsoft.Alm.Cli.OperationArguments)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope = "member", Target = "Microsoft.Alm.Cli.CommonFunctions.#QueryCredentials(Microsoft.Alm.Cli.Program,Microsoft.Alm.Cli.OperationArguments)")]
        public static async Task<Credential> QueryCredentials(Program program, OperationArguments operationArguments)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));
            if (operationArguments.TargetUri is null)
            {
                var innerException = new NullReferenceException($"{operationArguments.TargetUri} cannot be null.");
                throw new ArgumentException(innerException.Message, nameof(operationArguments), innerException);
            }

            var trace = program.Context.Trace;
            BaseAuthentication authentication = await program.CreateAuthentication(operationArguments);
            Credential credentials = null;

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    {
                        var basicAuth = authentication as BasicAuthentication;

                        // Attempt to get cached credentials or acquire credentials if interactivity is allowed.
                        if ((operationArguments.Interactivity != Interactivity.Always
                                && (credentials = await authentication.GetCredentials(operationArguments.TargetUri)) != null)
                            || (operationArguments.Interactivity != Interactivity.Never
                                && (credentials = await basicAuth.AcquireCredentials(operationArguments.TargetUri)) != null))
                        {
                            trace.WriteLine("credentials found.");
                            // No need to save the credentials explicitly, as Git will call back
                            // with a store command if the credentials are valid.
                        }
                        else
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                            program.LogEvent($"Failed to retrieve credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    {
                        var aadAuth = authentication as VstsAadAuthentication;
                        var patOptions = new PersonalAccessTokenOptions()
                        {
                            RequireCompactToken = true,
                            TokenDuration = operationArguments.TokenDuration,
                            TokenScope = null,
                        };

                        // Attempt to get cached credentials -> non-interactive logon -> interactive
                        // logon note that AAD "credentials" are always scoped access tokens.
                        if (((operationArguments.Interactivity != Interactivity.Always
                                && ((credentials = await aadAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                            || (operationArguments.Interactivity != Interactivity.Always
                                && ((credentials = await aadAuth.NoninteractiveLogon(operationArguments.TargetUri, patOptions)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && ((credentials = await aadAuth.InteractiveLogon(operationArguments.TargetUri, patOptions)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                            program.LogEvent($"Azure Directory credentials  for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                            program.LogEvent($"Failed to retrieve Azure Directory credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }
                    break;

                case AuthorityType.MicrosoftAccount:
                    {
                        var msaAuth = authentication as VstsMsaAuthentication;
                        var patOptions = new PersonalAccessTokenOptions()
                        {
                            RequireCompactToken = true,
                            TokenDuration = operationArguments.TokenDuration,
                            TokenScope = null,
                        };

                        // Attempt to get cached credentials -> interactive logon note that MSA
                        // "credentials" are always scoped access tokens.
                        if (((operationArguments.Interactivity != Interactivity.Always
                                && ((credentials = await msaAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && ((credentials = await msaAuth.InteractiveLogon(operationArguments.TargetUri, patOptions)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                            program.LogEvent($"Microsoft Live credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                            program.LogEvent($"Failed to retrieve Microsoft Live credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }
                    break;

                case AuthorityType.GitHub:
                    {
                        var ghAuth = authentication as Github.Authentication;

                        if ((operationArguments.Interactivity != Interactivity.Always
                                && ((credentials = await ghAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && ((credentials = await ghAuth.InteractiveLogon(operationArguments.TargetUri)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                            program.LogEvent($"GitHub credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                            program.LogEvent($"Failed to retrieve GitHub credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }
                    break;

                case AuthorityType.Bitbucket:
                    {
                        var bbcAuth = authentication as Bitbucket.Authentication;

                        if (((operationArguments.Interactivity != Interactivity.Always)
                                && ((credentials = await bbcAuth.GetCredentials(operationArguments.TargetUri, operationArguments.Username)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || ((credentials = await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.Username, credentials)) != null)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                && ((credentials = await bbcAuth.InteractiveLogon(operationArguments.TargetUri, operationArguments.Username)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || ((credentials = await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.Username, credentials)) != null))))
                        {
                            trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                            // Bitbucket relies on a username + secret, so make sure there is a
                            // username to return.
                            if (operationArguments.Username != null)
                            {
                                credentials = new Credential(operationArguments.Username, credentials.Password);
                            }
                            program.LogEvent($"Bitbucket credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            program.LogEvent($"Failed to retrieve Bitbucket credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }
                    break;

                case AuthorityType.Ntlm:
                    {
                        trace.WriteLine($"'{operationArguments.TargetUri}' is NTLM.");
                        credentials = BasicAuthentication.NtlmCredentials;
                    }
                    break;
            }

            if (credentials != null)
            {
                operationArguments.Credentials = credentials;
            }

            return credentials;
        }

        public static bool TryReadBoolean(Program program, OperationArguments operationArguments, KeyType key, out bool? value)
        {
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));

            if (program.ConfigurationKeys.TryGetValue(key, out string configKey)
                | program.EnvironmentKeys.TryGetValue(key, out string environKey))
            {
                var envars = operationArguments.EnvironmentVariables;

                // Look for an entry in the environment variables.
                string localVal = null;
                if (!string.IsNullOrWhiteSpace(environKey)
                    && envars.TryGetValue(environKey, out localVal))
                {
                    goto parse_localval;
                }

                var config = operationArguments.GitConfiguration;

                // Look for an entry in the git config.
                Git.Configuration.Entry entry;
                if (!string.IsNullOrWhiteSpace(configKey)
                    && config.TryGetEntry(Program.ConfigPrefix, operationArguments.QueryUri, configKey, out entry))
                {
                    localVal = entry.Value;
                    goto parse_localval;
                }

                // Parse the value into a bool.
                parse_localval:

                // An empty value is unset / should not be there, so treat it as if it isn't.
                if (string.IsNullOrWhiteSpace(localVal))
                {
                    value = null;
                    return false;
                }

                // Test `localValue` for a Git 'true' equivalent value.
                if (Program.ConfigValueComparer.Equals(localVal, "yes")
                    || Program.ConfigValueComparer.Equals(localVal, "true")
                    || Program.ConfigValueComparer.Equals(localVal, "1")
                    || Program.ConfigValueComparer.Equals(localVal, "on"))
                {
                    value = true;
                    return true;
                }

                // Test `localValue` for a Git 'false' equivalent value.
                if (Program.ConfigValueComparer.Equals(localVal, "no")
                    || Program.ConfigValueComparer.Equals(localVal, "false")
                    || Program.ConfigValueComparer.Equals(localVal, "0")
                    || Program.ConfigValueComparer.Equals(localVal, "off"))
                {
                    value = false;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public static bool TryReadString(Program program, OperationArguments operationArguments, KeyType key, out string value)
        {
            if (operationArguments is null)
                throw new ArgumentNullException(nameof(operationArguments));

            if (program.ConfigurationKeys.TryGetValue(key, out string configKey)
                | program.EnvironmentKeys.TryGetValue(key, out string environKey))
            {
                var envars = operationArguments.EnvironmentVariables;

                // Look for an entry in the environment variables.
                string localVal;
                if (!string.IsNullOrWhiteSpace(environKey)
                    && envars.TryGetValue(environKey, out localVal)
                    && !string.IsNullOrWhiteSpace(localVal))
                {
                    value = localVal;
                    return true;
                }

                Git.Configuration config = operationArguments.GitConfiguration;

                // Look for an entry in the git config.
                Git.Configuration.Entry entry;
                if (!string.IsNullOrWhiteSpace(configKey)
                    && config.TryGetEntry(Program.ConfigPrefix, operationArguments.QueryUri, configKey, out entry)
                    && !string.IsNullOrWhiteSpace(entry.Value))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
