:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

:: global constants
SET gitExtensionName="Microsoft Git Credential Manager for Windows"
SET name=manager
SET exeName=git-credential-%name%.exe

:: global variables
SET installPath=%~dp0
SET gitInstalled=0
SET netfxInstalled=0
SET pathToGit=""

:: parameter variables
SET paramGitPath=""
SET paramNetfx=0
SET paramInstallTo=""

:: clean up quoted values
CALL :DEQUOTE gitExtensionName


:PARAMETERS

    SETLOCAL ENABLEDELAYEDEXPANSION

    :: if the value of %~1 is empty, there are no more parameters to parse so break
    IF "%~1" EQU "" GOTO :PARAMETERS_END

    :: check for help queries
    IF "%~1" EQU "--help" (
        GOTO :PRINT_HELP
    )
    IF "%~1" EQU "/?" (
        GOTO :PRINT_HELP
    )

    :: check for --install-to options
    IF "%~1" EQU "--install-to" (
        IF EXIST "%~2" (
            SET paramInstallTo="%~2"
            CALL :DEQUOTE paramInstallTo

            ECHO Install path set to "!paramInstallTo!".

            SHIFT
        )
        IF NOT EXIST "%~2" (
            ECHO Supplied install path "%~2" appears to be invalid.

            EXIT /B 9
        )
    )

    :: check for special git install locations
    IF "%~1" EQU "--git-path" (
        IF EXIST "%~2" (
            IF EXIST "%~2\Cmd\Git.exe" (
                SET paramGitPath = "%~2\Cmd\Git.exe"
                CALL :DEQUOTE paramGitPath

                ECHO Git path set to "!paramGitPath!".

                SHIFT
            )
            IF NOT EXIST "%~2\Cmd\Git.exe" (
                ECHO Supplied Git path "%~2" appears to be invalid.

                EXIT /B 9
            )
        )
        IF NOT EXIST "%~2" (
            ECHO Supplied Git path "%~2" appears to be invalid.

            EXIT /B 9
        )
    )

    :: check for skipping NetFX detection
    IF "%~1" EQU "--no-netfx" (
        SET paramNetfx=1
    )

    SHIFT

    GOTO :PARAMETERS

:PARAMETERS_END

    SETLOCAL DISABLEDELAYEDEXPANSION


:HELLO

    ECHO Hello! I'll install %gitExtensionName%.
    ECHO(


:CHECK_PERMISSIONS

    :: Installation requires elevated privileges to write to the `Program Files` directories
    net session >nul 2>&1
    IF %errorLevel% EQU 0 (
        GOTO :INSTALL
    ) ELSE (
        GOTO :NEED_ADMIN_ACCESS
    )


:INSTALL

    SET netfxBase="HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4"
    SET netfxClient=%netfxBase%\Client
    SET netfxFull=%netfxBase%\Full

    :: Detect if NETFX 4.5.1 or greater is installed
    ECHO Looking for prequisites...
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "0x5C786" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfxInstalled=1

    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "378675" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "378758" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "379893" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "393295" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxClient%| findstr "Release"| findstr /I "393297" 1>nul 2>&1) && SET netfxInstalled=1

    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "0x5C786" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfxInstalled=1

    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "378675" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "378758" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "379893" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "393295" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY %netfxFull%| findstr "Release"| findstr /I "393297" 1>nul 2>&1) && SET netfxInstalled=1

    IF %paramNetfx% EQU 1 (
        ECHO NetFX detection skipped.
    )
    IF %paramNetfx% NEQ 1 (
        IF %netfxInstalled% EQU 1 (
            ECHO NetFX found.
        )
        IF %netfxInstalled% NEQ 1 (
            GOTO :NO_NETFX_FOUND
        )
    )

    IF EXIST "%paramInstallTo%" (
        :: custom location
        ECHO Deploying to custom location...
        ECHO(

        CALL :PERFORM_SETUP "%paramInstallTo%" "<empty>"
    )

    :: See if and where Git installed (in order of desirability)
    ECHO Looking for Git installation(s)...
    

    IF EXIST "%paramGitPath%\mingw64\libexec\git-core\" (
        :: 64-bit Git for Windows 2.x
        CALL :PERFORM_SETUP "%paramGitPath%\mingw64\libexec\git-core\" "%paramGitPath%\Git\Cmd"
    )

    IF EXIST "%paramGitPath%\mingw32\libexec\git-core\" (
        :: 32-bit Git for Windows 2.x
        CALL :PERFORM_SETUP "%paramGitPath%\mingw32\libexec\git-core\" "%paramGitPath%\Git\Cmd"
    )

    IF EXIST "%paramGitPath%\libexec\git-core\" (
        :: 32-bit Git for Windows 1.x
        CALL :PERFORM_SETUP "%paramGitPath%\libexec\git-core\" "%paramGitPath%\Git\Cmd"
    )

    IF EXIST "%ProgramFiles%\Git\mingw64\libexec\git-core\" (
        :: 64-bit Git for Windows 2.x on 64-bit Windows
        CALL :PERFORM_SETUP "%ProgramFiles%\Git\mingw64\libexec\git-core\" "%ProgramFiles%\Git\Cmd"
    )

    IF EXIST "%ProgramFiles%\Git\mingw32\libexec\git-core\" (
        :: 32-bit Git for Windows 2.x on 32-bit Windows
        CALL :PERFORM_SETUP "%ProgramFiles%\Git\mingw32\libexec\git-core\" "%ProgramFiles%\Git\Cmd"
        
    )

    IF EXIST "%ProgramFiles(x86)%\Git\mingw32\libexec\git-core\" (
        :: 32-bit Git for Windows 2.x on 64-bit Windows
        CALL :PERFORM_SETUP "%ProgramFiles(x86)%\Git\mingw32\libexec\git-core\" "%ProgramFiles(x86)%\Git\Cmd"
    )

    IF EXIST "%ProgramFiles%\Git\libexec\git-core\" (
        :: 32-bit Git for Windows 1.x on 32-bit Windows
        CALL :PERFORM_SETUP "%ProgramFiles%\Git\libexec\git-core\" "%ProgramFiles%\Git\Cmd"
    )

    IF EXIST "%ProgramFiles(x86)%\Git\libexec\git-core\" (
        :: 32-bit Git for Windows 1.x on 64-bit Windows
        CALL :PERFORM_SETUP "%ProgramFiles(x86)%\Git\libexec\git-core\" "%ProgramFiles(x86)%\Git\Cmd"
    )

    :: Check if Git was found or not
    IF %gitInstalled% EQU 1 (
        CALL :UPDATE_CONFIG
        GOTO :SUCCESS
    ) ELSE (
        GOTO :NO_GIT_FOUND
    )


