@echo Off
SETLOCAL EnableDelayedExpansion

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set ExitCode=0
set RepoDir=%~dp0

if exist %RepoDir%out\bin (
    echo:
    echo [Clean: Binaries^(bin^) Directories]
    call rmdir /Q /S %RepoDir%out\bin && echo Deleted: %RepoDir%out\bin || Goto :Error
)

if exist %RepoDir%out\obj (
    echo:
    echo [Clean: Intermediates^(obj^) Directories]
    call rmdir /Q /S %RepoDir%out\obj && echo Deleted: %RepoDir%out\obj || Goto :Error
)

call dir /S /A:d /B %RepoDir%src\*obj 1>nul 2>nul && (
    for /F "delims=" %%d in ('dir /S /A:d /B %RepoDir%src\*obj') do (
        rem Step through all subdirectories of the /src directory to find
        rem /obj directories within individual projects. These contain various types
        rem of intermediates related to NuGet package caches that we want to clean.
        call rmdir /Q /S %%d && echo Deleted: %%d || Goto :Error
    )
)

if exist %RepoDir%out\packages (
    echo:
    echo [Clean: Packages^(packages^) Directories]
    call rmdir /Q /S %RepoDir%out\packages && echo Deleted: %RepoDir%out\packages || Goto :Error
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
set RepoDir=
echo Clean Stage Exit Code: 0

:Finish
exit /B %ExitCode%
