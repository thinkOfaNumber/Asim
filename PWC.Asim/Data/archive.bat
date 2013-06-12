@echo off

rem date/time
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%b-%%a)
For /f "tokens=1-2 delims=/:" %%a in ("%TIME%") do (set mytime=%%a%%b)
set mytime=%mytime: =0%

set ARCDIR=Asim-Archive%mydate%_%mytime%
mkdir %ARCDIR%

set FILES=
call :splitFiles %ASIM_OUTPUTFILES%,%ASIM_INPUTFILES%,%ASIM_EXCELFILE%
@goto :EOF


:splitFiles
set tosplit=%*
for /F "tokens=1,* delims=," %%A in ("%tosplit%") DO (
	rem echo parsing %%A
	call :FullPath "%%A"
	copy "%FULLPATH%" %ARCDIR%
	if [%%B] == [] exit /b
	call :splitFiles %%B
)
exit /b


:FullPath
set fullpath=%~dpnx1
exit /b
