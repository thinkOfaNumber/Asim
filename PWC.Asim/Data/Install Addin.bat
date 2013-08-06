::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: The purpose of this document is to move the Asim Addin.xla into the Microsoft Addin            ::
:: folder, making it available to excel to use and enable.                                                         ::
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


@echo off 

set var_addin=%APPDATA%\Microsoft\AddIns
set var_simulator=.\Asim Addin.xla

echo Destination Folder: %var_addin%
::echo %var_simulator%

xcopy "%var_simulator%" "%var_addin%" /R /Y
if %ERRORLEVEL% EQU 0 goto endok
echo 
echo ===========================================================================
echo = An error ocurred copying the Add-in.  Please make sure you close all    =
echo = Microsoft Office documents.  If the Add-in still fails with this error, =
echo = please reboot and try again.                                            =
echo ===========================================================================
goto end

:endok
echo Add-in successfully installed.
:end
pause
