@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set ExitCode=0

echo:
echo [Restoring NuGet Packages]
echo --------------------------------------------------
call dotnet restore %~dp0src\VirtualClient\VirtualClient.sln %~1 && echo: || Goto :Error


:Usage
echo Invalid Usage.
echo:
echo Usage:
echo %~0 [--interactive]
Goto :End


:Error
set ExitCode=%ERRORLEVEL%


:End
echo:
echo Restore Stage Exit/Error Code: %ExitCode%
exit /B %ExitCode%