@echo Off

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

Set ExitCode=0

if /i "%CDP_FILE_VERSION_NUMERIC%" == "" (
    set PackageVersion=%~1
) else (
    set PackageVersion=%CDP_FILE_VERSION_NUMERIC%
)

if /i "%PackageVersion%" == "" (
    set ExitCode=1

    echo:
    echo Invalid Usage. The packages version must be provided on the command line.
    Goto :Usage
)

call %~dp0src\VirtualClient\build-packages.cmd %PackageVersion% && echo: || Goto :Error
rem call %~dp0src\VirtualClient\build-packages-workloads.cmd && echo: || Goto :Error
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
set PackageVersion=

echo Package Stage Exit/Error Code: %ExitCode%
exit /B %ExitCode%