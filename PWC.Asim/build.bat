@echo off
set ZIP=C:\Program Files\7-Zip\7z.exe
set BUILDDIR=Asim
SET ILMERGE=%ProgramFiles(x86)%\Microsoft\ILMerge\ILMerge.exe

rem date/time
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%b-%%a)
For /f "tokens=1-2 delims=/:" %%a in ("%TIME%") do (set mytime=%%a%%b)
set mytime=%mytime: =0%

echo make sure you bild the release version manually first!
rmdir /s /q Asim
mkdir %BUILDDIR%
mkdir %BUILDDIR%\bin
mkdir %BUILDDIR%\docs
mkdir "%BUILDDIR%\Excel Addin"
mkdir %BUILDDIR%\DropTests

rem bin
xcopy PWC.Asim.Sim\bin\Release\Asim.exe %BUILDDIR%\bin
xcopy PWC.Asim.ExcelTools\bin\Release\AsimExcelTools.exe %BUILDDIR%\bin
xcopy Algorithms\PWC.Asim.Algorithms.PvNone\bin\Release\PWC.SLMS.Algorithms.PvNone.dll %BUILDDIR%\bin
xcopy Algorithms\PWC.Asim.Algorithms.PvSimple\bin\Release\PWC.SLMS.Algorithms.PvSimple.dll %BUILDDIR%\bin

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

set ZIPFILE=Asim-%mydate%_%mytime%.zip
"%ZIP%" a -r "%ZIPFILE%" %BUILDDIR%
rmdir /s /q Asim
pause
