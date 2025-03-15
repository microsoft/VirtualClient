@echo Off

set EXIT_CODE=0
set BUILD_CONFIGURATION=
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

rem The default build configuration is 'Release'.
set BUILD_CONFIGURATION=Release

rem The default build configuration (e.g. Release) can be overridden 
rem by the 'VCBuildConfiguration' environment variable
if defined VCBuildConfiguration (
    echo:
    echo Using 'VCBuildConfiguration' = %VCBuildConfiguration%
    set BUILD_CONFIGURATION=%VCBuildConfiguration%
)

echo ********************************************************************
echo Repo Root       : %SCRIPT_DIR%
echo Configuration   : %BUILD_CONFIGURATION%
echo Tests Directory : %SCRIPT_DIR%\out\bin\%BUILD_CONFIGURATION%
echo ********************************************************************

echo:
echo [Running Tests]
echo --------------------------------------------------

for /f "tokens=*" %%f in ('dir /B /S %~dp0src\*Tests.csproj') do (
    call dotnet test -c %BUILD_CONFIGURATION% %%f --no-restore --no-build --filter "(Category=Unit|Category=Functional)" --logger "console;verbosity=normal" && echo: || Goto :Error
)

Goto :End


:Usage
echo:
echo:
echo Discovers and runs tests (unit + functional) defined in the build output/artifacts.
echo:
echo Usage:
echo ---------------------
echo build-test.cmd
echo:
echo Examples:
echo ---------------------
echo # Use defaults
echo %SCRIPT_DIR%^> build.cmd
echo %SCRIPT_DIR%^> build-test.cmd
echo:
echo # Set specific version and configuration
echo %SCRIPT_DIR%^> set VCBuildVersion=1.16.25
echo %SCRIPT_DIR%^> set VCBuildConfiguration=Debug
echo %SCRIPT_DIR%^> build.cmd
echo %SCRIPT_DIR%^> build-test.cmd
Goto :Finish


:Error
set EXIT_CODE=%ERRORLEVEL%


:End
set BUILD_CONFIGURATION=
set SCRIPT_DIR=
echo Test Stage Exit Code: %EXIT_CODE%


:Finish
exit /B %EXIT_CODE%