:NO_NETFX_FOUND

    ECHO(
    ECHO Failed to detect the Microsoft .NET Framework. Make sure it is installed. U_U
    ECHO Don't know where to get the Microsoft .NET Framework? Try http://bit.ly/1kE08Rz
    ECHO(
    PAUSE

    EXIT /B 4


:NO_GIT_FOUND

    ECHO(
    ECHO Git not found in the expected location(s). Make sure Git is installed. U_U
    ECHO Don't know where to get Git? Try http://git-scm.com/
    ECHO(
    PAUSE

    EXIT /B 3


:NEED_ADMIN_ACCESS

    :: Script requires elevated privileges
    ECHO(
    ECHO You need to run this script elevated for it to work. U_U
    PAUSE

    EXIT /B 2


:FAILURE

    ECHO(
    ECHO Something went wrong and I was unable to complete the installation. U_U
    PAUSE

    EXIT /B 1


:SUCCESS

    ECHO(
    ECHO Success! %gitExtensionName% was installed! ^^_^^
    ECHO(

    EXIT /B 0


:PERFORM_SETUP

    SET destination="%~1"
    SET localGitPath="%~2"
    
    CALL :DEQUOTE destination
    CALL :DEQUOTE localGitPath

    ECHO(
    ECHO Deploying from "%installPath%" to "%destination%"...
    ECHO(

    CALL :DEQUOTE destination

    :: Copy all of the necessary files to the git lib-exec folder
    (COPY /v /y "%installPath%"*.dll "%destination%"*.dll) || ((ECHO Oops! Fail to copy content from "%installPath%" to "%destination%") && GOTO :FAILURE)
    (COPY /v /y "%installPath%"*.exe "%destination%"*.exe) || ((ECHO Oops! Fail to copy content from "%installPath%" to "%destination%") && GOTO :FAILURE)

    CALL :DEQUOTE pathToGit

    IF NOT EXIST "%pathToGit%" (
        IF EXIST "%localGitPath%" (

            SET pathToGit="%localGitPath%"
            CALL :DEQUOTE pathToGit

            SETLOCAL ENABLEDELAYEDEXPANSION

            ECHO(
            ECHO Path to Git is "!pathToGit!".

            SETLOCAL DISABLEDELAYEDEXPANSION
        )
    )

    SET gitInstalled=1

    GOTO :eof


:UPDATE_CONFIG

    :: Pre-configure it
    ECHO(

    ("%pathToGit%\git.exe" config --global credential.helper %name% && (ECHO Updated your ~\.gitconfig [git config --global])) || GOTO :FAILURE

    GOTO :eof


:DEQUOTE

    FOR /f "delims=" %%A IN ('ECHO %%%1%%') do set %1=%%~A
    GOTO :eof


:PRINT_HELP

    :: Print help information
    ECHO:%exeName% [[--install-to ^<path^>] [--git-path ^<path-to-git^>] --no-netfx]
    ECHO(
    ECHO: --install-to  Specifies a path to install to. This is in addition to to
    ECHO:               any Git installation locations detected.
    ECHO(
    ECHO: --git-path    Specifies a location to look for git.exe when installing
    ECHO:               in addition to any Git installation locations detected.
    ECHO(
    ECHO: --no-netfx    Specifies that the installer should NOT detect if the
    ECHO:               Microsoft .NET Framwork (aka NetFX) is installed or not
    ECHO:               and that the installer should progress regardless.
    ECHO(

    EXIT /B 0


