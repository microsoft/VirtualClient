@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set ExitCode=0

echo:
echo [Running Tests]
echo --------------------------------------------------

for /f "tokens=*" %%f in ('dir /B /S %~dp0src\*Tests.csproj') do (
    call dotnet test %%f --no-restore --no-build --filter "(Category=Unit|Category=Functional)" --logger "console;verbosity=normal" && echo: || Goto :Error
)

Goto :End


:Usage
echo Invalid Usage.
echo:
echo Usage:
echo %~0
Goto :End


:Error
set ExitCode=%ERRORLEVEL%


:End
set TestResultsPath=
set TargetFramework=
set TestRunSettingsPath=

echo Build Stage Exit/Error Code: %ExitCode%
exit /B %ExitCode%