@echo Off
REM This script is used to build Virtual Client.

set ExitCode=0

rem The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
rem the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
call dotnet restore %~dp0VirtualClient.ExampleWorkload.csproj --force

echo:
echo [Creating Package: Example Workload]
echo --------------------------------------------------
call dotnet pack %~dp0VirtualClient.ExampleWorkload.csproj --force --no-restore --no-build -c Debug -p:NuspecFile=%~dp0\exampleworkload.nuspec && echo: || Goto :Error

Goto :End

:Error
set ExitCode=%ERRORLEVEL%

:End
exit /B %ExitCode%