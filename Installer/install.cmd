:: Want to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF
SETLOCAL ENABLEEXTENSIONS

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
:: https://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx
FOR /F "usebackq tokens=1,3" %%i in (`REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" /v Release`) DO IF "%%i" EQU "Release" SET /a netfx=%%j
FOR /F "usebackq tokens=1,3" %%i in (`REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release`) DO IF "%%i" EQU "Release" SET /a netfx=%%j

:: 0x5C733 - .NET Framework 4.5.1 installed with Windows 8.1 or Windows Server 2012 R2
IF %netfx% LSS 378675 (
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

"%~dp0\git-credential-manager" ?

EXIT /B 0
