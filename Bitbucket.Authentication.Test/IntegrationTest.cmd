@echo off

rem try and keep things clean and easy to read
set GCM_TRACE=

rem To use other accounts/repos override the following env vars
rem BASICAUTH_ACCOUNTNAME - the username/name of an account WITHOUT 2FA
rem BASICAUTH_USEREMAIL - the escaped email address associated with the above account
rem BASICAUTH_REPONAME - the name of private repo associated with the above account
rem OAUTH_ACCOUNTNAME the username/name of an account WITH 2FA
rem OAUTH_USEREMAIL - the escaped email address associated with the above account
rem OAUTH_REPONAME - the name of private repo associated with the above account

IF "%BASICAUTH_ACCOUNTNAME%"=="" set BASICAUTH_ACCOUNTNAME=testoneitofinity
IF "%BASICAUTH_USEREMAIL%"=="" set BASICAUTH_USEREMAIL=testone%%40itofinity.co.uk
IF "%OAUTH_ACCOUNTNAME%"=="" set OAUTH_ACCOUNTNAME=testtwoitofinity
IF "%OAUTH_USEREMAIL%"=="" set OAUTH_USEREMAIL=testtwo%%40itofinity.co.uk
IF "%BASICAUTH_REPONAME%"=="" set BASICAUTH_REPONAME=git-test-private
IF "%OAUTH_REPONAME%"=="" set OAUTH_REPONAME=git-test-private

set BASICAUTH_URL=https://bitbucket.org/%BASICAUTH_ACCOUNTNAME%/%BASICAUTH_REPONAME%.git
set BASICAUTH_USERURL=https://%BASICAUTH_ACCOUNTNAME%@bitbucket.org/%BASICAUTH_ACCOUNTNAME%/%BASICAUTH_REPONAME%.git
set BASICAUTH_USEREMAILURL=https://%BASICAUTH_USEREMAIL%@bitbucket.org/%BASICAUTH_ACCOUNTNAME%/%BASICAUTH_REPONAME%.git

set OAUTH_URL=https://bitbucket.org/%OAUTH_ACCOUNTNAME%/%OAUTH_REPONAME%.git
set OAUTH_USERURL=https://%OAUTH_ACCOUNTNAME%@bitbucket.org/%OAUTH_ACCOUNTNAME%/%OAUTH_REPONAME%.git
set OAUTH_USEREMAILURL=https://%OAUTH_USEREMAIL%@bitbucket.org/%OAUTH_ACCOUNTNAME%/%OAUTH_REPONAME%.git

