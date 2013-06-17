@echo off
rem this is a test of the batch executor
set LOGFILE=test.log

rm "%LOGFILE%"
echo. > "%LOGFILE%"
echo ASIM environment variables: >> "%LOGFILE%"
echo. >> "%LOGFILE%"
set ASIM_ >> "%LOGFILE%"
