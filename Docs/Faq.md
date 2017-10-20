# FAQ

If you have an issue using GCM or Askpass, please review the following FAQ and check our [issue history](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) or our [Twitter feed](https://twitter.com/microsoftgit) before opening up an item on our issues page on GitHub.

## Q: Why am always prompted for my username and password?

Most likely, your environment is not configured correctly. You can verify that your environment is configured correctly by running `git config --list` and looking for `credential.helper=manager`. If you do not see the line, then you know that Git does not know about the Git Credential Manager. You can configure Git to use the Credential Manager by running `git config credential.helper manager`.

## Q: Why does my GUI freeze when I push, pull, or fetch?

Most likely reason is that your GUI “shells out” to git.exe to perform Git operations. When it does so, it cannot respond to the command line prompts for username and password like a real user can. To avoid being asked for your credentials on the command line, and instead be asked via a modal dialog you’ll need to [configure the Credential Manager](Configuration.md#modalprompt).

1. Decide if you want this to be a global setting (all of your repositories) or a local setting (just one repository).
2. Start your favorite shell. (cmd, powershell, bash, etc.)
3. Update your settings, so that Git Credential Manager knows to display a dialog and not prompt at the command line:
  * If you’ve decided this is a global setting run `git config --global credential.modalprompt true`.
  * If you’ve decided this a per repository setting, `cd` to your repo and in that repo run `git config credential.modalprompt true`.

## Q: Why am I not seeing my SSH keys being saved?

The Git Credential Manager does not *yet* support secure storage for SSH keys. It is something we hope to implement, but it has not been a priority. If you feel otherwise, please comment on the [SSH Key support issue](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues/25) which is already open.

## Q: Is there a NuGet Package available?

Yes there is: <https://www.nuget.org/packages/Microsoft.Alm.Authentication>. It only supports the core authentication library, but if you're looking to extend the GCM then it is likely exactly what you're after.

## Q: Why doesn’t Git Credential Manager work on Windows XP, Mac OS, or Linux?

The Git Credential Manager does not work on Windows XP, Max OS, or Linux because we had to scope our work and we decided to support the same operating systems that Visual Studio support. Why Visual Studio? Well, because it is our favorite IDE and in order to support [Visual Studio Team Services](https://www.visualstudio.com/en-us/products/visual-studio-team-services-vs.aspx) we had to use the [Azure Directory Authentication Libraries](https://github.com/AzureAD) which only have multi-factor interactive logon support in their .NET libraries. Using .NET means using Visual Studio (which we love anyways) and using Visual Studio means Windows 7 or newer.

## Q: Will there ever be support for Windows XP, Mac OS, or Linux?

We can safely say that we have no interest in supporting Windows XP. Even [Microsoft has ended support for Windows XP](https://windows.microsoft.com/en-us/windows/end-support-help). Support for Mac OS and Linux are handled by [Microsoft Git Credential Manager for Mac and Linux](https://github.com/Microsoft/Git-Credential-Manager-for-Mac-and-Linux).

## Q: Why is my distribution/version of Git not supported? Why is Git for Windows favored?

The Credential Manager deployment helpers (`install.cmd` and `Setup.exe`) are focused on support for [Git for Windows](https://github.com/git-for-windows) because Git for Windows conforms to the expected/normal behavior of software on Windows. It is easy to detect, has predictable installation location, etc. This makes supporting it cheaper and more reliable.

That said, so long as your favorite version of Git supports Git’s git-credential flow, it is supported by the Git Credential Manager for Windows. Setup will have to be manual, and if you find a way to script it we would love to have you contribute that to our project.

1. Copy the contents of the `gcm-<version>.zip` to your Git’s /bin folder. This varies per distribution, but it is likely next to other git tools like `git-status.exe`.
2. Update your Git configuration by running `git config --global credential.helper manager`.

## Q: I thought Microsoft was maintaining this, why does the GCM not work as expected with TFS?

Team Foundation Server, when deployed on a corporate Active Directory, uses the [Microsoft Kerberos](https://msdn.microsoft.com/en-us/library/windows/desktop/aa378747(v=vs.85).aspx) protocol for authentication. Git doesn't "speak" the Kerberos protocol.

Git can be convinced to "forward" credentials by supplying a blank credentials (username and password). The GCM will attempt to detect the Team Foundation Server via the HTTP headers returned when an unauthenticated request is handled by the server. If the server is configured to allow NTLM as a supported authentication protocol, the GCM will detect the setting and instruct Git to use NTLM instead of basic authentication.

Alternatively, you can configure the GCM to assume a host supports NTLM without checking. To do so, update your Git configuration by running `git config --global credential.my-tfs.integrated true`, where `my-tfs` can be replaced by the name of your TFS server; the port number is not required for GCM configuration but you will want it for the Git remote.

Once updated, the new configuration tells the GCM to only forward domain credentials. If you set `credential.integrated true`, every domain will be assumed to support domain credentials. Most likely, this is **not** what you want. Therefore, it strongly suggested that you restrict the configuration setting to the URL of your TFS Git host.

## Q: Why doesn't SourceTree use the credentials in the GCM?

You need to configure SourceTree to use the version of Git installed for the entire system. By default, SourceTree uses a local copy of portable Git.

To fix this go to Tools → Options → Git and click the "Use System Git" button. This works in v1.8.3.0 of SourceTree.

## Q: Why is Git not using the GCM in some of my repositories (but instead using SSH authentication)?

Check that you are using the HTTP(S) URL instead of the SSH URL for your repository. You can do this by running `git remote show origin`. The Fetch URL and Push URL should start with `https://` or `http://`. If this is not the case, look for the HTTP(S) URL in the web interface of VSTS, TFS, GitHub or Bitbucket, and then run `git remote set-url origin <url>`, where `<url>` is the HTTP(S) URL.
