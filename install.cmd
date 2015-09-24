:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

:: global constants
SET title="Microsoft Git Credential Manager for Windows"
SET powershell=0

:ADMIN_DETECT

    :: Installation requires elevated privileges to write to the `Program Files` directories
    net session 2>&1 1>nul
    IF %errorLevel% NEQ 0 (
        GOTO :ADMIN_NOT_FOUND
    )


:POWERSHELL_DETECT

    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\2\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\4\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1
    (REG QUERY HKLM\SOFTWARE\Microsoft\PowerShell\5\PowerShellEngine 2>&1| findstr "PSCompatibleVersion" 2>&1| findstr /I "2.0" 2>&1 1>nul) && SET powershell=1

    IF %powershell% NEQ 1 (
        GOTO :POWERSHELL_NOT_FOUND
    )

    (powershell -nologo -file install.ps1 "%title%" %*) || GOTO :FAILURE


:SUCCESS

    ECHO(
    ECHO Success! %title% was installed! ^^_^^
    ECHO(

    EXIT /B 0


:FAILURE

    ECHO(
    ECHO Something went wrong and I was unable to complete the installation. U_U
    PAUSE

    EXIT /B 1


:ADMIN_NOT_FOUND

    :: Script requires elevated privileges
    ECHO(
    ECHO You need to run this script elevated for it to work. U_U
    PAUSE

    EXIT /B 2


:POWERSHELL_NOT_FOUND

    ECHO(
    ECHO Failed to detect Microsoft PowerShell. Make sure it is installed. U_U
    ECHO Don't know where to get Microsoft Powershell? Try http://bit.ly/1KuWq86
    ECHO(
    PAUSE

    EXIT /B 3


