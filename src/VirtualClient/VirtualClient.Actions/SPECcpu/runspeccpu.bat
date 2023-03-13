@if not defined SPEC_BATCH_DEBUG echo off
rem
rem shrc.bat
rem
rem   This file sets up your path and other environment variables for SPEC CPU
rem
rem   --> YOU MUST EDIT THIS FILE BEFORE USING IT <--
rem
rem   SPEC CPU benchmarks are supplied as source code (C, C++, Fortran).
rem   They must be compiled before they are run.
rem
rem    1. If someone else compiled the benchmarks, you need
rem       to edit just one line in this file.  To find it,
rem       search for:
rem                    "Already Compiled"
rem
rem    2. If you are compiling the benchmarks, you need to change
rem       two parts of this file.  Search for *all* the places
rem       that say:
rem                    "My Compiler"
rem
rem    Usage: do the edits, then:
rem           cd <specroot>
rem           shrc
rem
rem Authors: J.Henning, Cloyce Spradling
rem
rem Copyright 1999-2017 Standard Performance Evaluation Corporation
rem
rem $Id: shrc.bat 6467 2020-08-17 17:57:44Z CloyceS $
rem ---------------------------------------------------------------

call :clean_shrc_vars

rem ================
rem Already Compiled
rem ================
rem        If someone else has compiled the benchmarks, then the
rem        only change you need to make is to uncomment the line
rem        that follows - just remove the word 'rem'

rem set SHRC_PRECOMPILED=yes

rem ==================
rem My Compiler Part 1
rem ==================
rem      If you are compiling the benchmarks, you need to *both*
rem       - set your path, at the section marked "My Compiler Part 2"
rem       - *and* uncomment the next line (remove the word "rem")

set SHRC_COMPILER_PATH_SET=yes

rem ---------------------------------------------------------------
rem
if defined SHRC_COMPILER_PATH_SET goto SHRC_compiler_path_set
if defined SHRC_PRECOMPILED       goto SHRC_compiler_path_set
echo Please read and edit shrc.bat before attempting to execute it!
goto :EOF

:SHRC_compiler_path_set
call :clean_shrc_vars

rem ==================
rem My Compiler Part 2
rem ==================
rem  A few lines down (at "BEGIN EDIT HERE"), insert commands that
rem  define the path to your compiler.  There are two basic options:
rem     - Option A (usually better): call a vendor-supplied batch file, or
rem     - Option B: directly use the "set" command.
rem
rem  WARNING: Do not assume that examples below will work as is.
rem  These files change frequently.  Use the examples to help you
rem  understand what to look for in your compiler documentation.
rem
rem  Option A.  Examples of vendor path .bat files:
rem    call "C:\Program Files (x86)\IntelSWTools\compilers_and_libraries_2017\windows\bin\compilervars.bat" intel64 vs2015
rem    call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat" amd64
rem    call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\amd64\vcvars64.bat"
rem    call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\vcvars32.bat"
rem    call "c:\Program Files\PGI\win64\16.5\pgi_env.bat"
rem    call "c:\Program Files (x86)\PGI\win32\16.5\pgi_env.bat"
rem
rem  Option B.  Examples of setting the path directly:
set PATH=%PATH%;"C:\tools\cygwin\bin"
rem    set PATH=%PATH%;"c:\program files\microsoft visual studio\df98\bin"
rem  Note that you may also need to set other variables, such as LIB and
rem  INCLUDE.  Check your compiler documentation.
rem
rem XXXXXXXX BEGIN EDIT HERE XXXXXXXXXXX
rem   Call .bat or set PATH here.  Warning: no semicolons inside quotes!
rem   https://www.spec.org/cpu2017/docs/faq.html#runcpu.02
rem XXXXXXXX END EDIT HERE XXXXXXXXXXX

call %~dp0\bin\windows\setspecvars.bat
if errorlevel 1 goto :DONE

if defined SHRC_QUIET goto :DONE
rem    Finally, let's print all this in a way that makes sense.
rem    While we're at it, this is a good little test of whether
rem    specperl is working for you!

echo.
echo SPECPERLLIB set to %SPECPERLLIB%
echo SPEC        set to %SPEC%

%~dp0\bin\windows\printpath.pl.bat

:DONE
exit /B %errorlevel%

rem ---------- Subroutines ------------

:clean_shrc_vars
set SHRC_PRECOMPILED=
set SHRC_COMPILER_PATH_SET=
goto :EOF

rem Editor settings: (please leave this at the end of the file)
rem vim: set filetype=dosbatch syntax=dosbatch shiftwidth=4 tabstop=8 expandtab nosmarttab:

runcpu %1