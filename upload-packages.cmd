@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage
if /i "%~1" == "" Goto :Usage
if /i "%~2" == "" Goto :Usage

set ExitCode=0
set PackageDirectory=%~1

echo:
echo [Uploading NuGet Packages]
echo --------------------------------------------------
echo Package Directory : %PackageDirectory%
echo Feed              : %FeedUri%
echo:

for %%f in (%PackageDirectory%\*.nupkg) do (
    call dotnet nuget push %%f --api-key %~2 --timeout 1200 --source https://api.nuget.org/v3/index.json  %~3 && echo: || Goto :Error
)

Goto :End


:Usage
echo Invalid Usage. 
echo Usage:
echo %~0 {packageDirectory} {nugetApiKey} [{dotnet push args}]
echo:
echo Examples:
echo %~0 S:\source\one\repo\out\bin\Release\x64\Packages apikey
echo %~0 S:\source\one\repo\out\bin\Release\x64\Packages apikey --interactive
Goto :End


:Error
set ExitCode=%ERRORLEVEL%


:End
rem Reset environment variables
set PackageDirectory=
set FeedUri=

echo Build Stage Exit/Error Code: %ExitCode%
exit /B %ExitCode%