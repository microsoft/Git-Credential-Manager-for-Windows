:: Want to update this thing and don't know how?
:: Check http://ss64.com/nt/syntax.html
:: This script requires Pandoc 1.19.2 or later at https://github.com/jgm/pandoc/releases
@ECHO OFF
SETLOCAL ENABLEEXTENSIONS

SET assets=%~dp0Assets
SET dstdir=%~dp0Deploy
SET srcdir=%~dp0Docs

IF NOT EXIST "%dstdir%" (
    mkdir "%dstdir%" > nul 2>&1
)

IF EXIST %srcdir% (
    ((WHERE pandoc > nul 2>&1) && (pandoc --from=markdown --to=html5 --self-contained --standalone --include-in-header="%assets%\head_css.html" --normalize --output="%dstdir%\git-credential-manager.html" "%srcdir%\CredentialManager.md" "%srcdir%\Configuration.md" "%srcdir%\Faq.md" "%srcdir%\Automation.md" "%srcdir%\Development.md") && (ECHO git-credential-manager.html generated.)) || (ECHO Skipping git-credential-manager HTML generation, Pandoc not installed.)
    ((WHERE pandoc > nul 2>&1) && (pandoc --from=markdown --to=html5 --self-contained --standalone --include-in-header="%assets%\head_css.html" --normalize --output="%dstdir%\git-askpass.html" "%srcdir%\Askpass.md" "%srcdir%\Configuration.md" "%srcdir%\Faq.md" "%srcdir%\Automation.md" "%srcdir%\Development.md") && (ECHO git-askpass.html generated.)) || (ECHO Skipping git-askpass HTML generation, Pandoc not installed.)
)

EXIT /B 0