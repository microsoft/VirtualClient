@echo Off

set ExitCode=0

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set "VCBuildVersion="

REM The "VCBuildVersion" environment variable is referenced by the MSBuild processes during build.
REM All binaries will be compiled with this version (e.g. .dlls + .exes). The packaging process uses 
REM the same environment variable to define the version of the NuGet package(s) produced. The build 
REM version can be overridden on the command line.
if /i NOT "%~1" == "" (
    set VCBuildVersion=%~1
)

REM Default version to the VERSION file but append -alpha for manual builds
if /i "%VCBuildVersion%" == "" (
    set /p VCBuildVersion=<VERSION
)

set VCSolutionDir=%~dp0src\VirtualClient

set TrimFlag="-p:PublishTrimmed=true"
set TrimFlag=""
if /i "%~1" == "noTrim" set TrimFlag=""

echo:
echo [Building Source Code] VirtualClient %VCBuildVersion%
echo -------------------------------------------------------
call dotnet build "%VCSolutionDir%\VirtualClient.sln" -c Release && echo: || Goto :Error
call dotnet publish "%VCSolutionDir%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-x64 -c Release --self-contained -p:InvariantGlobalization=true %TrimFlag% && echo: || Goto :Error
call dotnet publish "%VCSolutionDir%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-arm64 -c Release --self-contained -p:InvariantGlobalization=true %TrimFlag% && echo: || Goto :Error
call dotnet publish "%VCSolutionDir%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-x64 -c Release --self-contained %TrimFlag% && echo: || Goto :Error
call dotnet publish "%VCSolutionDir%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-arm64 -c Release --self-contained %TrimFlag% && echo: || Goto :Error
Goto :End

:Usage
echo:
echo Usage:
echo ---------------------
echo %~0
echo %~0 {buildVersion}
echo:
echo:
echo Examples:
echo ---------------------
echo # Build using default build version
echo %~0
echo:
echo # Pass the build version into the command
echo %~0 1.0.1485.571
echo:
echo # Set the build version in an environment variable, then build
echo set VCBuildVersion=1.0.1485.571
echo %~0
Goto :Finish

:Error
set ExitCode=%ERRORLEVEL%

:End
REM Reset environment variables
set TrimFlag=
set VCSolutionDir=
echo Build Stage Exit Code: %ExitCode%

:Finish
exit /B %ExitCode%