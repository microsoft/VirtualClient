@echo Off

set ExitCode=0

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

REM The "VCBuildVersion" environment variable is referenced by the MSBuild processes during build.
REM All binaries will be compiled with this version (e.g. .dlls + .exes). The packaging process uses 
REM the same environment variable to define the version of the NuGet package(s) produced. The build 
REM version can be overridden on the command line.
if /i NOT "%~1" == "" (
    set VCBuildVersion=%~1
)

if /i "%VCBuildVersion%" == "" (
    set VCBuildVersion=0.0.1.0
)

set VCSolutionDir=%~dp0src\VirtualClient
set PackageDir=%VCSolutionDir%\VirtualClient.Packaging
set PackagesProject=%VCSolutionDir%\VirtualClient.Packaging\VirtualClient.Packaging.csproj


REM The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
REM the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
call dotnet restore %PackagesProject% --force

echo:
echo [Create NuGet Package] VirtualClient %VCBuildVersion%
echo --------------------------------------------------
call dotnet pack %PackagesProject% --force --no-restore --no-build -c Release -p:Version=%VCBuildVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.nuspec && echo: || Goto :Error

echo:
echo [Create NuGet Package] VirtualClient.Framework %VCBuildVersion%
echo --------------------------------------------------
call dotnet pack %PackagesProject%  --force --no-restore --no-build -c Release -p:Version=%VCBuildVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.Framework.nuspec && echo: || Goto :Error

echo:
echo [Create NuGet Package] VirtualClient.TestFramework %VCBuildVersion%
echo --------------------------------------------------
call dotnet pack %PackagesProject%  --force --no-restore --no-build -c Release -p:Version=%VCBuildVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.TestFramework.nuspec && echo: || Goto :Error
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
echo # Build packages using default build version
echo %~0
echo:
echo # Pass the build/package version into the command
echo %~0 1.0.1485.571
echo:
echo # Set the build/package version in an environment variable, then build
echo set VCBuildVersion=1.0.1485.571
echo %~0
echo:
Goto :Finish

:Error
set ExitCode=%ERRORLEVEL%

:End
REM Reset environment variables
set PackageDir=
set PackagesProject=
set VCSolutionDir=
echo Packaging Stage Exit Code: %ExitCode%

:Finish
exit /B %ExitCode%