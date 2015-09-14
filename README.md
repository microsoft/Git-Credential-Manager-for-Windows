# Git Credential Manager for Windows
The [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) provides secure Git credential storage for Windows devices. It's the successor to the [Windows Credential Store for Git](https://gitcredentialstore.codeplex.com/), which is not longer maintained. All future developement will happen here and we've implemented a few of the features requests from the Windows Credential Store for Git, such as support for two-factor authentication on GitHub and use the standard Git prompt for credential input. In addition to these features, we've added support for Visual Studio Online authentication so it's easy to generate a personal access token and respond to a muti-factor authentication challegne from Microsot Account or Azure Active Directory.

This is a community project so feel free to contribute ideas, submit bugs, or code new features and fix bugs.

## Download and Install
If you just want to use the Git Credential Manager for Windows, you can download the [latest installer](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases). If you clone and build the repo, you can run install.cmd to install.

## Build agents
Build agents cannot manage modal dialogs, therefore we recommended the following configuration.
```
git config --global credential.helper.interactive never
```

Build agents often need to minimize the amount of network traffic they generate. 

To avoid Microsoft Account vs. Azure Active Directory look-up against a Visual Studio Online account use: 
```
git config --global credential.helper.authority Azure
```

To avoid unnecessary service account credential validation use: 
```
git config --global credential.helper.validate false
```

## Contribute
There are many ways to contribue.
* [Submit bugs](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) and help us verify fixes as they are checked in.
* Review [source code changes](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/pulls).
* Contribute bug fixes and features.

For code contributions, you will need to complete a Contributor License Agreement (CLA). Briefly, this agreement testifies that you are granting us permission to use the submitted change according to the terms of the project's license, and that the work being submitted is under appropriate copyright.

Please submit a Contributor License Agreement (CLA) before submitting a pull request. You may visit https://cla.microsoft.com to sign digitally. Alternatively, download the agreement ([Microsoft Contribution License Agreement.docx](https://www.codeplex.com/Download?ProjectName=typescript&DownloadId=822190) or [Microsoft Contribution License Agreement.pdf](https://www.codeplex.com/Download?ProjectName=typescript&DownloadId=921298)), sign, scan, and email it back to <cla@microsoft.com>. Be sure to include your github user name along with the agreement. Once we have received the signed CLA, we'll review the request.

## Debugging
To enable the Git Credential Manager logging, use the following:
```
git config --global credential.helper.writelog true
```

Log files will be written to the repo's local .git/ folder.

## License
[License](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/LICENSE.txt)
