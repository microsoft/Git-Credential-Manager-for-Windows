Git Credential Manager for Windows
==================================

The [Git Credential Manager for Windows (GCM)](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows>) provides secure Git credential storage for Windows. It's the successor to the [Windows Credential Store for Git (git-credential-winstore)](<https://gitcredentialstore.codeplex.com/>), which is no longer maintained. Compared to Git's built-in credential storage for Windows ([wincred](<https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage>)), which provides single-factor authentication support working on any HTTP enabled Git repository, GCM provides multi-factor authentication support for Visual Studio Team Services and GitHub.

This project includes:

 * Secure password storage in the Windows Credential Store
 * Multi-factor authentication support for Visual Studio Team Services
 * Two-factor authentication support for GitHub
 * Personal Access Token generation and usage support for Visual Studio Team Services and GitHub
 * Non-interactive mode support for Visual Studio Team Services backed by Azure Directory
 * Optional settings for build agent optimization

This is a community project so feel free to contribute ideas, submit bugs, fix bugs, or code new features. For detailed information on how the GCM works go to the [wiki](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/wiki/How-the-Git-Credential-Managers-works).

## Download and Install ##

To use the GCM, you can download the [latest installer](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases/latest>). To install, double-click Setup.exe and follow the instructions presented.

When prompted to select your terminal emulator for Git Bash you should choose the Windows' default console window. GCM cannot prompt you for credentials in a MinTTY setup.

## How to use ##

You don't. It [magically](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues/31>) works when credentials are needed. For example, when pushing to [Visual Studio Team Services](http://www.visualstudio.com), it automatically opens a window and initializes an oauth2 flow to get your token.

### Manual Installation

Note for users with special installation needs, you can still extract the `gcm-<version>.zip` file and run install.cmd from an administrator command prompt. This allows specification of the installation options explained below.

### Build and Install from Sources

To build and install the GCM yourself, clone the sources, open the solution file in Visual Studio, and build the solution. All necessary components will be copied from the build output locations into a `.\Deploy` folder at the root of the solution. From an elevated command prompt in the `.\Deploy` folder issue the following command `git-credential-manager install`.

Various options are available for uniquely configured systems, like automated build systems. For systems with a **non-standard placement of Git** use the `--path <git>` parameter to supply where Git is located and thus where the GCM should be deployed to. For systems looking to **avoid checking for the Microsoft .NET Framework** and other similar prerequisites use the `--force` option. For systems looking for **silent installation without any prompts**, use the `--passive` option.

## FAQ ##

