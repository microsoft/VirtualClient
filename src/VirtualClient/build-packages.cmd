@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set ExitCode=0
set PackageVersion=%~1
set PackageDir=%~dp0VirtualClient.Packaging
set PackagesProject=%~dp0VirtualClient.Packaging\VirtualClient.Packaging.csproj

if /i "%PackageVersion%" == "" (
    set ExitCode=1

    echo:
    echo Invalid Usage. The packages version must be provided on the command line.
    Goto :Usage
)

rem The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
rem the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
call dotnet restore %PackagesProject% --force

echo:
echo [Creating NuGet Package: VirtualClient]
echo --------------------------------------------------
call dotnet pack %PackagesProject% --force --no-restore --no-build -c Debug -p:Version=%PackageVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.nuspec && echo: || Goto :Error

echo:
echo [Creating NuGet Package: VirtualClient Framework]
echo --------------------------------------------------
call dotnet pack %PackagesProject%  --force --no-restore --no-build -c Debug -p:Version=%PackageVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.Framework.nuspec && echo: || Goto :Error

echo:
echo [Creating NuGet Package: VirtualClient Test Framework]
echo --------------------------------------------------
call dotnet pack %PackagesProject%  --force --no-restore --no-build -c Debug -p:Version=%PackageVersion% -p:NuspecFile=%PackageDir%\nuspec\VirtualClient.TestFramework.nuspec && echo: || Goto :Error

Goto :End


:Usage
echo:
echo Usage:
echo %~0 {packageVersion}
echo:
echo Examples:
echo %~0 1.0.1485.571
Goto :End


:Error
set ExitCode=%ERRORLEVEL%


:End
rem Reset environment variables
set PackageDir=
set PackageVersion=
set PackagesProject=

exit /B %ExitCode%