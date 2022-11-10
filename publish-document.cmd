@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage
if /i "%~1" == "" Goto :Usage
if /i "%~2" == "" Goto :Usage

set ExitCode=0
set GIT_USER=%~1
set GIT_PASS=%~2

echo:
echo [Uploading document to GitHub page]
CHDIR docs
echo %cd%
echo --------------------------------------------------

call yarn deploy && echo: || Goto :Error


Goto :End


:Usage
echo Invalid Usage. 
echo Usage:
echo %~0 {username} {github PAT}
echo:
echo Examples:
echo %~0 yangpanMS pat123
Goto :End


:Error
set ExitCode=%ERRORLEVEL%
echo If you see errors redirecting you to url like https://github.com/enterprises/microsoftopensource/sso
echo You need to go to that link and agree to PAT SAML SSO. This is one-time action.


:End
rem Reset environment variables
CHDIR ..
set GIT_USER=
set GIT_PASS=

echo Exit/Error Code: %ExitCode%
exit /B %ExitCode%