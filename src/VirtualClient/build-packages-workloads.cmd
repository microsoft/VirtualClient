@echo Off

set ExitCode=0

if /i "%~1" == "/?" Goto :Usage
if /i "%~1" == "-?" Goto :Usage
if /i "%~1" == "--help" Goto :Usage

set PackageName=%~1
set PackagingDir=%~dp0VirtualClient.Packaging
set PackagingProject=%~dp0VirtualClient.Packaging\VirtualClient.Packaging.csproj

call %~dp0build-stage-packaging-tools.cmd && echo: || Goto :Error

rem The packages project itself is not meant to produce a binary/.dll and thus is not built. However, to ensure
rem the requisite NuGet package assets file exist in the local 'obj' folder, we need to perform a restore.
call dotnet restore %PackagingProject% --force

if /i "%PackageName%" NEQ "" (
    echo:
    echo [Creating NuGet Package: %PackageName% Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\%PackageName%.nuspec && echo: || Goto :Error

) else (
    echo:
    echo [Creating NuGet Package: DiskSpd Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\diskspd.nuspec && echo: || Goto :Error
    
    echo:
    echo [Creating NuGet Package: FIO Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\fio.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: Graph500 Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\graph500.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: LAPACK Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\lapack.nuspec && echo: || Goto :Error
    
    echo:
    echo [Creating NuGet Package: Networking Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\networking.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: OpenFoam Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\openfoam.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: OpenSSL Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\openssl.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: PowerShell7 Runtime]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\powershell7.nuspec && echo: || Goto :Error

    echo:
    echo [Creating NuGet Package: LMbench Workload]
    echo --------------------------------------------------
    call dotnet pack %PackagingProject% --force --no-restore --no-build -c Debug -p:NuspecFile=%PackagingDir%\nuspec\lmbench.nuspec && echo: || Goto :Error
)

Goto :End

:Usage
echo:
echo Usage:
echo %~0 [{packageName}]
echo:
echo Examples:
echo %~0
echo %~0 DiskSpd
Goto :Finish

:Error
set ExitCode=%ERRORLEVEL%

:End
rem Reset environment variables
set PackagingDir=
set PackageName=
set PackagingProject=
set PackagesDir=
echo Packaging Exit Code: %ExitCode%

:Finish
exit /B %ExitCode%