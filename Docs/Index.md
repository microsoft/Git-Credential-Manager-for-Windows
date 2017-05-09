# Git Credential Manager for Windows

The [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (GCM) provides secure Git credential storage for Windows. GCM provides multi-factor authentication support for [Visual Studio Team Services](https://www.visualstudio.com/), [Team Foundation Server](Faq.md#q-i-thought-microsoft-was-maintaining-this-why-does-the-gcm-not-work-as-expected-with-tfs), and [GitHub](https://github.com/).

This project includes:

* Secure password storage in the Windows Credential Store
* Multi-factor authentication support for Visual Studio Team Services
* Two-factor authentication support for GitHub
* Personal Access Token generation and usage support for Visual Studio Team Services and GitHub
* Non-interactive mode support for Visual Studio Team Services backed by Azure Directory
* Kerberos authentication for Team Foundation Server ([see notes](#q-i-thought-microsoft-was-maintaining-this-why-does-the-gcm-not-work-as-expected-with-tfs))
* Optional settings for build agent optimization

This is a community project so feel free to contribute ideas, submit bugs, fix bugs, or code new features. For detailed information on how the GCM works go to the [wiki](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/wiki/How-the-Git-Credential-Managers-works).

## Download and Install

To use the GCM, you can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases/latest). To install, double-click Setup.exe and follow the instructions presented.

When prompted to select your terminal emulator for Git Bash you should choose the Windows' default console window, or make sure GCM is [configured to use modal dialogs](Configuration.md#modalprompt). GCM cannot prompt you for credentials, at the console, in a MinTTY setup.

## How to use

You don't. It [magically](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues/31) works when credentials are needed. For example, when pushing to [Visual Studio Team Services](https://www.visualstudio.com), it automatically opens a window and initializes an oauth2 flow to get your token.

### Manual Installation

Note for users with special installation needs, you can still extract the `gcm-<version>.zip` file and run install.cmd from an administrator command prompt. This allows specification of the installation options explained below.

### Build and Install from Sources

To build and install the GCM yourself, clone the sources, open the solution file in Visual Studio, and build the solution. All necessary components will be copied from the build output locations into a `.\Deploy` folder at the root of the solution. From an elevated command prompt in the `.\Deploy` folder issue the following command `git-credential-manager install`.

[Various options](Configuration.md) are available for uniquely configured systems, like automated build systems. For systems with a **non-standard placement of Git** use the `--path <git>` parameter to supply where Git is located and thus where the GCM should be deployed to. For systems looking to **avoid checking for the Microsoft .NET Framework** and other similar prerequisites use the `--force` option. For systems looking for **silent installation without any prompts**, use the `--passive` option.

#### Additional Resources

* [Configuration Options](Configuration.md)
* [Usage Options](CredentialManager.md)
* [Build Agent and Automation Support](Automation.md)
* [Frequently Asked Questions](Faq.md)
* [Development and Debugging](Development.md)
* [Bitbucket Support](Bitbucket.md)

## License

This project uses the [MIT License](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/LICENSE.txt).
