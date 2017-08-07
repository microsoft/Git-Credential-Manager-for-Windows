@ECHO OFF

SETLOCAL

PUSHD %~dp0

SET PATH=C:\MSYS\bin;%PATH%

IF NOT DEFINED COVDIR SET "COVDIR=C:\cov-analysis"
IF DEFINED COVDIR IF NOT EXIST "%COVDIR%" (
    ECHO.
    ECHO ERROR: Coverity not found in "%COVDIR%"
    GOTO End
)


CALL :SubVSPath
IF NOT EXIST "%VS_PATH%" CALL :SUBMSG "ERROR" "Visual Studio 2017 NOT FOUND!"


:Cleanup
IF EXIST "cov-int"  RD /q /s "cov-int"
IF EXIST "gcw.lzma" DEL "gcw.lzma"
IF EXIST "gcw.tar"  DEL "gcw.tar"
IF EXIST "gcw.tgz"  DEL "gcw.tgz"


:Main
SET "TOOLSET=%VS_PATH%\Common7\Tools\vsdevcmd"
CALL "%TOOLSET%"

SET MSBUILD_SWITCHES=/nologo /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"^
 /maxcpucount /consoleloggerparameters:DisableMPLogging;Summary;Verbosity=minimal

"%COVDIR%\bin\cov-build.exe" --dir cov-int MSBuild.exe GitCredentialManager.sln %MSBUILD_SWITCHES%


:tar
tar --version 1>&2 2>NUL || (ECHO. & ECHO ERROR: tar not found & GOTO SevenZip)
tar caf "gcw.lzma" "cov-int"
GOTO End


:SevenZip
CALL :SubDetectSevenzipPath

rem Coverity is totally bogus with lzma...
rem And since I cannot replicate the arguments with 7-Zip, just use tar/gzip.
IF EXIST "%SEVENZIP%" (
    "%SEVENZIP%" a -ttar "gcw.tar" "cov-int"
    "%SEVENZIP%" a -tgzip "gcw.tgz" "gcw.tar"
    IF EXIST "gcw.tar" DEL "gcw.tar"
    GOTO End
)


:SubDetectSevenzipPath
FOR %%G IN (7z.exe) DO (SET "SEVENZIP_PATH=%%~$PATH:G")
IF EXIST "%SEVENZIP_PATH%" (SET "SEVENZIP=%SEVENZIP_PATH%" & EXIT /B)

FOR %%G IN (7za.exe) DO (SET "SEVENZIP_PATH=%%~$PATH:G")
IF EXIST "%SEVENZIP_PATH%" (SET "SEVENZIP=%SEVENZIP_PATH%" & EXIT /B)

FOR /F "tokens=2*" %%A IN (
    'REG QUERY "HKLM\SOFTWARE\7-Zip" /v "Path" 2^>NUL ^| FIND "REG_SZ" ^|^|
     REG QUERY "HKLM\SOFTWARE\Wow6432Node\7-Zip" /v "Path" 2^>NUL ^| FIND "REG_SZ"') DO SET "SEVENZIP=%%B\7z.exe"
EXIT /B


:SubVSPath
FOR /f "delims=" %%A IN ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -property installationPath -latest -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.ATLMFC Microsoft.VisualStudio.Component.VC.Tools.x86.x64') DO SET "VS_PATH=%%A"
EXIT /B


:End
POPD
ECHO. & ECHO Press any key to close this window...
PAUSE >NUL
ENDLOCAL
EXIT /B