Frequently asked questions collected from our [issues page](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues>), our [twitter feed](<https://twitter.com/microsoftgit>), and other sources. Please look through this list of questions-and-answers before posting a new issue on a topic.

### Q: Why am always prompted for my username and password?

Most likely, your environment is not configured correctly. You can verify that your environment is configured correctly by running `git config --list` and looking for `credential.helper=manager`. If you do not see the line, then you know that Git does not know about the Git Credential Manager. You can configure Git to use the Credential Manager by running `git config credential.helper manager`.

### Q: Why does my GUI freeze when I push, pull, or fetch?

Most likely reason is that your GUI “shells out” to git.exe to perform Git operations. When it does so, it cannot respond to the command line prompts for username and password like a real user can. To avoid being asked for your credentials on the command line, and instead be asked via a modal dialog you’ll need to configure the Credential Manager.

 1. Decide if you want this to be a global setting (all of your repos) or a local setting (just one repo).

 2. Start your favorite shell. (cmd, powershell, bash, etc.)

 3. Update your settings, so that Git Credential Manager knows to display a dialog and not prompt at the command line:

    * If you’ve decided this is a global setting run `git config --global credential.modalprompt true`.

    * If you’ve decided this a per repo setting, `cd` to your repo and in that repo run `git config credential.modalprompt true`.

### Q: Why am I not seeing my SSH keys being saved?

The Git Credential Manager does not *yet* support secure storage for SSH keys. It is something we hope to implement, but it has not been a priority. If you feel otherwise, please comment on the [SSH Key support issue](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues/25>) which is already open.

### Q: Why doesn’t Git Credential Manager work on Windows XP, Mac OS, or Linux?

The Git Credential Manager does not work on Windows XP, Max OS, or Linux because we had to scope our work and we decided to support the same operating systems that Visual Studio support. Why Visual Studio? Well, because it is our favorite IDE and in order to support [Visual Studio Team Services](<https://www.visualstudio.com/en-us/products/visual-studio-team-services-vs.aspx>) we had to use the [Azure Directory Authentication Libraries](<https://github.com/AzureAD>) which only have multi-factor interactive logon support in their .NET libraries. Using .NET means using Visual Studio (which we love anyways) and using Visual Studio means Windows 7 or newer.

### Q: Will there ever be support for Windows XP, Mac OS, or Linux?

We can safely say that we have no interest in supporting Windows XP. Even [Microsoft has ended support for Windows XP](<http://windows.microsoft.com/en-us/windows/end-support-help>). Support for Mac OS and Linux are very much something that we’re interesting in. We’re looking at options with [.NET Foundation cross-platform support](<https://github.com/dotnet>) and [ADAL Node.js support](<https://github.com/AzureAD/azure-activedirectory-library-for-nodejs>). Neither option is ready for a security application, and we’re not ready to port yet -- let’s at least get the ‘for Windows’ version to version 1.0 first!

### Q: Why is my distribution/version of Git not supported? Why is Git for Windows favored?

The Credential Manager deployment helpers (`install.cmd` and `Setup.exe`) are focused on support for [Git for Windows](<https://github.com/git-for-windows>) because Git for Windows conforms to the expected/normal behavior of software on Windows. It is easy to detect, has predictable installation location, etc. This makes supporting it cheaper and more reliable.

That said, so long as your favorite version of Git supports Git’s git-credential flow, it is supported by the Git Credential Manager for Windows. Setup will have to be manual, and if you find a way to script it we would love to have you contribute that to our project.

 1. Copy the contents of the `gcm-<version>.zip` to your Git’s /bin folder. This varies per distribution, but it is likely next to other git tools like `git-status.exe`.

 2. Update your Git configuration by running `git config --global credential.helper manager`.

### Q: I thought Microsoft was maintaing this, why does the GCM not work as expected with TFS?

Team Foundation Server, when deployed on a corporate Active Directory, uses the [Microsoft Kerberos](https://msdn.microsoft.com/en-us/library/windows/desktop/aa378747(v=vs.85).aspx) protocol for authentication. Git doesn't "speak" the Kerberos protocol.

Git can be convinced to "forward" domain credentials by supplying a blank credentials (username and password). Since, by default, the GCM doesn't allow for a blank credentials, you will need to configure it to allow for them. To do so, update your Git configuration by running `git config --global credential.<url_to_TFS_here>.integrated true`.

Once the updated, the new configuration tells the GCM to only forward domain credentials. If you set `credential.integrated = true`, every domain will be assumed to support domain credentials. Most likely, this is **not** what you want. Therefore, it strongly suggested that you restric the configuration setting to the URL of your TFS Git host.

## Build agents ##

Build agents cannot manage modal dialogs, therefore we recommended the following configuration.

```
git config --global credential.interactive never
```

Build agents often need to minimize the amount of network traffic they generate.

To avoid Microsoft Account vs. Azure Active Directory look-up against a Visual Studio Team Services account use:

```
git config --global credential.authority Azure
```

To avoid unnecessary service account credential validation use:

```
git config --global credential.validate false
```

## Contribute ##

There are many ways to contribue.

 * [Submit bugs](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues>) and help us verify fixes as they are checked in.

 * Review [code changes](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/pulls>).

 * Contribute bug fixes and features.

For code contributions, you will need to complete a Contributor License Agreement (CLA). Briefly, this agreement testifies that you grant us permission to use the submitted change according to the terms of the project's license, and that the work being submitted is under the appropriate copyright.

Please submit a Contributor License Agreement (CLA) before submitting a pull request. You may visit https://cla.microsoft.com to sign digitally. Alternatively, download the agreement [Microsoft Contribution License Agreement.pdf](<https://cla.microsoft.com/cladoc/microsoft-contribution-license-agreement.pdf>), sign, scan, and email it back to <cla@microsoft.com>. Be sure to include your github user name along with the agreement. Once we have received the signed CLA, we'll review the request.

## Debugging ##

To enable logging, use the following:

```
git config --global credential.writelog true
```

Log files will be written to the repo's local `.git/` folder.

## License ##

This project uses the [MIT License](<https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/LICENSE.txt>).
