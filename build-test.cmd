@echo Off

set EXIT_CODE=0
set BUILD_CONFIGURATION=Release
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

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
echo Discovers and runs tests (unit + functional) defined in the build output/artifacts.
echo:
echo Options:
echo ---------------------
echo --debug  - Flag requests tests for build configuration 'Debug' vs. the default 'Release'.
echo:
echo Usage:
echo ---------------------
echo build-test.cmd [--debug]
echo:
echo Examples:
echo ---------------------
echo %SCRIPT_DIR%^> build-test.cmd
echo:
echo %SCRIPT_DIR%^> build-test.cmd --debug
Goto :Finish


:Error
set EXIT_CODE=%ERRORLEVEL%


:End
set BUILD_CONFIGURATION=
set SCRIPT_DIR=
echo Test Stage Exit Code: %EXIT_CODE%


:Finish
exit /B %EXIT_CODE%