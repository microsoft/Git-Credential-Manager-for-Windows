:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

:: global constants
SET powershell=0

IF "%~1" EQU "--help" (
    GOTO :PRINT_HELP
)
IF "%~1" EQU "/?" (
    GOTO :PRINT_HELP
)

:ADMIN_DETECT

    :: Installation requires elevated privileges to write to the `Program Files` directories
    (net session 2>&1 1>nul) || GOTO :ADMIN_NOT_FOUND


:POWERSHELL_DETECT

    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\2\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\4\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\5\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1

    IF %powershell% NEQ 1 (
        GOTO :POWERSHELL_NOT_FOUND
    )

    (powershell -file install.ps1 %*) || GOTO :FAILURE


:SUCCESS

    EXIT /B 0


:FAILURE

    ECHO.
    ECHO Something went wrong and I was unable to complete the installation. U_U

    EXIT /B %errorlevel%


:ADMIN_NOT_FOUND

    :: Script requires elevated privileges
    ECHO.
    ECHO You need to run this script elevated for it to work. U_U

    EXIT /B 2


:POWERSHELL_NOT_FOUND

    ECHO.
    ECHO Failed to detect Microsoft PowerShell. Make sure it is installed. U_U
    ECHO Don't know where to get Microsoft Powershell? Try http://bit.ly/1KuWq86

    EXIT /B 3


:PRINT_HELP

    ECHO.
    ECHO install [[--git-path ^<path-to-git^>] [--install-to ^<installation-path^>] [--skip-netfx]]
    ECHO.
    ECHO: --git-path    Specifies a location to look for git.exe when installing in
    ECHO:               addition to any Git installation locations detected.
    ECHO.
    ECHO: --install-to  Specifies a path to install to. This is in addition to any Git
    ECHO:               installation locations detected.
    ECHO.
    ECHO: --skip-netfx  Specifies that the installer should skip the detection of the
    ECHO:               Microsoft .NET Framwork (aka netfx) and that the installer should
    ECHO:               progress regardless.
    ECHO.

    EXIT /B 0