rem clear earlier runs
del /s /f /q .\gcm_test\*.*
for /f %%f in ('dir /ad /b .\gcm_test\') do rd /s /q .\gcm_test\%%f

IF "%1"=="basic-username" GOTO basic-username
IF "%1"=="basic-email" GOTO basic-email
IF "%1"=="oauth-username" GOTO oauth-username
IF "%1"=="oauth-email" GOTO oauth-email
IF "%1"=="basic-username-peruserurl" GOTO basic-username-peruserurl
IF "%1"=="basic-email-peruserurl" GOTO basic-email-peruserurl
IF "%1"=="oauth-username-peruserurl" GOTO oauth-username-peruserurl
IF "%1"=="oauth-email-peruserurl" GOTO oauth-email-peruserurl
IF "%1"=="basic-username-peruseremailurl" GOTO basic-username-peruseremailurl
IF "%1"=="basic-email-peruseremailurl" GOTO basic-email-peruseremailurl
IF "%1"=="oauth-username-peruseremailurl" GOTO oauth-username-peruseremailurl
IF "%1"=="oauth-email-peruseremailurl" GOTO oauth-email-peruseremailurl

IF "%2"=="true" GOTO set GCM_TRACE=true

rem url + username login + basic auth + get-prompt
rem - https://bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem url + username login + basic auth + get-read
rem - https://bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem url + email login + basic auth + get-prompt
rem - https://bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk
rem url + email login + basic auth + get-read
rem - https://bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk

rem url + username login + oauth auth + get-prompt
rem - https://bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem url + username login + oauth auth + get-read
rem - https://bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem url + email login + oauth auth + get-prompt
rem - https://bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk
rem url + email login + oauth auth + get-read
rem - https://bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk

:basic-username
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_URL%
set URLTYPE=url
set LOGIN=username
set AUTH=basic
set USER=%BASICAUTH_ACCOUNTNAME%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
pause
IF NOT "%1"=="" GOTO EOF

:basic-email
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_URL%
set URLTYPE=url
set LOGIN=email
set AUTH=basic
set USER=%BASICAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents... 
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-username
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.
set CLONE_URL=%OAUTH_URL%
set URLTYPE=url
set LOGIN=username
set AUTH=oauth
set USER=%OAUTH_ACCOUNTNAME%
echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-email
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%OAUTH_URL%
set URLTYPE=url
set LOGIN=email
set AUTH=oauth
set USER=%OAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST


rem user-specific-url + username login + basic auth + get-prompt
rem - https://testoneitofinity@bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem user-specific-url + username login + basic auth + get-read
rem - https://testoneitofinity@bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem user-specific-url + email login + basic auth + get-prompt
rem - https://testoneitofinity@bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk
rem user-specific-url + email login + basic auth + get-read
rem - https://testoneitofinity@bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk


rem user-specific-url + username login + oauth auth + get-prompt
rem - https://testtwoitofinity@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem user-specific-url + username login + oauth auth + get-read
rem - https://testtwoitofinity@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem user-specific-url + email login + oauth auth + get-prompt
rem - https://testtwoitofinity@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk
rem user-specific-url + email login + oauth auth + get-read
rem - https://testtwoitofinity@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk

:basic-username-peruserurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_USERURL%
set URLTYPE=peruserurl
set LOGIN=username
set AUTH=basic
set USER=%BASICAUTH_ACCOUNTNAME%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:basic-email-peruserurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_USERURL%
set URLTYPE=peruserurl
set LOGIN=email
set AUTH=basic
set USER=%BASICAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents... 
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.echo checking Windows Vault entries...
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-username-peruserurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.
set CLONE_URL=%OAUTH_USERURL%
set URLTYPE=peruserurl
set LOGIN=username
set AUTH=oauth
set USER=%OAUTH_ACCOUNTNAME%
echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-email-peruserurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%OAUTH_USERURL%
set URLTYPE=peruserurl
set LOGIN=email
set AUTH=oauth
set USER=%OAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.
echo checking Windows Vault entries...
set CRED_KEY=git:https://%OAUTH_ACCOUNTNAME%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%OAUTH_ACCOUNTNAME%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

rem useremail-specific-url + username login + basic auth + get-prompt
rem - https://testoneitofinity%40itofinity.co.uk@bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem useremail-specific-url + username login + basic auth + get-read
rem - https://testoneitofinity%40itofinity.co.uk@bitbucket.org/testoneitofinity/git-test-private.git
rem - testoneitofinity
rem useremail-specific-url + email login + basic auth + get-prompt
rem - https://testoneitofinity%40itofinity.co.uk@bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk
rem useremail-specific-url + email login + basic auth + get-read
rem - https://testoneitofinity%40itofinity.co.uk@bitbucket.org/testoneitofinity/git-test-private.git
rem - testone@itofinity.co.uk


rem useremail-specific-url + username login + oauth auth + get-prompt
rem - https://testtwo%40itofinity.co.uk@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem useremail-specific-url + username login + oauth auth + get-read
rem - https://testtwo%40itofinity.co.uk@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwoitofinity
rem useremail-specific-url + email login + oauth auth + get-prompt
rem - https://testtwo%40itofinity.co.uk@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk
rem useremail-specific-url + email login + oauth auth + get-read
rem - https://testtwo%40itofinity.co.uk@bitbucket.org/testtwoitofinity/git-test-private.git
rem - testtwo@itofinity.co.uk

:basic-username-peruseremailurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_USEREMAILURL%
set URLTYPE=peruseremailurl
set LOGIN=username
set AUTH=basic
set USER=%BASICAUTH_ACCOUNTNAME%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:basic-email-peruseremailurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%BASICAUTH_USEREMAILURL%
set URLTYPE=peruseremailurl
set LOGIN=email
set AUTH=basic
set USER=%BASICAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents... 
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-username-peruseremailurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.
set CLONE_URL=%OAUTH_USEREMAILURL%
set URLTYPE=peruseremailurl
set LOGIN=username
set AUTH=oauth
set USER=%OAUTH_ACCOUNTNAME%
echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause
IF NOT "%1"=="" GOTO EOF

rem NEXT TEST

:oauth-email-peruseremailurl
echo.
echo.
echo Clear 'git:' credentials
cmdkey.exe /list > "%TEMP%\List.txt"
findstr.exe git: "%TEMP%\List.txt" > "%TEMP%\tokensonly.txt"
FOR /F "tokens=1,2 delims= " %%G IN (%TEMP%\tokensonly.txt) DO cmdkey.exe /delete:%%H
del "%TEMP%\List.txt" /s /f /q
del "%TEMP%\tokensonly.txt" /s /f /q
echo.

set CLONE_URL=%OAUTH_USEREMAILURL%
set URLTYPE=peruseremailurl
set LOGIN=email
set AUTH=oauth
set USER=%OAUTH_USEREMAIL%

echo TEST TEST TEST
echo %URLTYPE% + %LOGIN% + %AUTH% using [%USER%]
echo.
pause
set PROMPT_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getprompt
set READ_TARGET=.\gcm_test\%URLTYPE%_%LOGIN%_%AUTH%_getread
echo git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
git clone --quiet  %CLONE_URL% %PROMPT_TARGET%
echo git clone --quiet  %CLONE_URL% %READ_TARGET%
git clone --quiet  %CLONE_URL% %READ_TARGET%
echo checking clone contents...
set CLONE_ERROR=true
for /F %%i in ('dir /b "%PROMPT_TARGET%\*.*"') do (
   set CLONE_ERROR=false 
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %PROMPT_TARGET% Clone Failed!
set CLONE_ERROR=true
for /F %%i in ('dir /b "%READ_TARGET%\*.*"') do (
   set CLONE_ERROR=false
)
IF "%CLONE_ERROR%"=="true" ECHO ERROR %READ_TARGET% Clone Failed!
echo.echo checking Windows Vault entries...
set CRED_KEY=git:https://bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
set CRED_KEY=git:https://%USER%@bitbucket.org/refresh_token
cmdkey /list | findstr %CRED_KEY% > cred.txt
for /f %%i in ("cred.txt") do set size=%%~zi
if NOT %size% gtr 0 echo ERROR Could not find %CRED_KEY%
del cred.txt
pause


:EOF