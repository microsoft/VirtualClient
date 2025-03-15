@echo Off
setlocal enabledelayedexpansion 

set EXIT_CODE=0
set SCRIPT_DIR=%~dp0
set SCRIPT_DIR=%SCRIPT_DIR:~0,-1%
set BUILD_CONFIGURATION=Release
set BUILD_VERSION=
set PACKAGE_SUFFIX=
set SUFFIX_FOUND=
set TEMP_SUFFIX=

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

for %%a in (%*) do (
    
    if defined SUFFIX_FOUND (
        set SUFFIX_FOUND=!!

        rem Note that we MUST use delayed variable expansion inside of the
        rem 'for' loop here because environment variable are evaluated only once
        rem before the 'for' loop begins otherwise (i.e. vs on each loop).
        set TEMP_SUFFIX=%%a

        :Loop
        if "!TEMP_SUFFIX:~0,1!" == "-" (
            set TEMP_SUFFIX=!TEMP_SUFFIX:~1!
            Goto :Loop
        )

        set PACKAGE_SUFFIX=!TEMP_SUFFIX!

    ) else (
        rem Pass in the --suffix flag to define a suffix for the NuGet package
        rem build artifacts produced (e.g. suffix = -beta -> 1.16.25-beta)
        if /i "%%a" == "--suffix" (
            set SUFFIX_FOUND="true"
        )
    )
)

for %%a in (%*) do (

    rem Build Configurations:
    rem 1) Release (Default)
    rem 2) Debug
    rem
    rem Pass in the --debug flag to use 'Debug' build configuration
    if /i "%%a" == "--debug" (
        set BUILD_CONFIGURATION=Debug
    )
)

rem The default build version is defined in the repo VERSION file.
set /p BUILD_VERSION=<%SCRIPT_DIR%\VERSION

rem The default build version can be overridden by the 'VCBUILD_VERSION' 
rem environment variable
if defined VCBUILD_VERSION (
    set BUILD_VERSION=%VCBUILD_VERSION%
)

set PACKAGE_VERSION=%BUILD_VERSION%
if defined PACKAGE_SUFFIX set PACKAGE_VERSION=%BUILD_VERSION%-%PACKAGE_SUFFIX%

echo:
echo **********************************************************************
echo Build Version   : %BUILD_VERSION%
echo Repo Root       : %SCRIPT_DIR%
echo Configuration   : %BUILD_CONFIGURATION%
echo Package Version : %PACKAGE_VERSION%
echo **********************************************************************

set VC_SOLUTION_DIR=%SCRIPT_DIR%\src\VirtualClient
set PACKAGE_DIR=%VC_SOLUTION_DIR%\VirtualClient.Packaging
set PACKAGES_PROJECT=%VC_SOLUTION_DIR%\VirtualClient.Packaging\VirtualClient.Packaging.csproj

REM The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
REM the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
call dotnet restore %PACKAGES_PROJECT% --force

echo:
echo [Create NuGet Package] VirtualClient.%PACKAGE_VERSION%
echo ----------------------------------------------------------
call dotnet pack %PACKAGES_PROJECT% --force --no-restore --no-build -c Release ^
-p:Version=%PACKAGE_VERSION% -p:NuspecFile=%PACKAGE_DIR%\nuspec\VirtualClient.nuspec && echo: || Goto :Error

echo:
echo [Create NuGet Package] VirtualClient.Framework.%PACKAGE_VERSION%
echo ----------------------------------------------------------
call dotnet pack %PACKAGES_PROJECT%  --force --no-restore --no-build -c Release ^
-p:Version=%PACKAGE_VERSION% -p:NuspecFile=%PACKAGE_DIR%\nuspec\VirtualClient.Framework.nuspec && echo: || Goto :Error

echo:
echo [Create NuGet Package] VirtualClient.TestFramework.%PACKAGE_VERSION%
echo ----------------------------------------------------------
call dotnet pack %PACKAGES_PROJECT%  --force --no-restore --no-build -c Release ^
-p:Version=%PACKAGE_VERSION% -p:NuspecFile=%PACKAGE_DIR%\nuspec\VirtualClient.TestFramework.nuspec && echo: || Goto :Error

Goto :End


:Usage
echo:
echo Creates packages from the build artifacts (e.g. NuGet).
echo:
echo Options:
echo ---------------------
echo --debug   - Flag requests build configuration to be 'Debug' vs. the default 'Release'.
echo --suffix  - Flag requests a suffix be added to the packages produced (e.g. virtualclient.1.16.25-beta).
echo             Valid values include: alpha, beta.
echo:
echo Usage:
echo ---------------------
echo build-packages.cmd [--debug] [--suffix <alpha|beta>]
echo:
echo Examples:
echo ---------------------
echo %SCRIPT_DIR%^> build-packages.cmd
echo:
echo %SCRIPT_DIR%^> build-packages.cmd --debug
echo:
echo %SCRIPT_DIR%^> build-packages.cmd --suffix beta
echo:
echo %SCRIPT_DIR%^> set VCBUILD_VERSION=1.16.25
echo %SCRIPT_DIR%^> build.cmd --debug --suffix beta
Goto :Finish


:Error
set EXIT_CODE=%ERRORLEVEL%


:End
REM Reset environment variables
set BUILD_CONFIGURATION=
set BUILD_VERSION=
set PACKAGE_SUFFIX=
set PACKAGE_VERSION=
set SCRIPT_DIR=
set SUFFIX_FOUND=
set TEMP_SUFFIX=
set VC_SOLUTION_DIR=

echo Packaging Stage Exit Code: %EXIT_CODE%


:Finish
exit /B %EXIT_CODE%