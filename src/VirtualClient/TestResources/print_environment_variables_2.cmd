@echo Off

REM Show all environment variables
REM This is largely used for testing purposes and should not be used in production environments.

set EXIT_CODE=0

echo:
echo Showing environment variables...
set

exit /b %EXIT_CODE%