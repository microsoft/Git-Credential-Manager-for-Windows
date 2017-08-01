# Configuration Options

GCM/Askpass work out of the box for most users. Configuration options are available to customize or tweak behavior(s).

The Git Credential Manager for Windows [GCM] can be configured using Git's configuration files, and follows all of the same rules Git does when consuming the files. Global configuration settings override system configuration settings, and local configuration settings override global settings; and because the configuration details exist within Git's configuration files you can use Git's `git config` utility to set, unset, and alter the setting values.

The GCM honors several levels of settings, in addition to the standard local \> global \> system tiering Git uses. Since the GCM is HTTPS based, it'll also honor URL specific settings. Regardless, all of the GCM's configuration settings begin with the term `credential`.

Regardless, the GCM will only be used by Git if the GCM is installed and the key/value pair `credential.helper manager` is present in Git's configuration.

For example:

> `credential.microsoft.visualstudio.com.namespace` is more specific than `credential.visualstudio.com.namespace`, which is more specific than `credential.namespace`.

In the examples above, the `credential.namespace` setting would affect any remote repository; the `credential.visualstudio.com.namespace` would affect any remote repository in the domain, and/or any subdomain (including `www.`) of, 'visualstudio.com'; where as the the `credential.microsoft.visualstudio.com.namespace` setting would only be applied to remote repositories hosted at 'microsoft.visualstudio.com'.

For the complete list of settings the GCM knows how to check for an apply, see the list below.

## Configuration Setting Names

### authority

Defines the type of authentication to be used.

Supports **Auto**, **Basic**, **AAD**, **MSA**, **GitHub**, **Integrated**, and **NTLM**.

Use **AAD** or **MSA** if the host is visualstudio.com Azure Domain or Live Account authentication, relatively.

Use **GitHub** if the host is github.com.

Use **BitBucket** or **Atlassian** if the host is bitbucket.org.

Use **Integrated** or **NTLM** if the host is a Team Foundation, or other NTLM authentication based, server.

Defaults to _Auto_.

    git config --global credential.microsoft.visualstudio.com.authority AAD

### httpProxy

Causes the proxy value to be considered when evaluating credential target information. A proxy setting should established if use of a proxy is required to interact with Git remotes.

The value should the URL of the proxy server.

    git config --global credential.github.com.httpProxy https://myproxy:8080

### interactive

 Specifies if user can be prompted for credentials or not.

 Supports Auto, Always, or Never. Defaults to Auto.

    git config --global credential.microsoft.visualstudio.com.interactive never

### modalPrompt

Forces authentication to use a modal dialog instead of asking for credentials at the command prompt.

Defaults to _true_.

    git config --global credential.modalPrompt true

### namespace

Sets the namespace for stored credentials.

By default the GCM uses the 'git' namespace for all stored credentials, setting this configuration value allows for control of the namespace used globally, or per host.

    git config --global credential.namespace name

### preserve

Prevents the deletion of credentials even when they are reported as invalid by Git. Can lead to lockout situations once credentials expire and until those credentials are manually removed.

Defaults to _false_.

    git config --global credential.visualstudio.com.preserve true

### tokenDuration

Sets a duration, in hours, limit for the validity of Personal Access Tokens requested from Visual Studio Team Services [VSTS].

If the value is greater than the maximum duration set for the account, the account value supercedes. The value cannot be less than a one hour (1).

Defaults to the account token duration. Honored by _AAD_ and _MSA_ authorities.

    git config --global credential.visualstudio.com.tokenDuration 24

### useHttpPath

Causes the path portion of the target URI to considered meaningful.

By default the path portion of the target URI is ignore, if this is set to true the path is considered meaningful and credentials will be store for each path.

Defaults to _false_.

    git config --global credential.bitbucket.com.useHttpPath true

### validate

Causes validation of credentials before supplying them to Git. Invalid credentials get a refresh attempt before failing. Incurs some minor overhead.

Defaults to _true_. Ignored by _Basic_ authority.

    git config --global credential.microsoft.visualstudio.com.validate false

### writelog

Enables trace logging of all activities. Logs are written to the local .git/ folder at the root of the repository.

__Note:__ This setting will not override the **GCM_TRACE** environment variable.

Defaults to _false_.

    git config --global credential.writelog true

## Sample Configuration

```ini
[credential "microsoft.visualstudio.com"]
    authority = AAD
    interactive = never
    preserve = true
    tokenDuration = 12
    validate = false
[credential "visualstudio.com"]
    authority = MSA
[credential]
    helper = manager
    writelog = true
```
