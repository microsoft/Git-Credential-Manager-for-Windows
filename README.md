#Git Credential Manager for Windows
The [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) provides secure Git credential storage for Windows. It's the successor to the [Windows Credential Store for Git  (git-credential-winstore)](https://gitcredentialstore.codeplex.com/), which is not longer maintained.

This project provides the functionality from the Windows Credential Store for
Git(git-credential-winstore), and new features, including:

 * Secure password storage in the Windows Credential Store
 * Non-interative mode with multiple settings for build agents
 * Two-factor authentication for GitHub authentication
 * Visual Studio Online authentication including Multi-Factor Authentication,
   using a personal access token, Microsoft Account or Azure Active Directory
   account

This is a community project so feel free to contribute ideas, submit bugs, or code new features and fix bugs.

## Download and Install
If you just want to use the Git Credential Manager for Windows, you can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases). If you clone and build the repo, you can run install.cmd to install.

## Build agents
Build agents cannot manage modal dialogs, therefore we recommended the following configuration.
```
git config --global credential.interactive never
```

Build agents often need to minimize the amount of network traffic they generate. 

To avoid Microsoft Account vs. Azure Active Directory look-up against a Visual Studio Online account use: 
```
git config --global credential.authority Azure
```

To avoid unnecessary service account credential validation use: 
```
git config --global credential.validate false
```

## Contribute
There are many ways to contribue.
* [Submit bugs](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) and help us verify fixes as they are checked in.
* Review [source code changes](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/pulls).
* Contribute bug fixes and features.

For code contributions, you will need to complete a Contributor License Agreement (CLA). Briefly, this agreement testifies that you are granting us permission to use the submitted change according to the terms of the project's license, and that the work being submitted is under appropriate copyright.

Please submit a Contributor License Agreement (CLA) before submitting a pull request. You may visit https://cla.microsoft.com to sign digitally. Alternatively, download the agreement [Microsoft Contribution License Agreement.pdf](https://cla.microsoft.com/cladoc/microsoft-contribution-license-agreement.pdf), sign, scan, and email it back to <cla@microsoft.com>. Be sure to include your github user name along with the agreement. Once we have received the signed CLA, we'll review the request.

## Debugging
To enable the Git Credential Manager logging, use the following:
```
git config --global credential.writelog true
```

Log files will be written to the repo's local `.git/` folder.

## License
This project uses the [MIT License](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/LICENSE.txt).