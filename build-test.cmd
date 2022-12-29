@echo Off

set ExitCode=0

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

echo:
echo [Running Tests]
echo --------------------------------------------------

for /f "tokens=*" %%f in ('dir /B /S %~dp0src\*Tests.csproj') do (
    call dotnet test %%f --no-restore --no-build --filter "(Category=Unit|Category=Functional)" --logger "console;verbosity=normal" && echo: || Goto :Error
)

Goto :End

:Usage
echo:
echo Usage:
echo ---------------------
echo %~0
Goto :Finish

:Error
set ExitCode=%ERRORLEVEL%

:End
echo Test Stage Exit Code: %ExitCode%

:Finish
exit /B %ExitCode%