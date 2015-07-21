:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF


SET gitExtensionName=Git Credential Helper

:CHECK_PERMISSIONS
    ECHO Administrative permissions required. 
    ECHO Detecting permissions...

    net session >nul 2>&1
    IF %errorLevel% == 0 (
        ECHO Running as admin. Good!
    ) ELSE (
        GOTO NEED_ADMIN_ACCESS
    )

::32-bit OS not supported
IF NOT EXIST "%ProgramFiles(x86)%" GOTO :LEGACY_OS

ECHO Hello! I'll install "%gitExtensionName%" so that you can interact with Visual Studio Online.
SET installPath=%~dp0
SET helperInstalled=0
SET remoteFileName=git-credential-man.exe


:GIT_TOOLS_FOR_MICROSOFT_ENGINEERS

::See if Git Tools for Microsoft Engineers are installed
SET destination=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\libexec\git-core\
SET exeInstall=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\bin\git.exe
IF NOT EXIST "%exeInstall%" GOTO :MSYSGIT

ECHO I'm installing "%gitExtensionName%" from "%installPath%" to "%destination%"...

:: Copy the files
GOTO COPY_FILES


:MSYSGIT

::See if Msys Git is installed
SET destination=%ProgramFiles(x86)%\Git\libexec\git-core\
SET exeInstall=%ProgramFiles(x86)%\Git\cmd\git.exe
IF NOT EXIST "%exeInstall%" GOTO :INSTALLED_CHECK

ECHO I'm installing "%gitExtensionName%" from "%installPath%" to "%destination%"...

::Copy the files
GOTO COPY_FILES

:COPY_FILES
IF EXIST "%destination%git-credential-store.exe" IF NOT EXIST "%destination%~git-credential-store.exe~" RENAME "%destination%git-credential-store.exe" "~git-credential-store.exe~"
(COPY /y "%installPath%"*.exe "%destination%"*.exe) || ECHO Oops! Fail to copy content from "%installPath%" to "%destination%"
(COPY /y "%installPath%"*.dll "%destination%"*.dll) || ECHO Oops! Fail to copy content from "%installPath%" to "%destination%"
SET helperInstalled=1
GOTO :END


:INSTALLED_CHECK

::Check if Git was found or not
IF %helperInstalled% == 1 GOTO :GIT_FOUND


:NO_GIT_FOUND
ECHO Git not found at the expected locations :(. Make sure Git is installed.
GOTO :END


:GIT_FOUND
:: Pre-configure it for microsoft.visualstudio.com and mseng.visualstudio.com
git config --global credential.helper store

ECHO(
ECHO %gitExtensionName% was installed!
ECHO(
GOTO :END


:LEGACY_OS
ECHO Oops! 32-bit OS Not Supported
GOTO :END


:NEED_ADMIN_ACCESS
ECHO You need to run this script elevated for it to work. Press ENTER to exit...
pause >nul
GOTO :END


:END