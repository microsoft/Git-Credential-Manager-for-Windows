# Git Credential Manager for Windows

The [Git Credential Manager for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (GCM) provides secure Git credential storage for Windows. GCM provides multi-factor authentication support for [Visual Studio Team Services](https://www.visualstudio.com/), [Team Foundation Server](https://www.visualstudio.com/en-us/products/tfs-overview-vs.aspx), [GitHub](https://github.com/), and [BitBucket](https://bitbucket.org).

## Usage

After installation, Git will use the Git Credential Manager for Windows and you will only need to interact with any authentication dialogs asking for credentials. The GCM stays invisible as much as possible, so ideally you’ll forget that you’re depending on GCM at all.

Assuming the GCM has been installed, using your favorite Windows console (Command Prompt, PowerShell, ConEmu, etc), use the following command to interact directly with the GCM.

    git credential-manager [<command> [<args>]]

## Commands

### delete

Removes stored credentials for a given URL. Any future attempts to authenticate with the remote will require authentication steps to be completed again.

### deploy _\[--path \<installation_path\>\] \[--passive\] \[--force\]_

Deploys the Git Credential Manager for Windows package and sets Git configuration to use the helper.

#### --path \<installation_path\>

Specifies a path (\<installation_path\>) for the installer to deploy to. If a path is provided, the installer will not seek additional Git installations to modify.

#### --passive

Instructs the installer to not prompt the user for input during deployment and restricts output to error messages only.

When combined with *--force* all output is eliminated; only the return code can be used to validate success.

#### --force

Instructs the installer to proceed with deployment even if prerequisites are not met or errors are encountered.

When combined with *--passive* all output is eliminated; only the return code can be used to validate success.

### remove _\[--path \<installation_path\>\] \[--passive\] \[--force\]_

Removes the Git Credential Manager for Windows package and unsets Git configuration to no longer use the helper.

#### --path \<installation_path\>

Specifies a path (\<installation_path\>) for the installer to remove from. If a path is provided, the installer will not seek additional Git installations to modify.

#### --passive

Instructs the installer to not prompt the user for input during removal and restricts output to error messages only.

When combined with *--force* all output is eliminated; only the return code can be used to validate success.

#### --force

Instructs the installer to proceed with removal even if prerequisites are not met or errors are encountered.

When combined with *--passive* all output is eliminated; only the return code can be used to validate success.

### version

Displays the current version.

### clear

Synonym for **delete**.

### install

Synonym for **deploy**.

### uninstall

Synonym for **remove**.

### get / store / erase / fill / approve / reject

Commands for interaction with Git.
