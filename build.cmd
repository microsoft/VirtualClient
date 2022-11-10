@echo Off

Set ExitCode=0

if "%CDP_FILE_VERSION_NUMERIC%" NEQ "" (
    set VCBuildVersion=%CDP_FILE_VERSION_NUMERIC%
)

echo:
echo [Building Solutions]
echo VirtualClient Version: %VCBuildVersion%

call dotnet build %~dp0src\VirtualClient\VirtualClient.sln -c Debug && echo: || Goto :Error
call %~dp0src\VirtualClient\build.cmd && echo: || Goto :Error

echo [Copy .artifactignore to Output]
echo -------------------------------------------------------
call robocopy %~dp0 %~dp0out *.artifactignore
if %ERRORLEVEL% GEQ 8 goto :Error
Goto :End


:Error
set ExitCode=%ERRORLEVEL%


:End

echo Build Stage Exit/Error Code: %ExitCode%
exit /B %ExitCode%