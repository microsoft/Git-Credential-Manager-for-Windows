# Git Credential Manager for Windows 
[![GitHub Release](https://img.shields.io/github/release/microsoft/git-credential-manager-for-windows.svg?style=flat-square)](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases)
[![Build status](https://img.shields.io/appveyor/ci/whoisj/git-credential-manager-for-windows.svg?style=flat-square)](https://ci.appveyor.com/project/whoisj/git-credential-manager-for-windows/branch/master) 
[![Coverity Scan Build Status](https://img.shields.io/coverity/scan/11371.svg?style=flat-square)](https://scan.coverity.com/projects/git-credential-manager-for-windows)
[![GitHub Downloads](https://img.shields.io/github/downloads/Microsoft/Git-Credential-Manager-for-Windows/total.svg?style=flat-square)](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases)
[![@MicrosoftGit on Twitter](https://img.shields.io/twitter/follow/microsoftgit.svg?style=social&label=Follow%20%40microsoftgit)](https://twitter.com/microsoftgit)

The [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (GCM) provides secure Git credential storage for Windows. It's the successor to the [Windows Credential Store for Git](https://gitcredentialstore.codeplex.com/) (git-credential-winstore), which is no longer maintained. Compared to Git's built-in credential storage for Windows ([wincred](https://git-scm.com/book/en/v2/Git-Tools-Credential-Storage)), which provides single-factor authentication support working on any HTTP enabled Git repository, GCM provides multi-factor authentication support for [Visual Studio Team Services](https://www.visualstudio.com/), [Team Foundation Server](Docs/Faq.md#q-i-thought-microsoft-was-maintaining-this-why-does-the-gcm-not-work-as-expected-with-tfs), GitHub, and Bitbucket.

This project includes:

* Secure password storage in the Windows Credential Store.
* Multi-factor authentication support for Visual Studio Team Services.
* Two-factor authentication support for GitHub.
* Two-factor authentication support for Bitbucket.
* Personal Access Token generation and usage support for Visual Studio Team Services and GitHub.
* Non-interactive mode support for Visual Studio Team Services backed by Azure Directory..
* NTLM/Kerberos authentication for Team Foundation Server ([see notes](Docs/Faq.md#q-i-thought-microsoft-was-maintaining-this-why-does-the-gcm-not-work-as-expected-with-tfs)).
* Optional settings for [build agent optimization](Docs/Automation.md).

## Community

This is a community project so feel free to contribute ideas, submit bugs, fix bugs, or code new features. For detailed information on how the GCM works go to the [wiki](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/wiki/How-the-Git-Credential-Managers-works).

## Download and Install

To use the GCM, you can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases/latest). To install, double-click Setup.exe and follow the instructions presented.

When prompted to select your terminal emulator for Git Bash you should choose the Windows' default console window, or make sure GCM is [configured to use modal dialogs](Docs/Configuration.md#modalprompt). GCM cannot prompt you for credentials, at the console, in a MinTTY setup.

## How to use

You don't. It [magically](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues/31) works when credentials are needed. For example, when pushing to [Visual Studio Team Services](https://www.visualstudio.com), it automatically opens a window and initializes an oauth2 flow to get your token.

### Manual Installation

Note for users with special installation needs, you can still extract the `gcm-<version>.zip` file and run install.cmd from an administrator command prompt. This allows specification of the installation options explained below.

### Build and Install from Sources

To build and install the GCM yourself, clone the sources, open the solution file in Visual Studio, and build the solution. All necessary components will be copied from the build output locations into a `.\Deploy` folder at the root of the solution. From an elevated command prompt in the `.\Deploy` folder issue the following command `git-credential-manager install`. Additional information about [development and debugging](Docs/Development.md) are available in our documents area.

[Various options](Docs/Configuration.md) are available for uniquely configured systems, like automated build systems. For systems with a **non-standard placement of Git** use the `--path <git>` parameter to supply where Git is located and thus where the GCM should be deployed to. For systems looking to **avoid checking for the Microsoft .NET Framework** and other similar prerequisites use the `--force` option. For systems looking for **silent installation without any prompts**, use the `--passive` option.

### Additional Resources

* [Credential Manager Usage](Docs/CredentialManager.md)
* [Askpass Usage](Docs/Askpass.md)
* [Configuration Options](Docs/Configuration.md)
* [Build Agent and Automation Support](Docs/Automation.md)
* [Bitbucket Specific Details](Docs/Bitbucket.md)
* [Frequently Asked Questions](Docs/Faq.md)
* [Development and Debugging](Docs/Development.md)

## Contribute

There are many ways to contribute.

* [Submit bugs](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) and help us verify fixes as they are checked in.
* Review [code changes](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/pulls).
* Contribute bug fixes and features.

### Code Contributions

For code contributions, you will need to complete a Contributor License Agreement (CLA). Briefly, this agreement testifies that you grant us permission to use the submitted change according to the terms of the project's license, and that the work being submitted is under the appropriate copyright.

Please submit a Contributor License Agreement (CLA) before submitting a pull request. You may visit <https://cla.microsoft.com> to sign digitally. Alternatively, download the agreement [Microsoft Contribution License Agreement.pdf](https://cla.microsoft.com/cladoc/microsoft-contribution-license-agreement.pdf), sign, scan, and email it back to <cla@microsoft.com>. Be sure to include your GitHub user name along with the agreement. Once we have received the signed CLA, we'll review the request.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact <opencode@microsoft.com> with any additional questions or comments.

## License

This project uses the [MIT License](LICENSE.txt).
