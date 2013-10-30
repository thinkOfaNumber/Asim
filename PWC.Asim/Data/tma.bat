@echo off
rem this is a test of the batch executor to spawn the tma graphing tool
setlocal enableextensions enabledelayedexpansion

set FILES=
echo starting tma with %* > tma.log
call :splitFiles %ASIM_OUTPUTFILES%
set "FILESBS=%FILES:\=/%"
set FILE_NAME="%~nx1"
set FILE_FOLDER="%~dp1"
cd %FILE_FOLDER%
echo call %FILE_NAME% %FILESBS% >> tma.log
call %1 %FILESBS%
endlocal
GOTO:EOF

:splitFiles
set tosplit=%*
for /F "tokens=1,* delims=," %%A in ("%tosplit%") DO (
	rem echo parsing %%A
	call :QuoteString %%A str
	call :FullPath !str! FP
	echo received path !FP! >> tma.log
	set FILES=!FILES! -get "!FP!"
	IF [%%B] == [] exit /b
	call :splitFiles %%B
)
GOTO:EOF

:FullPath
set "%~2=%~dpnx1"
GOTO:EOF

:QuoteString
for /f "useback tokens=*" %%A in ('%1') do set %~2="%%~A"
GOTO:EOF