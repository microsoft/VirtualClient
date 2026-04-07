@echo Off
setlocal enabledelayedexpansion

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%
set START_PORT=10000
set END_PORT=60000
set NUM_PORTS=50000

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

:Parse
if "%~1" == "" (
    Goto :EndParse
)

rem Remove quotes for processing
set "current_arg=%~1"

rem Check if this arg starts with --port-range
echo !current_arg! | findstr /i /b /c:"--port-range" >nul
if !errorlevel! == 0 (
    rem Split the string at the '=' sign
    rem "tokens=2 delims==" picks everything after the first =
    for /f "tokens=2 delims==" %%v in ("!current_arg!") do (
        set "raw_values=%%v"
        
        rem Strip any remaining quotes from the values (200 500)
        set "raw_values=!raw_values:"=!"
        
        rem Split the two numbers apart
        for /f "tokens=1,2" %%a in ("!raw_values!") do (
            set "START_PORT=%%a"
            set "END_PORT=%%b"
        )

        set /a NUM_PORTS=!END_PORT! - !START_PORT!
    )
)

shift
Goto :Parse

:EndParse

rem Validation
if "%START_PORT%" == "" (
    echo ERROR: --port-range requires two values, e.g. "--port-range=10000 60000"
    Goto :Error
)

if "%END_PORT%" == "" (
    echo ERROR: --port-range requires two values, e.g. "--port-range=10000 60000"
    Goto :Error
)

echo:
echo CONFIGURE NETWORK
echo **********************************************************************
echo Ephemeral Port Range : %START_PORT% %END_PORT%
echo Dynamic Port Count   : %NUM_PORTS%
echo Script Directory     : %SCRIPT_DIR%
echo **********************************************************************

call netsh int ipv4 set dynamicport tcp start=%START_PORT% num=%NUM_PORTS% && echo: || Goto :Error
call netsh int ipv4 show dynamicport tcp && echo: || Goto :Error

Goto :End

:Usage
echo:
echo:
echo Sets network settings on the local system.
echo:
echo Usage:
echo ---------------------
echo config_network.cmd "[--port-range=<start end>]"
echo:
echo Examples:
echo ---------------------
echo %SCRIPT_DIR%^> config_network.cmd
echo:
echo %SCRIPT_DIR%^> config_network.cmd "--port-range=10000 50000"
echo:
Goto :Finish

:Error
set EXIT_CODE=%ERRORLEVEL%

:End
rem Reset environment variables
set SCRIPT_DIR=
set START_PORT=
set END_PORT=
set NUM_PORTS=

echo Exit Code: %EXIT_CODE%

:Finish
exit /B %EXIT_CODE%
