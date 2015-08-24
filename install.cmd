:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

SET gitExtensionName="Microsoft Git Credential Manager for Windows"
SET name=manager
SET exeName=git-credential-%name%.exe
SET destination=%~dp0
SET installPath=%~dp0
SET gitInstalled=0
SET netfxInstalled=0


:Hello
    ECHO Hello! I'll install %gitExtensionName%.
    ECHO(


:CHECK_PERMISSIONS
    :: Installation requires elevated privileges to write to the `Program Files` directories
    net session >nul 2>&1
    IF %errorLevel% == 0 (
        GOTO :INSTALL
    ) ELSE (
        GOTO :NEED_ADMIN_ACCESS
    )


:INSTALL

    
    :: Detect if NETFX 4.5.1 or greater is installed
    ECHO Looking for prequisites...
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfxInstalled=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfxInstalled=1

    IF %netfxInstalled% NEQ 1 (
        GOTO :NO_NETFX_FOUND
    )

    :: See if and where Git installed
    ECHO Looking for Git installation(s)...

    IF EXIST "%ProgramFiles%\Git\libexec\git-core\" (
        :: 32-bit Git for Windows 1.x on 32-bit Windows
        SET destination="%ProgramFiles%\Git\libexec\git-core\"

        CALL :PERFORM_SETUP
    )

    IF EXIST "%ProgramFiles(x86)%\Git\libexec\git-core\" (
        :: 32-bit Git for Windows 1.x on 64-bit Windows
        SET destination="%ProgramFiles(x86)%\Git\libexec\git-core\"

        CALL :PERFORM_SETUP
    )

    IF EXIST "%ProgramFiles%\Git\mingw32\libexec\git-core\" (
        :: 32-bit Git for Windows 2.x on 32-bit Windows
        SET destination="%ProgramFiles%\Git\mingw64\libexec\git-core\"

        CALL :PERFORM_SETUP
    )

    IF EXIST "%ProgramFiles(x86)%\Git\mingw32\libexec\git-core\" (
        :: 32-bit Git for Windows 2.x on 64-bit Windows
        SET destination="%ProgramFiles(x86)%\Git\mingw32\libexec\git-core\"

        CALL :PERFORM_SETUP
    )

    IF EXIST "%ProgramFiles%\Git\mingw64\libexec\git-core\" (
        :: 64-bit Git for Windows 2.x on 64-bit Windows
        SET destination="%ProgramFiles%\Git\mingw64\libexec\git-core\"

        CALL :PERFORM_SETUP
    )

    :: Check if Git was found or not
    IF %gitInstalled% == 1 (
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
    ECHO(
    ECHO Deploying from "%installPath%" to %destination%...
    ECHO(

    :: Copy all of the necessary files to the git lib-exec folder
    (COPY /v /y "%installPath%"*.dll %destination%*.dll) || ((ECHO Oops! Fail to copy content from "%installPath%" to %destination%) && GOTO :FAILURE)
    (COPY /v /y "%installPath%"*.exe %destination%*.exe) || ((ECHO Oops! Fail to copy content from "%installPath%" to %destination%) && GOTO :FAILURE)

    :: Pre-configure it
    ECHO(

    (git config --global credential.helper %name% && (ECHO Updated your ~\.gitconfig [git config --global])) || GOTO :FAILURE

    SET gitInstalled=1


