@echo Off

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%
set BUILD_CONFIGURATION=Release
set BUILD_FLAGS=-p:PublishTrimmed=true
set BUILD_VERSION=
set VC_SOLUTION_DIR=%SCRIPT_DIR%\src\VirtualClient

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

for %%a in (%*) do (

    rem Build Configurations:
    rem 1) Release (Default)
    rem 2) Debug
    rem
    rem Pass in the --debug flag to use 'Debug' build configuration
    if /i "%%a" == "--debug" set BUILD_CONFIGURATION=Debug

    rem Pass in the --notrim flag to opt out of trimming the project 
    rem assemblies during build.
    if /i "%%a" == "--notrim" set BUILD_FLAGS=
)

rem The default build version is defined in the repo VERSION file.
set /p BUILD_VERSION=<%SCRIPT_DIR%\VERSION

rem The default build version can be overridden by the 'VCBuildVersion' 
rem environment variable
if defined VCBuildVersion (
    set BUILD_VERSION=%VCBuildVersion%
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
echo Builds the source code in the repo.
echo:
echo Options:
echo ---------------------
echo --debug  - Flag requests build configuration to be 'Debug' vs. the default 'Release'. 
echo:
echo Usage:
echo ---------------------
echo build.cmd [--debug]
echo:
echo Examples:
echo ---------------------
echo %SCRIPT_DIR%^> build.cmd
echo:
echo %SCRIPT_DIR%^> build.cmd --debug
echo:
echo %SCRIPT_DIR%^> set VCBuildVersion=1.16.25
echo %SCRIPT_DIR%^> build.cmd --debug
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