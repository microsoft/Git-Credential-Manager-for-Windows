# Configuration Options

[Git Credential Manager](CredentialManager.md) and [Git Askpass](Askpass.md) work out of the box for most users.
Configuration options are available to customize or tweak behavior(s).

The Git Credential Manager for Windows [GCM] can be configured using Git's configuration files, and follows all of the same rules Git does when consuming the files.
Global configuration settings override system configuration settings, and local configuration settings override global settings; and because the configuration details exist within Git's configuration files you can use Git's `git config` utility to set, unset, and alter the setting values.

The GCM honors several levels of settings, in addition to the standard local \> global \> system tiering Git uses.
Since the GCM is HTTPS based, it'll also honor URL specific settings.
Regardless, all of the GCM's configuration settings begin with the term `credential`.
Additionally, the GCM respects GCM specific [environment variables](Environment.md) as well.

Regardless, the GCM will only be used by Git if the GCM is installed and the key/value pair `credential.helper manager` is present in Git's configuration.

For example:

> `credential.microsoft.visualstudio.com.namespace` is more specific than `credential.visualstudio.com.namespace`, which is more specific than `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any remote repository; the `credential.visualstudio.com.namespace` would affect any remote repository in the domain, and/or any subdomain (including `www.`) of, 'visualstudio.com'; where as the the `credential.microsoft.visualstudio.com.namespace` setting would only be applied to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings the GCM understands, see the list below.

## Configuration Setting Names

### authority

Defines the type of authentication to be used.

Supports `Auto`, `Basic`, `AAD`, `MSA`, `GitHub`, `Bitbucket`, `Integrated`, and `NTLM`.

Use `AAD` or `MSA` if the host is 'visualstudio.com' Azure Domain or Live Account authentication, relatively.

Use `GitHub` if the host is 'github.com'.

Use `BitBucket` or `Atlassian` if the host is 'bitbucket.org'.

Use `Integrated` or `NTLM` if the host is a Team Foundation, or other NTLM authentication based, server.

Defaults to `Auto`.

```shell
git config --global credential.microsoft.visualstudio.com.authority AAD
```

See [GCM_AUTHORITY](Environment.md#gcm_authority)

### httpProxy

Causes the proxy value to be considered when evaluating credential target information.
A proxy setting should established if use of a proxy is required to interact with Git remotes.

The value should the URL of the proxy server.

Defaults to not using a proxy server.

```shell
git config --global credential.github.com.httpProxy https://myproxy:8080
```

See [HTTP_PROXY](Environment.md#http_proxy--https_proxy)

### interactive

Specifies if user can be prompted for credentials or not.

Supports `Auto`, `Always`, or `Never`. Defaults to `Auto`.

```shell
git config --global credential.microsoft.visualstudio.com.interactive never
```

See [GCM_INTERACTIVE](Environment.md#gcm_interactive)

### modalPrompt

Forces authentication to use a modal dialog instead of asking for credentials at the command prompt.

Supports `true` or `false`. Defaults to `true`.

```shell
git config --global credential.modalPrompt true
```

See [GCM_MODAL_PROMPT](Environment.md#gcm_modal_prompt)

### namespace

Sets the namespace for stored credentials.

By default the GCM uses the 'git' namespace for all stored credentials, setting this configuration value allows for control of the namespace used globally, or per host.

Supports any ASCII, alpha-numeric only value. Defaults to `git`.

```shell
git config --global credential.namespace name
```

See [GCM_NAMESPACE](Environment.md#gcm_namespace)

### preserve

Prevents the deletion of credentials even when they are reported as invalid by Git.
Can lead to lockout situations once credentials expire and until those credentials are manually removed.

Supports `true` or `false`. Defaults to `false`.

```shell
git config --global credential.visualstudio.com.preserve true
```

See [GCM_PRESERVE](Environment.md#gcm_preserve)

### httpTimeout

Sets the maximum time, in milliseconds, for a network request to wait before timing out. 
This allows changing the default for slow connections.

Supports an integer value. Defaults to 90,000 milliseconds.

```shell
git config --global credential.visualstudio.com.httpTimeout 100000
```

See [GCM_HTTP_TIMEOUT](Environment.md#gcm_http_timeout)

### tokenDuration

Sets a duration, in hours, limit for the validity of Personal Access Tokens requested from Azure DevOps.

If the value is greater than the maximum duration set for the account, the account value supersedes.
The value cannot be less than a one hour (1).

Defaults to the account token duration. Honored when authority is set to `AAD` or `MSA`.

```shell
git config --global credential.visualstudio.com.tokenDuration 24
```

See [GCM_TOKEN_DURATION](Environment.md#gcm_token_duration)

### useHttpPath

Instructs Git to supply the path portion of the remote URL to credential helpers.
When path is supplied, the GCM will use the host-name + path as the key when reading and/or writing credentials.

_Note:_ This option changes the behavior of Git.

Supports `true` or `false`. Defaults to `false`.

```shell
git config --global credential.bitbucket.com.useHttpPath true
```

### username

Instructs Git to provide user-info to credential helpers.
When user-info is supplied, the GCM will use the user-info + host-name as the key when reading and/or writing credentials.
See [RFC: URI Syntax, User Information](https://tools.ietf.org/html/rfc3986#section-3.2.1) for more details.

_Note:_ This option changes the behavior of Git.

Supports any URI legal user-info. Defaults to not providing user-info.

```shell
git config --global credential.microsoft.visualstudio.com.username johndoe
```

### validate

Causes validation of credentials before supplying them to Git.
Invalid credentials get a refresh attempt before failing.
Incurs minor network operation overhead.

Supports `true` or `false`. Defaults to `true`. Ignored when authority is set to `Basic`.

```shell
git config --global credential.microsoft.visualstudio.com.validate false
```

See [GCM_VALIDATE](Environment.md#gcm_validate)

### vstsScope

Overrides GCM default scope request when generating a Personal Access Token from Azure DevOps.
The supported format is one or more [scope values](https://docs.microsoft.com/en-us/vsts/integrate/get-started/authentication/oauth#scopes) separated by whitespace, commas, semi-colons, or pipe `'|'` characters.

Defaults to `vso.code_write|vso.packaging`; Honored when host is 'dev.azure.com'.

```shell
git config --global credential.microsoft.visualstudio.com.vstsScope vso.code_write|vso.packaging_write|vso.test_write
```

See [GCM_VSTS_SCOPE](Environment.md#gcm_vsts_scope)

### writeLog

Enables trace logging of all activities.
Logs are written to the local `.git/` folder at the root of the repository.

__Note:__ This setting will not override the `GCM_TRACE` environment variable.

Supports `true` or `false`. Defaults to `false`.

```shell
git config --global credential.writeLog true
```

See [GCM_WRITELOG](Environment.md#gcm_writelog)

## Sample Configuration

```ini
[credential "microsoft.visualstudio.com"]
    authority = AAD
    interactive = never
    preserve = true
    tokenDuration = 24
    validate = false
[credential "visualstudio.com"]
    authority = MSA
[credential]
    helper = manager
    writelog = true
```
