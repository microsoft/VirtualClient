@echo Off
setlocal EnableDelayedExpansion

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set EXIT_CODE=0
set REPO_DIR=%~dp0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%

if exist %REPO_DIR%out\bin (
    echo:
    echo [Clean: Binaries^(bin^) Directories]
    call rmdir /Q /S %REPO_DIR%out\bin && echo Deleted: %REPO_DIR%out\bin || Goto :Error
)

if exist %REPO_DIR%out\obj (
    echo:
    echo [Clean: Intermediates^(obj^) Directories]
    call rmdir /Q /S %REPO_DIR%out\obj && echo Deleted: %REPO_DIR%out\obj || Goto :Error
)

call dir /S /A:d /B %REPO_DIR%src\*obj 1>nul 2>nul && (
    for /F "delims=" %%d in ('dir /S /A:d /B %REPO_DIR%src\*obj') do (
        rem Step through all subdirectories of the /src directory to find
        rem /obj directories within individual projects. These contain various types
        rem of intermediates related to NuGet package caches that we want to clean.
        call rmdir /Q /S %%d && echo Deleted: %%d || Goto :Error
    )
)

if exist %REPO_DIR%out\packages (
    echo:
    echo [Clean: Packages^(packages^) Directories]
    call rmdir /Q /S %REPO_DIR%out\packages && echo Deleted: %REPO_DIR%out\packages || Goto :Error
)

Goto :End


:Usage
echo:
echo:
echo Deletes build artifacts from the repo.
echo:
echo Usage:
echo ---------------------
echo clean.cmd
echo:
echo Examples
echo ---------------------
echo %SCRIPT_DIR%^> clean.cmd
echo:
Goto :Finish


:Error
set EXIT_CODE=%ERRORLEVEL%


:End
set REPO_DIR=
set SCRIPT_DIR=

echo Clean Stage Exit Code: 0


:Finish
exit /B %EXIT_CODE%
