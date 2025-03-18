@echo Off

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%
set BUILD_CONFIGURATION=
set BUILD_FLAGS=-p:PublishTrimmed=true
set BUILD_FLAGS=
set BUILD_VERSION=
set VC_SOLUTION_DIR=%SCRIPT_DIR%\src\VirtualClient

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

for %%a in (%*) do (

    rem Pass in the --notrim flag to opt out of trimming the project 
    rem assemblies during build.
    if /i "%%a" == "--trim" set BUILD_FLAGS=-p:PublishTrimmed=true
)

rem The default build version is defined in the repo VERSION file.
set /p BUILD_VERSION=<%SCRIPT_DIR%\VERSION

rem The default build version can be overridden by the 'VCBuildVersion' 
rem environment variable
if defined VCBuildVersion (
    echo:
    echo Using 'VCBuildVersion' = %VCBuildVersion%
    set BUILD_VERSION=%VCBuildVersion%
)

rem The default build configuration is 'Release'.
set BUILD_CONFIGURATION=Release

rem The default build configuration (e.g. Release) can be overridden 
rem by the 'VCBuildConfiguration' environment variable
if defined VCBuildConfiguration (
    echo:
    echo Using 'VCBuildConfiguration' = %VCBuildConfiguration%
    set BUILD_CONFIGURATION=%VCBuildConfiguration%
)

echo:
echo **********************************************************************
echo Build Version : %BUILD_VERSION%
echo Repo Root     : %SCRIPT_DIR%
echo Configuration : %BUILD_CONFIGURATION%
echo Flags         : %BUILD_FLAGS%
echo **********************************************************************

echo:
echo [Build Solution]
echo -------------------------------------------------------
call dotnet build "%VC_SOLUTION_DIR%\VirtualClient.sln" -c %BUILD_CONFIGURATION% ^
-p:AssemblyVersion=%BUILD_VERSION% && echo: || Goto :Error

echo:
echo [Build Virtual Client: linux-x64]"
echo ----------------------------------------------------------------------
call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-x64 -c %BUILD_CONFIGURATION% --self-contained ^
-p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error

echo:
echo [Build Virtual Client: linux-arm64]"
echo ----------------------------------------------------------------------
call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-arm64 -c %BUILD_CONFIGURATION% --self-contained ^
-p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error

echo:
echo [Build Virtual Client: win-x64]"
echo ----------------------------------------------------------------------
call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-x64 -c %BUILD_CONFIGURATION% --self-contained ^
-p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error

echo:
echo [Build Virtual Client: win-arm64]"
echo ----------------------------------------------------------------------
call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-arm64 -c %BUILD_CONFIGURATION% --self-contained ^
-p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error

Goto :End


:Usage
echo:
echo:
echo Builds the source code in the repo. 
echo:
echo Usage:
echo ---------------------
echo build.cmd
echo:
echo Examples:
echo ---------------------
echo # Use defaults
echo %SCRIPT_DIR%^> build.cmd
echo:
echo # Set specific version and configuration
echo %SCRIPT_DIR%^> set VCBuildVersion=1.16.25
echo %SCRIPT_DIR%^> set VCBuildConfiguration=Debug
echo %SCRIPT_DIR%^> build.cmd
Goto :Finish


:Error
set EXIT_CODE=%ERRORLEVEL%


:End
REM Reset environment variables
set BUILD_FLAGS=
set BUILD_CONFIGURATION=
set BUILD_VERSION=
set SCRIPT_DIR=
set VC_SOLUTION_DIR=

echo Build Stage Exit Code: %EXIT_CODE%


:Finish
exit /B %EXIT_CODE%