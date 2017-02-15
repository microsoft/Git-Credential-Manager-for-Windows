# Git Askpass for Windows

[Git Askpass for Windows](https://github.com/Microsoft/Git-Credential-Manager-for-Windows) (Askpass) provides secure Git credential storage for Windows. Askpass provides multi-factor authentication support for [Visual Studio Team Services](https://www.visualstudio.com/), [Team Foundation Server](https://www.visualstudio.com/en-us/products/tfs-overview-vs.aspx), and [GitHub](https://github.com/).

## Usage

After installation and configuration, Git will use the Git Askpass and you will only need to interact with any authentication dialogs asking for credentials. The Askpass stays invisible as much as possible, so ideally you’ll forget that you’re depending on Askpass at all.

    git askpass

For Git to use Askpass correctly, the `GIT_ASKPASS` environment variable needs contain the full path to the `git-askpass.exe` executable (example: `setx GIT_ASKPASS "C:\Program Files\Git\mingw64\libexec\git-core\git-askpass.exe"`). SSH can also be configured to use Askpass in the same manner using the `SSH_ASKPASS` environment variable; however, SSH currently will not use Askpass if it detects a TTY console (ala the ability to just ask on the console).
