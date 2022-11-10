@echo Off
SETLOCAL EnableDelayedExpansion

set RepoDir=%~dp0

if exist %RepoDir%out\bin (
    echo:
    echo [Clean: Binaries^(bin^) Directories]
    call rmdir /Q /S %RepoDir%out\bin && echo Deleted: %RepoDir%out\bin || goto :Error
)

if exist %RepoDir%out\obj (
    echo:
    echo [Clean: Intermediates^(obj^) Directories]
    call rmdir /Q /S %RepoDir%out\obj && echo Deleted: %RepoDir%out\obj || goto :Error
)

call dir /S /A:d /B %RepoDir%src\*obj 1>nul 2>nul && (
    for /F "delims=" %%d in ('dir /S /A:d /B %RepoDir%src\*obj') do (
        rem Step through all subdirectories of the /src directory to find
        rem /obj directories within individual projects. These contain various types
        rem of intermediates related to NuGet package caches that we want to clean.
        call rmdir /Q /S %%d && echo Deleted: %%d || goto :Error
    )
)

if exist %RepoDir%out\packages (
    echo:
    echo [Clean: Packages^(packages^) Directories]
    call rmdir /Q /S %RepoDir%out\packages && echo Deleted: %RepoDir%out\packages || goto :Error
)

goto :End


:Error
set RepoDir=
echo Clean Stage Exit/Error Code: %ERRORLEVEL%
exit /B %ERRORLEVEL%


:End
set RepoDir=
echo Clean Stage Exit/Error Code: 0
exit /B 0
