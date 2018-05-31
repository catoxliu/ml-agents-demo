@echo off

if "%~1" == "" GOTO HELP
SET "ENV=%~1"
if "%~2" == "" GOTO HELP
SET "RUNID=%~2"

rd /s /q "summaries/%RUNID%"
rd /s /q "models/%RUNID%"
python learn.py %ENV% --run-id=%RUNID% --train
GOTO END

:HELP
echo "should run with env and runid"
echo "EXAMPLE: train.bat car car_1"

:END
exit /B