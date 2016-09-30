:: Want to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
@ECHO OFF
SETLOCAL ENABLEEXTENSIONS

SET assets=%~dp0..\Assets
SET dstdir=%~dp0..\Deploy
SET srcdir=%~dp0..\Docs

IF NOT EXIST "%dstdir%" (
    mkdir "%dstdir%" > nul 1>&2
)

IF EXIST %srcdir% (
    (WHERE pandoc > nul 1>&2) && (pandoc --from=markdown --include-in-header="%assets%\help.css" --to=html --normalize --output="%dstdir%\git-credential-manager.html" "%srcdir%\CredentialManager.md" "%srcdir%\Configuration.md" "%srcdir%\Faq.md" "%srcdir%\Automation.md" "%srcdir%\Development.md" > nul 1>&2) && (ECHO git-credential-manager.html generated.)
    (WHERE pandoc > nul 1>&2) && (pandoc --from=markdown --include-in-header="%assets%\help.css" --to=html --normalize --output="%dstdir%\git-askpass.html" "%srcdir%\Askpass.md" "%srcdir%\Configuration.md" "%srcdir%\Faq.md" "%srcdir%\Automation.md" "%srcdir%\Development.md" > nul 1>&2) && (ECHO git-askpass.html generated.)
)

EXIT /B 0