@echo Off
REM This script is used to build Virtual Client.

set ExitCode=0

echo [Building Example Workload]
echo --------------------------------------------------
call dotnet publish "%~dp0VirtualClient.ExampleWorkload.csproj" -r linux-x64 -c Debug && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.ExampleWorkload.csproj" -r linux-arm64 -c Debug && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.ExampleWorkload.csproj" -r win-x64 -c Debug && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.ExampleWorkload.csproj" -r win-arm64 -c Debug && echo: || Goto :Error

Goto :End

:Error
set ExitCode=%ERRORLEVEL%

:End
exit /B %ExitCode%