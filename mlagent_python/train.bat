@echo off

if "%~1" == "" GOTO HELP
SET "ENV=%~1"
if "%~2" == "" GOTO HELP
SET "RUNID=%~2"
GOTO RUN

:HELP
echo "should run with env and runid"
echo "EXAMPLE: train.bat car car_1"
GOTO :END

:RUN
rd /s /q "summaries/%RUNID%"
rd /s /q "models/%RUNID%"
start python learn.py %ENV% --run-id=%RUNID% --train

:END