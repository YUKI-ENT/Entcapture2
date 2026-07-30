@echo off
setlocal

if "%~1"=="" (
  echo Usage:
  echo   build\train_entcapture_yolo.cmd DATASET_DIR [RUN_NAME]
  echo.
  echo Example:
  echo   build\train_entcapture_yolo.cmd D:\work\ENTcapture2_YOLO_Dataset gram_stain_v1
  exit /b 1
)

set "DATASET=%~1"
set "RUNNAME=%~2"

if "%RUNNAME%"=="" (
  py -3 "%~dp0train_entcapture_yolo.py" --dataset "%DATASET%" --install-deps
) else (
  py -3 "%~dp0train_entcapture_yolo.py" --dataset "%DATASET%" --name "%RUNNAME%" --install-deps
)

exit /b %ERRORLEVEL%
