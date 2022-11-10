@echo Off
REM This script is used to build Virtual Client.

set ExitCode=0
set OutputDirectory=%~dp0..\..\out\tools

set TrimFlag="-p:PublishTrimmed=true -p:TrimUnusedDependencies=true"
if /i "%~1" == "noTrim" set TrimFlag=""

rem Robocopy exit codes that mean success
rem 
rem https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy
rem 0 = No files were copied. No failure was encountered. No files were mismatched. The files already exist 
rem     in the destination directory; therefore, the copy operation was skipped.
rem 1 = All files were copied successfully.
rem 2 = There are some additional files in the destination directory that are not present in the source directory. 
rem     No files were copied.
rem 3 = Some files were copied. Additional files were present. No failure was encountered.
rem 5 = Some files were copied. Some files were mismatched. No failure was encountered.
rem 6 = Additional files and mismatched files exist. No files were copied and no failures were encountered. This 
rem     means that the files already exist in the destination directory.
rem 7 = Files were copied, a file mismatch was present, and additional files were present.

echo [Copying External Tools to Packaging Location]
call robocopy %~dp0VirtualClient.Packaging\any %OutputDirectory%\any /mir /ns /nc /njh /nfl /ndl
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

call robocopy %~dp0VirtualClient.Packaging\linux-arm64 %OutputDirectory%\linux-arm64 /mir /ns /nc /njh /nfl /ndl
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

call robocopy %~dp0VirtualClient.Packaging\linux-x64 %OutputDirectory%\linux-x64 /mir /ns /nc /njh /nfl /ndl
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

call robocopy %~dp0VirtualClient.Packaging\win-arm64 %OutputDirectory%\win-arm64 /mir /ns /nc /njh /nfl /ndl
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

call robocopy %~dp0VirtualClient.Packaging\win-x64 %OutputDirectory%\win-x64 /mir /ns /nc /njh /nfl /ndl
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

call robocopy %~dp0VirtualClient.Packaging %OutputDirectory% /ns /nc /njh /nfl /ndl *.vcpkg
echo Robocopy Exit Code = %ERRORLEVEL%
if %ERRORLEVEL% GEQ 8 goto :Error

Goto :End

:Error
set ExitCode=%ERRORLEVEL%

:End
set OutputDirectory=
set TrimFlag=
exit /B %ExitCode%