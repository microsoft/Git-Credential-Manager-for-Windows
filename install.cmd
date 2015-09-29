:: Wanto to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF

:: globals
SET netfx=0

IF "%~1" EQU "--help" (
    GOTO :PRINT_HELP
)
IF "%~1" EQU "/?" (
    GOTO :PRINT_HELP
)


:NETFX_DETECT

    :: Detect if NETFX 4.5.1 or greater is installed
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client"| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x5C733" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x5CBF5" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x6004F" 1>nul 2>&1) && SET netfx=1
    (REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"| findstr "Release"| findstr /I "0x60051" 1>nul 2>&1) && SET netfx=1

    IF %netfx% NEQ 1 (
        GOTO :NETFX_NOT_FOUND
    )
    
    "%~dp0\git-credential-manager" install


:SUCCESS

    EXIT /B 0


:FAILURE

    ECHO.
    ECHO Something went wrong and I was unable to complete the installation. U_U

    EXIT /B %errorlevel%


:NETFX_NOT_FOUND

    ECHO.
    ECHO Failed to detect the Microsoft .NET Framework. Make sure it is installed. U_U
    ECHO Don't know where to get the Microsoft .NET Framework? Try http://bit.ly/1kE08Rz

    EXIT /B 3


:PRINT_HELP

    "~dp0\%git-credential-manager ?"

    EXIT /B 0


