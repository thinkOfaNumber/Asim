@echo off
rem this is a test of the batch executor to spawn the tma graphing tool

set FILES=
call :splitFiles %ASIM_OUTPUTFILES%
call %1 %FILES%
@goto :EOF

:splitFiles
set tosplit=%*
for /F "tokens=1,* delims=," %%A in ("%tosplit%") DO (
	rem echo parsing %%A
	call :FullPath "%%A"
	set FILES=%FILES% -get "%FULLPATH%"
	IF [%%B] == [] exit /b
	call :splitFiles %%B
)
exit /b

:FullPath
set fullpath=%~dpnx1
exit /b
