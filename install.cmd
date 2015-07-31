:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

SET gitExtensionName=Microsoft Git Credential Secure Store for Windows
SET name=manager
SET exeName=git-credential-%name%.exe

:CHECK_PERMISSIONS
    ECHO Administrative permissions required. 
    ECHO Detecting permissions...

    net session >nul 2>&1
    IF %errorLevel% == 0 (
        ECHO Running as admin. Good!
    ) ELSE (
        GOTO NEED_ADMIN_ACCESS
    )

    :: Legacy OS not supported
    IF NOT EXIST "%ProgramFiles(x86)%" GOTO :LEGACY_OS
    
    ECHO(
    ECHO Hello! I'll install "%gitExtensionName%" so that Git can store credentials securely.
    ECHO(

    SET installPath=%~dp0
    SET helperInstalled=0


:GIT_TOOLS_FOR_MICROSOFT_ENGINEERS
    :: See if Git Tools for Microsoft Engineers is installed
    SET destination=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\libexec\git-core\
    SET exeInstall=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\bin\git.exe
    IF NOT EXIST "%exeInstall%" GOTO :MSYSGIT
    
    ECHO I'm installing "%gitExtensionName%" from "%installPath%" to "%destination%"...
    
    GOTO COPY_FILES


:MSYSGIT
    :: See if Msys Git is installed
    SET destination=%ProgramFiles(x86)%\Git\libexec\git-core\
    SET exeInstall=%ProgramFiles(x86)%\Git\cmd\git.exe
    IF NOT EXIST "%exeInstall%" GOTO :INSTALLED_CHECK
    
    ECHO I'm installing from "%installPath%" to "%destination%"...
    
    GOTO COPY_FILES


:COPY_FILES
    :: Copy all of the necessary files to the git lib-exec folder
    (COPY /v /y "%installPath%"*.dll "%destination%"*.dll) || ((ECHO Oops! Fail to copy content from "%installPath%" to "%destination%") && GOTO :FAILURE)
    (COPY /v /y "%installPath%"*.exe "%destination%"*.exe) || ((ECHO Oops! Fail to copy content from "%installPath%" to "%destination%") && GOTO :FAILURE)
    
    SET helperInstalled=1


:INSTALLED_CHECK
    :: Check if Git was found or not
    IF %helperInstalled% == 1 (
        GOTO :GIT_FOUND
    ) ELSE (
        GOTO :NO_GIT_FOUND
    )


:NO_GIT_FOUND
    ECHO Git not found in the expected location(s). Make sure Git is installed. U_U
    ECHO Don't know where to get Git? Try http://git-scm.com/

    GOTO :END


:GIT_FOUND
    :: Pre-configure it
    ECHO(
        
    (git config --global credential.helper %name% && (ECHO Updated your ~\.gitconfig [git config --global])) || GOTO :FAILURE
    git config --global --remove url.mshttps://devdiv.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for devdiv.visualstudio.com
    git config --global --remove url.mshttps://microsoft.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for microsoft.visualstudio.com
    git config --global --remove url.mshttps://mseng.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for mseng.visualstudio.com
    git config --global --remove url.mshttps://office.visualstudio.com/ >nul 2>&1 && ECHO Removed mshttp nonsense for office.visualstudio.com
    
    ECHO(
    ECHO %gitExtensionName% was installed! ^^_^^
    ECHO(
        
    GOTO :END


:LEGACY_OS
    :: No support for legacy operating systems
    ECHO Oops! 32-bit OS Not Supported. U_U
    GOTO :END


:NEED_ADMIN_ACCESS
    :: Script requires elevated privileges
    ECHO You need to run this script elevated for it to work. U_U

    GOTO :END


:FAILURE
    ECHO Something went wrong and I was unable to complete the installation. U_U

    GOTO :END


:END
