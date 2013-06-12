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

pause