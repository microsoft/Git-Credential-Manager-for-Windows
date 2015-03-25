@ECHO OFF

SET extensionName=Git Credential Manager

:CHECK_PERMISSIONS
	ECHO Administrative permissions required.
	ECHO Detecting permissions...

	net session >nul 2>&1
	IF %errorLevel% ==0 (
		ECHO Running as admin. Good!
	) ELSE (
		GOTO NEED_ADMIN_ACCESS
	)

::32-bit OS not supported
IF NOT EXISTS "%programFiles (x86)%" GOTO :LEGACY_OS

ECHO Hello! I'll install %extensionName%.
SET installPath=%~dp0
SET extensionInstalled=0
SET fileName=git-credential-man.exe

:GIT_TOOLS_FOR_MICROSOFT_ENGINEERS

::See if Git Tools for Microsoft Engineers are installed
SET destination=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\libexec\git-core\
SET exeInstall=%ProgramFiles(x86)%\Git Tools for Microsoft Engineers\bin\git.exe
IF NOT EXIST "%exeInstall%" GOTO :MSYSGIT

ECHO I'm installing "%gitExtensionName%" from "%installPath%" to "%destination%"...

::Copy the files
(COPY /y "%installPath%" "%destination%") || ECHO Oops! Fail to copy content from "%installPath%" to "%destination%"
SET extensionInstalled=1


:MSYSGIT

::See if Msys Git is installed
SET destination=%ProgramFiles(x86)%\Git\libexec\git-core\
SET exeInstall=%ProgramFiles(x86)%\Git\cmd\git.exe
IF NOT EXIST "%exeInstall%" GOTO :INSTALLED_CHECK

ECHO I'm installing "%gitExtensionName%" from "%installPath%" to "%destination%"...

::Copy the files
(COPY /y "%installPath%" "%destination%") || ECHO Oops! Fail to copy content from "%installPath%" to "%destination%"
SET extensionInstalled=1