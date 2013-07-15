@echo off
setlocal enableextensions enabledelayedexpansion
set ZIP=C:\Program Files\7-Zip\7z.exe
set BUILDDIR=Asim
SET ILMERGE=%ProgramFiles(x86)%\Microsoft\ILMerge\ILMerge.exe

rem date/time
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%b-%%a)
For /f "tokens=1-2 delims=/:" %%a in ("%TIME%") do (set mytime=%%a%%b)
set mytime=%mytime: =0%

rem Dos can't test file dates on two files in different directories...
SET RELEASEDLL=PWC.Asim.Core\bin\Release\PWC.Asim.Core.dll
SET DEBUGDLL=PWC.Asim.Core\bin\Debug\PWC.Asim.Core.dll
echo Please build the RELEASE configuration before building this package.
<nul set /p ="The Release build date is "
for %%a in (%RELEASEDLL%) do echo %%~ta
<nul set /p ="The Debug   build date is "
for %%a in (%DEBUGDLL%) do echo %%~ta
pause

rmdir /s /q Asim
mkdir %BUILDDIR%
mkdir %BUILDDIR%\bin
mkdir %BUILDDIR%\docs
mkdir "%BUILDDIR%\Excel Addin"
mkdir %BUILDDIR%\DropTests

rem bin
rem /targetplatform:"v4,C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0"
echo using ILMerge to build Asim.exe...
"%ILMERGE%" /target:exe /ndebug /targetplatform:"v4,%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /out:%BUILDDIR%\bin\Asim.exe PWC.Asim.ConsoleApp\bin\Release\Asim.exe PWC.Asim.Core\bin\Release\PWC.Asim.Core.dll
xcopy PWC.Asim.ExcelTools\bin\Release\AsimExcelTools.exe %BUILDDIR%\bin
xcopy Algorithms\PWC.Asim.Algorithms.PvNone\bin\Release\PWC.SLMS.Algorithms.PvNone.dll %BUILDDIR%\bin
xcopy Algorithms\PWC.Asim.Algorithms.PvSimple\bin\Release\PWC.SLMS.Algorithms.PvSimple.dll %BUILDDIR%\bin
xcopy Algorithms\PWC.Asim.Algorithms.PvFsc\bin\Release\PWC.Asim.Algorithms.PvFsc.dll %BUILDDIR%\bin

rem docs
xcopy "..\..\..\Documentation\Model User Manual.pdf" %BUILDDIR%\docs
xcopy "..\..\..\Documentation\InstallingMacro\Excel Addin Installation.pdf" %BUILDDIR%\docs

rem Excel Addin
xcopy "Data\Install Addin.bat" "%BUILDDIR%\Excel Addin"
xcopy "Data\Asim Addin.xla" "%BUILDDIR%\Excel Addin"

rem samples
xcopy Data\Example.xlsx %BUILDDIR%
xcopy Data\Example.xls %BUILDDIR%

rem Tests
xcopy /E /I ..\DropTests\bin %BUILDDIR%\DropTests\bin
xcopy /E /I ..\DropTests\tests %BUILDDIR%\DropTests\tests
xcopy ..\DropTests\README.txt %BUILDDIR%\DropTests\
xcopy ..\DropTests\run-tests.sh %BUILDDIR%\DropTests\

rem Source
mkdir %BUILDDIR%\src
robocopy . %BUILDDIR%\src *.cs *.csproj *.sln /S /PURGE /NFL /NDL /XD %BUILDDIR%

set ZIPFILE=Asim-%mydate%_%mytime%.zip
"%ZIP%" a -r "%ZIPFILE%" %BUILDDIR%
rmdir /s /q Asim
pause
