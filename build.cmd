@echo Off

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%
set BUILD_CONFIGURATION=
set BUILD_FLAGS=
set BUILD_VERSION=
set BUILD_ALL=true
set BUILD_LINUX_X64=false
set BUILD_LINUX_ARM64=false
set BUILD_WIN_X64=false
set BUILD_WIN_ARM64=false
set BUILD_SDK_AGENT=false
set VC_SOLUTION_DIR=%SCRIPT_DIR%\src\VirtualClient

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

for %%a in (%*) do (
    if /i "%%a" == "--trim" set BUILD_FLAGS=-p:PublishTrimmed=true

    if /i "%%a" == "--linux-x64" (
        set BUILD_LINUX_X64=true
        set BUILD_ALL=false
    )

    if /i "%%a" == "--linux-arm64" (
        set BUILD_LINUX_ARM64=true
        set BUILD_ALL=false
    )

    if /i "%%a" == "--win-x64" (
        set BUILD_WIN_X64=true
        set BUILD_ALL=false
    )

    if /i "%%a" == "--win-arm64" (
        set BUILD_WIN_ARM64=true
        set BUILD_ALL=false
    )

    if /i "%%a" == "+sdk-agent" (
        set BUILD_SDK_AGENT=true
    )

    if /i "%%a" == "#sdk-agent" (
        set BUILD_SDK_AGENT=true,only
    )
)

rem The default build version is defined in the repo VERSION file.
set /p BUILD_VERSION=<%SCRIPT_DIR%\VERSION

if defined VCBuildVersion (
    echo:
    echo Using 'VCBuildVersion' = %VCBuildVersion%
    set BUILD_VERSION=%VCBuildVersion%
)

set BUILD_CONFIGURATION=Release

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
REM We build the AnyCPU .dlls with Debug configuration to support extensions debugging for
REM external users and partner teams.
echo:
echo !!!
echo !!! Note that the solution is be built with 'Debug' configuration in order to support a better debugging experience for extensions development.
echo !!!
echo:
call dotnet build "%VC_SOLUTION_DIR%\VirtualClient.sln" -c Debug -v Detailed ^
-p:AssemblyVersion=%BUILD_VERSION% && echo: || Goto :Error

if /i "%BUILD_ALL%" == "true" (
    set BUILD_LINUX_X64=true
    set BUILD_LINUX_ARM64=true
    set BUILD_WIN_X64=true
    set BUILD_WIN_ARM64=true
)

if /i "%BUILD_LINUX_X64%" == "true" (

    if /i not "%BUILD_SDK_AGENT%" == "true,only" (
        echo:
        echo [Build Virtual Client: linux-x64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-x64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error
    )

    if /i "%BUILD_SDK_AGENT:~0,4%" == "true" (
        echo:
        echo [Build SDK Agent: linux-x64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.SdkAgent.Main\VirtualClient.SdkAgent.Main.csproj" -r linux-x64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error
    )
)

if /i "%BUILD_LINUX_ARM64%" == "true" (

    if /i not "%BUILD_SDK_AGENT%" == "true,only" (
        echo:
        echo [Build Virtual Client: linux-arm64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r linux-arm64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error
    )

    if /i "%BUILD_SDK_AGENT:~0,4%" == "true" (
        echo:
        echo [Build SDK Agent: linux-arm64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.SdkAgent.Main\VirtualClient.SdkAgent.Main.csproj" -r linux-arm64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% -p:InvariantGlobalization=true %BUILD_FLAGS% && echo: || Goto :Error
    )
)

if /i "%BUILD_WIN_X64%" == "true" (

    if /i not "%BUILD_SDK_AGENT%" == "true,only" (
        echo:
        echo [Build Virtual Client: win-x64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-x64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error
    )

    if /i "%BUILD_SDK_AGENT:~0,4%" == "true" (
        echo:
        echo [Build SDK Agent: win-x64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.SdkAgent.Main\VirtualClient.SdkAgent.Main.csproj" -r win-x64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error
    )
)

if /i "%BUILD_WIN_ARM64%" == "true" (

    if /i not "%BUILD_SDK_AGENT%" == "true,only" (
        echo:
        echo [Build Virtual Client: win-arm64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.Main\VirtualClient.Main.csproj" -r win-arm64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error
    )

    if /i "%BUILD_SDK_AGENT:~0,4%" == "true" (
        echo:
        echo [Build SDK Agent: win-arm64]
        echo ----------------------------------------------------------------------
        call dotnet publish "%VC_SOLUTION_DIR%\VirtualClient.SdkAgent.Main\VirtualClient.SdkAgent.Main.csproj" -r win-arm64 -c %BUILD_CONFIGURATION% -v Detailed --self-contained ^
        -p:AssemblyVersion=%BUILD_VERSION% %BUILD_FLAGS% && echo: || Goto :Error
    )
)

Goto :End

:Usage
echo:
echo:
echo Builds the source code in the repo. 
echo:
echo Usage:
echo ---------------------
echo build.cmd [--win-x64] [--win-arm64] [--linux-x64] [--linux-arm64] [+sdk-agent^|#sdk-agent] [--trim]
echo:
echo Examples:
echo ---------------------
echo # Build all targets:
echo %SCRIPT_DIR%^> build.cmd
echo:
echo # Build with specific version and Debug configuration:
echo set VCBuildVersion=2.1.0
echo set VCBuildConfiguration=Debug
echo %SCRIPT_DIR%^> build.cmd
echo:
echo # Build only for Windows x64:
echo %SCRIPT_DIR%^> build.cmd --win-x64
echo:
echo # Build for Linux ARM64 and Windows x64:
echo %SCRIPT_DIR%^> build.cmd --linux-arm64 --win-x64
echo:
echo # Include SDK Agent builds:
echo %SCRIPT_DIR%^> build.cmd +sdk-agent
echo:
echo # Build the SDK Agent only:
echo %SCRIPT_DIR%^> build.cmd #sdk-agent
echo:
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
set BUILD_ALL=
set BUILD_LINUX_X64=
set BUILD_LINUX_ARM64=
set BUILD_WIN_X64=
set BUILD_WIN_ARM64=
set BUILD_SDK_AGENT=

echo Build Stage Exit Code: %EXIT_CODE%

:Finish
exit /B %EXIT_CODE%
