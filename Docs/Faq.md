# FAQ

If you have an issue using GCM or Askpass, please review the following FAQ and check our [issue history](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) or our [Twitter feed](https://twitter.com/microsoftgit) before opening up an item on our issues page on GitHub.

## Q: Why am always prompted for my username and password?

Most likely, your environment is not configured correctly.
You can verify that your environment is configured correctly by running `git config --list` and looking for `credential.helper=manager`.
If you do not see the line, then you know that Git does not know about the Git Credential Manager.
You can configure Git to use the Credential Manager by running `git config credential.helper manager`.

## Q: Why does my GUI freeze when I push, pull, or fetch?

Most likely reason is that your GUI “shells out” to git.exe to perform Git operations.
When it does so, it cannot respond to the command line prompts for username and password like a real user can.
To avoid being asked for your credentials on the command line, and instead be asked via a modal dialog you’ll need to [configure the Git Credential Manager](Configuration.md#modalprompt).

1. Decide if you want this to be a global setting (all of your repositories) or a local setting (just one repository).
2. Start your favorite shell (CMD, PowerShell, bash, etc.).
3. Update your settings, so that Git Credential Manager knows to display a dialog and not prompt at the command line:
   * If you’ve decided this is a global setting run `git config --global credential.modalprompt true`.
   * If you’ve decided this a per repository setting, `cd` to your repo and in that repo run `git config credential.modalprompt true`.

## Q: Why am I not seeing my SSH keys being saved?

The Git Credential Manager supports caching of SSH key password through [git-askpass](Askpass.md).
Unfortunately, OpenSSH will only interact with an askpass helper if there no TTY detected (no console available).
This mean, in general and for the vast majority of users, the GCM does not help with SSH passwords or certificates.

Fortunately, the [Posh-Git](https://github.com/dahlbyk/posh-git) support automatic startup of 'ssh-agent.exe' which does assist with SSH certificate password caching.

## Q: Is there a NuGet Package available?

Yes there is: <https://www.nuget.org/packages/Microsoft.Alm.Authentication>.
It supports the core authentication library and the VSTS specific components.
If you're looking to extend the GCM, or need a way to authenticate with VSTS but cannot leverage the GCM directly, then it is likely what you're after.

## Q: Why doesn’t Git Credential Manager work on Windows XP, Mac OS, or Linux?

The Git Credential Manager does not work on Windows XP, Max OS, or Linux because we had to scope our work and we decided to support the same operating systems that Visual Studio support.
Why Visual Studio?
Well, because it is our favorite IDE and in order to support [Azure DevOps](https://www.visualstudio.com/en-us/products/visual-studio-team-services-vs.aspx) we had to use the [Azure Directory Authentication Libraries](https://github.com/AzureAD) which only have multi-factor interactive logon support in their .NET libraries.
Using .NET means using Visual Studio (which we love anyways) and using Visual Studio means Windows 7 or newer.

## Q: Will there ever be support for Windows XP, Mac OS, or Linux?

We can safely say that we have no interest in supporting Windows XP.
Even [Microsoft has ended support for Windows XP](https://windows.microsoft.com/en-us/windows/end-support-help).
Support for Mac OS and Linux are handled by [Microsoft Git Credential Manager for Mac and Linux](https://github.com/Microsoft/Git-Credential-Manager-for-Mac-and-Linux).

## Q: Why is my distribution/version of Git not supported? Why is Git for Windows favored?

The Credential Manager deployment helpers (`install.cmd` and `GCMW-{version}.exe`) are focused on support for [Git for Windows](https://github.com/git-for-windows) because Git for Windows conforms to the expected/normal behavior of software on Windows.
It is easy to detect, has predictable installation location, etc. This makes supporting it easier and more reliable.

That said, so long as your favorite version of Git supports Git’s git-credential flow, it is supported by the Git Credential Manager for Windows.
Setup will have to be manual, and if you find a way to script it we would love to have you contribute that to our project.

1. Copy the contents of the `gcm-<version>.zip` to your Git’s /bin folder.
   This varies per distribution, but it is likely next to other git tools like `git-status.exe`.
2. Update your Git configuration by running `git config --global credential.helper manager`.

## Q: I thought Microsoft was maintaining this, why does the GCM not work as expected with TFS?

Team Foundation Server, when deployed on a corporate Active Directory, uses the [Microsoft Kerberos](https://msdn.microsoft.com/en-us/library/windows/desktop/aa378747(v=vs.85).aspx) protocol for authentication.
Git hasn't traditionally be able to "speak" the Kerberos protocol.
However, starting with [v1.14.0](https://github.com/git-for-windows/git/releases/tag/v2.14.0.windows.1) Git for Windows supports Microsoft [Secure Channel](https://msdn.microsoft.com/en-us/library/windows/desktop/aa380123(v=vs.85).aspx).
To enable Secure Channel support run `git config --global http.sslBackend=schannel` or selecting the 'Native Windows Secure Channel library' option during installation of Git for Windows.
Secure Channel provides better integration with Windows' networking and certificate management; enabling easier use of proxies and alternate authentication mechanisms previously unavailable to Git for Windows users.

Git needs to be convinced to "forward" credentials by supplying a blank credential set (username and password).
The GCM will attempt to detect the Team Foundation Server via the HTTP headers returned when an unauthenticated request is handled by the server.
If the server is configured to allow NTLM as a supported authentication protocol, the GCM will detect the setting and instruct Git to use NTLM instead of basic authentication.

Alternatively, you can configure the GCM to assume a host supports NTLM without checking.
To do so, update your Git configuration by running `git config --global credential.{my-tfs}.authority NTLM`, where `{my-tfs}` can be replaced by the host name of your TFS server; the port number is not required for GCM configuration but you will want it for the Git remote.

_Note:_ Previous versions of the GCM suggested using `git credential.{url}.integrated true`; while this configuration option continues to work, it has been deprecated in favor of specifying the correct authority.

Once updated, the new configuration tells the GCM to only negotiate via NTLM with the host and forward Windows credentials.
Most likely, this is **not** what you want.
Therefore, it strongly suggested that you restrict the configuration setting to the URL of your TFS Git host.

## Q: Why doesn't SourceTree use the credentials in the GCM?

You need to configure SourceTree to use the version of Git installed for the entire system.
By default, SourceTree uses a local copy of portable Git.

To fix this go to 'Tools > Options > Git' and click the 'Use System Git' button.
This works in v1.8.3.0 of SourceTree.

## Q: Why is Git not using the GCM in some of my repositories (but instead using SSH authentication)?

Check that you are using the HTTP(S) URL instead of the SSH URL for your repository.
You can do this by running `git remote show origin`.
The Fetch URL and Push URL should start with `https://` or `http://`.
If this is not the case, look for the HTTP(S) URL in the web interface of Azure DevOps, TFS, GitHub or Bitbucket, and then run `git remote set-url origin <url>`, where `<url>` is the HTTP(S) URL.

## Q: Why is git.exe failing to authenticate after linking/unlinking your Azure DevOps account from Azure Active Directory?

When the tenant backing the Azure DevOps account changes like when you [Connect VSTS account to Azure Active Directory (Azure AD)](https://docs.microsoft.com/en-us/vsts/accounts/connect-account-to-aad), the tenant cache needs to be cleared if you're using a GCM version prior to v1.15.0.
Clearing the tenant cache is as easy as deleting the *%LocalAppData%\GitCredentialManager\tenant.cache* file on each machine returning a login error like below.
The GCM will automatically recreate and populate the cache file as needed on subsequent login attempts.

Example:

```text
Error: cannot spawn askpass: No such file or directory
Error encountered while pushing to the remote repository: Git failed with a fatal error.
could not read Username for ‘https://******.********.***’: terminal prompts disabled
```

## Q: Why doesn’t the GCM work with ServicePointManager?

When you have `git config http.proxy` or `HTTPS_PROXY` configured to use [`ServicePointManager`](https://docs.microsoft.com/en-us/dotnet/api/system.net.servicepointmanager?view=netframework-4.7.1) proxy, and the URL doesn’t begin with `http://` the GCM will be unable to negotiate with the proxy. This is due the way the .NET Framework [`WebProxy`](https://docs.microsoft.com/en-us/dotnet/api/system.net.webproxy?view=netframework-4.7.1) and `ServicePointManager` interact. You can read "[ServicePointManager does not support proxies of https scheme](https://blogs.msdn.microsoft.com/jpsanders/2007/04/25/the-servicepointmanager-does-not-support-proxies-of-https-scheme-net-1-1-sp1/)" for additional information about this issue.

The work around is to use a proxy URL which starts with `http://`, or discontinue use of `ServicePointManager`.
