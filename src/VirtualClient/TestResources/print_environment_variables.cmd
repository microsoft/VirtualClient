@echo Off

REM Show all environment variables
REM This is largely used for testing purposes and should not be used in production environments.

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

echo:
echo Showing environment variables...
set

call "%SCRIPT_DIR%\print_environment_variables_2.cmd"

exit /b %EXIT_CODE%