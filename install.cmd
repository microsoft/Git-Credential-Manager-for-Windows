:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

SET gitExtensionName="Microsoft Git Credential Manager for Windows"
SET name=manager
SET exeName=git-credential-%name%.exe


:Hello
    ECHO Hello! I'll install %gitExtensionName%.
    ECHO(


:CHECK_PERMISSIONS
    ECHO Administrative permissions are required, detecting permissions...

    net session >nul 2>&1
    IF %errorLevel% == 0 (
        ECHO Running as admin. Good!
        ECHO(
    ) ELSE (
        GOTO NEED_ADMIN_ACCESS
    )

    :: Legacy OS not supported
    IF NOT EXIST "%ProgramFiles(x86)%" GOTO :LEGACY_OS

    SET destination=%~dp0
    SET installPath=%~dp0



:CHECK_GIT_INSTALL

    :: See if Git installed
    SET gitInstalled=0
    SET exeInstall=%~dp0

    ECHO Looking for your Git installation...

    IF EXIST "%ProgramFiles(x86)%\Git\cmd\git.exe" (
        :: 32-bit Git for Windows
        SET destination="%ProgramFiles(x86)%\Git\libexec\git-core\"
        SET exeInstall="%ProgramFiles(x86)%\Git\cmd\git.exe"
        SET gitInstalled=1
    ) ELSE IF EXIST "%ProgramFiles%\Git\cmd\git.exe" (
        :: 64-bit Git for Windows
        SET destination="%ProgramFiles%\Git\mingw64\libexec\git-core\"
        SET exeInstall="%ProgramFiles%\Git\cmd\git.exe"
        SET gitInstalled=1
    ) ELSE IF EXIST "%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\libexec\git-core\" (
        :: 32-bit Git Tools for Microsoft Engineers
        SET destination="%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\libexec\git-core\"
        SET exeInstall="%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\bin\git.exe"
        SET gitInstalled=1
    )

    :: Check if Git was found or not
    IF %gitInstalled% == 1 (
        ECHO Git found: %exeInstall%.
        ECHO(
        GOTO :PERFORM_SETUP
    ) ELSE (
        GOTO :NO_GIT_FOUND
    )


:PERFORM_SETUP
    ECHO Deploying from "%installPath%" to "%destination%"...

    :: Copy all of the necessary files to the git lib-exec folder
    (COPY /v /y "%installPath%"*.dll %destination%*.dll) || ((ECHO Oops! Fail to copy content from "%installPath%" to %destination%) && GOTO :FAILURE)
    (COPY /v /y "%installPath%"*.exe %destination%*.exe) || ((ECHO Oops! Fail to copy content from "%installPath%" to %destination%) && GOTO :FAILURE)


    :: Pre-configure it
    ECHO(

    (git config --global credential.helper %name% && (ECHO Updated your ~\.gitconfig [git config --global])) || GOTO :FAILURE
    git config --global --remove url.mshttps://devdiv.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for devdiv.visualstudio.com
    git config --global --remove url.mshttps://microsoft.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for microsoft.visualstudio.com
    git config --global --remove url.mshttps://mseng.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for mseng.visualstudio.com
    git config --global --remove url.mshttps://office.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for office.visualstudio.com

    ECHO(
    ECHO Success! %gitExtensionName% was installed! ^^_^^
    ECHO(

    GOTO :END


:NO_GIT_FOUND
    ECHO(
    ECHO Git not found in the expected location(s). Make sure Git is installed. U_U
    ECHO Don't know where to get Git? Try http://git-scm.com/
    ECHO(

    GOTO :END


:LEGACY_OS
    :: No support for legacy operating systems
    ECHO(
    ECHO Oops! 32-bit OS Not Supported. U_U
    PAUSE

    GOTO :END


:NEED_ADMIN_ACCESS
    :: Script requires elevated privileges
    ECHO(
    ECHO You need to run this script elevated for it to work. U_U
    PAUSE

    GOTO :END


:FAILURE
    ECHO(
    ECHO Something went wrong and I was unable to complete the installation. U_U
    PAUSE

    GOTO :END


:END
