# Environment Variables

[Git Credential Manager](CredentialManager.md) and [Git Askpass](Askpass.md) work out of the box for most users.
Environment variables, to customize or tweak behavior, are available as needed.

The Git Credential Manager for Windows [GCM] can be configured using [environment variables](https://msdn.microsoft.com/en-us/library/windows/desktop/bb776899(v=vs.85).aspx). Environment variables take precedence over [configuration](Configuration.md) settings.

For the complete list of environment variables the GCM understands, see the list below.

## Configuration Setting Names

### GCM_AUTHORITY

Defines the type of authentication to be used.

Supports `Auto`, `Basic`, `AAD`, `MSA`, `GitHub`, `Bitbucket`, `Integrated`, and `NTLM`.

Use `AAD` or `MSA` if the host is 'visualstudio.com' Azure Domain or Live Account authentication, relatively.

Use `GitHub` if the host is 'github.com'.

Use `BitBucket` or `Atlassian` if the host is 'bitbucket.org'.

Use `Integrated` or `NTLM` if the host is a Team Foundation, or other NTLM authentication based, server.

Defaults to `Auto`.

See [credential.authority](Configuration.md#authority).

### GCM_CONFIG_NOLOCAL

Determines if the the GCM should ignore Git local configuration values.

Supports `true` or `false`. Defaults to `false`.

_No configuration equivalent._

### GCM_CONFIG_NOSYSTEM

Determines if the the GCM should ignore Git system configuration values.

Supports `true` or `false`. Defaults to `false`.

_No configuration equivalent._

### HTTP_PROXY / HTTPS_PROXY

Causes the proxy value to be considered when evaluating credential target information.
A proxy setting should established if use of a proxy is required to interact with Git remotes.

The value should the URL of the proxy server.

Defaults to not using a proxy server.

See [credential.httpProxy](Configuration.md#httpproxy).

### GCM_HTTP_USER_AGENT

Sets the reported [user-agent](https://en.wikipedia.org/wiki/User_agent) when GCM performs network operations.

Defaults to the GCM's user-agent.

_No configuration equivalent._

### GCM_INTERACTIVE

Specifies if user can be prompted for credentials or not.

Supports `Auto`, `Always`, or `Never`. Defaults to `Auto`.

See [credential.interactive](Configuration.md#interactive).

### GCM_MODAL_PROMPT

Forces authentication to use a modal dialog instead of asking for credentials at the command prompt.

Supports `true` or `false`. Defaults to `true`.

See [credential.modalPrompt](Configuration.md#modalprompt).

### GCM_NAMESPACE

Sets the namespace for stored credentials.

By default the GCM uses the 'git' namespace for all stored credentials, setting this configuration value allows for control of the namespace used globally, or per host.

See [credential.namespace](Configuration.md#namespace).

### GCM_PRESERVE

Prevents the deletion of credentials even when they are reported as invalid by Git.
Can lead to lockout situations once credentials expire and until those credentials are manually removed.

Supports `true` or `false`. Defaults to `false`.

See [credential.preserve](Configuration.md#preserve).

### GCM_HTTP_TIMEOUT

Sets the maximum time, in milliseconds, for a network request to wait before timing out. 
This allows changing the default for slow connections.

Supports an integer value. Defaults to 90,000 miliseconds.

See [credential.httpTimeout](Configuration.md#httpTimeout).

### GCM_TOKEN_DURATION

Sets a duration, in hours, limit for the validity of Personal Access Tokens requested from Azure DevOps.

If the value is greater than the maximum duration set for the account, the account value supersedes. The value cannot be less than a one hour (1).

Defaults to the account token duration. Honored when authority is set to `AAD` or `MSA`.

See [credential.tokenDuration](Configuration.md#tokenduration).

### GCM_TRACE

Enables trace logging of all activities.
Configuring Git and the GCM to trace to the same location is often desirable, and the GCM is compatible and cooperative with `GIT_TRACE`.

Example:

```text
SET GIT_TRACE=%UserProfile%\git.log
SET GCM_TRACE=%UserProfile%\git.log
```

If the value of `GCM_TRACE` is a full path to a file in an existing directory, logs are appended to the file.

IF the value of `GCM_TRACE` is `true`, logs are written standard error.

Defaults tracing being disabled.

_No configuration equivalent._

### GCM_VALIDATE

Causes validation of credentials before supplying them to Git.
Invalid credentials get a refresh attempt before failing.
Incurs minor network operation overhead.

Defaults to `true`. Ignored when authority set to `Basic`.

See [credential.validate](Configuration.md#validate).

### GCM_VSTS_SCOPE

Overrides GCM default scope request when generating a Personal Access Token from Azure DevOps.
The supported format is one or more [scope values](https://docs.microsoft.com/en-us/vsts/integrate/get-started/authentication/oauth#scopes) separated by whitespace, commas, semi-colons, or pipe characters (`' '`, `','`, `';'`, `'|'`).

Defaults to `vso.code_write|vso.packaging`; Honored when host is 'visualstudio.com'.

See [credential.vstsScope](Configuration.md#vstsscope).

### GCM_WRITELOG

Enables trace logging of all activities.
Logs are written to the local .git/ folder at the root of the repository.

__Note:__ This setting will not override the `GCM_TRACE` environment variable.

See [credential.writeLog](Configuration.md#writelog).
