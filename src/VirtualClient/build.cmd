@echo Off
REM This script is used to build Virtual Client.

set ExitCode=0

set TrimFlag="-p:PublishTrimmed=true -p:TrimUnusedDependencies=true"
if /i "%~1" == "noTrim" set TrimFlag=""

call %~dp0build-stage-packaging-tools.cmd && echo: || Goto :Error

echo [Building VirtualClient]
echo -------------------------------------------------------
call dotnet publish "%~dp0VirtualClient.Main\VirtualClient.Main.csproj" -c Debug && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.Main\VirtualClient.Main.csproj" -r linux-x64 -c Debug --self-contained -p:InvariantGlobalization=true %TrimFlag% && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.Main\VirtualClient.Main.csproj" -r linux-arm64 -c Debug --self-contained -p:InvariantGlobalization=true %TrimFlag% && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.Main\VirtualClient.Main.csproj" -r win-x64 -c Debug --self-contained %TrimFlag% && echo: || Goto :Error
call dotnet publish "%~dp0VirtualClient.Main\VirtualClient.Main.csproj" -r win-arm64 -c Debug --self-contained %TrimFlag% && echo: || Goto :Error

Goto :End

:Error
set ExitCode=%ERRORLEVEL%

:End
set TrimFlag=
exit /B %ExitCode%