@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set ExitCode=0

echo:
echo [Restoring NuGet Packages]
echo --------------------------------------------------
call dotnet restore %~dp0src\VirtualClient\VirtualClient.sln %~1 && echo: || Goto :Error
Goto :End

:Usage
echo:
echo Usage:
echo ---------------------
echo %~0 [--interactive]
echo:
echo:
echo Examples:
echo ---------------------
echo # Restore NuGet packages for all projects
echo %~0
echo:
echo # Restore allow user to provide credentials
echo %~0 --interactive
Goto :Finish

:Error
set ExitCode=%ERRORLEVEL%

:End
echo Restore Stage Exit Code: %ExitCode%

:Finish
exit /B %ExitCode%