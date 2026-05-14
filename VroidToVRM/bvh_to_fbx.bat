@echo off
setlocal enabledelayedexpansion

:: BVH to FBX batch converter (Blender background mode)
::
:: Usage:
::   bvh_to_fbx.bat                        - convert default BVH folder
::   bvh_to_fbx.bat  file.bvh              - drag one or more BVH files
::   bvh_to_fbx.bat  "BVH folder"          - drag a folder
::   bvh_to_fbx.bat  "BVH folder"  "output folder"

:: --- Find Blender ---
set BLENDER=
for /d %%D in ("C:\Program Files\WindowsApps\*BlenderFoundation*") do (
    set TRY=%%~fD\Blender\blender.exe
    if exist "!TRY!" ( set BLENDER=!TRY! & goto :found_blender )
)
for %%V in (5.1 5.0 4.4 4.3 4.2 4.1 4.0 3.6) do (
    set TRY=C:\Program Files\Blender Foundation\Blender %%V\blender.exe
    if exist "!TRY!" ( set BLENDER=!TRY! & goto :found_blender )
)
where blender >nul 2>&1 && set BLENDER=blender && goto :found_blender
echo [ERROR] Blender not found.
pause & cmd /k
:found_blender
echo [INFO] Blender: !BLENDER!

:: --- Python script path ---
set SCRIPT=%~dp0bvh_to_fbx.py
if not exist "!SCRIPT!" (
    echo [ERROR] bvh_to_fbx.py not found: !SCRIPT!
    pause & cmd /k
)

set /a COUNT=0
set /a ERRORS=0
set OUTPUT_DIR=

:: No argument -> use default folder
if "%~1"=="" goto :mode_default

:: Folder or file?
if exist "%~f1\" goto :mode_folder

:: --- File mode: process each dragged .bvh file ---
:mode_files
if "%~1"=="" goto :done
if /i "%~x1"==".bvh" (
    set SRC=%~f1
    set DST=%~dp1%~n1.fbx
    echo [!COUNT!] %~nx1
    "!BLENDER!" -b --python "!SCRIPT!" -- "!SRC!" "!DST!"
    if !errorlevel! neq 0 (
        echo      [WARN] failed: %~nx1
        set /a ERRORS+=1
    )
    set /a COUNT+=1
)
shift
goto :mode_files

:: --- Folder mode ---
:mode_folder
set INPUT_DIR=%~f1
if not "%~2"=="" ( set OUTPUT_DIR=%~f2 ) else ( set OUTPUT_DIR=!INPUT_DIR! )
goto :run_folder

:: --- Default mode ---
:mode_default
set INPUT_DIR=%~dp0VRMtoUnity3D\Assets\Motion\BVH
set OUTPUT_DIR=!INPUT_DIR!

:run_folder
if not exist "!INPUT_DIR!" (
    echo [ERROR] Folder not found: !INPUT_DIR!
    pause & cmd /k
)
if not exist "!OUTPUT_DIR!" mkdir "!OUTPUT_DIR!"
echo [INFO] Input : !INPUT_DIR!
echo [INFO] Output: !OUTPUT_DIR!
echo.
for %%F in ("!INPUT_DIR!\*.bvh") do (
    set SRC=%%~fF
    set DST=!OUTPUT_DIR!\%%~nF.fbx
    echo [!COUNT!] %%~nxF
    "!BLENDER!" -b --python "!SCRIPT!" -- "!SRC!" "!DST!"
    if !errorlevel! neq 0 (
        echo      [WARN] failed: %%~nxF
        set /a ERRORS+=1
    )
    set /a COUNT+=1
)

:done
echo.
if !COUNT! == 0 (
    echo [INFO] No BVH files found.
) else (
    echo Done: !COUNT! file(s^), !ERRORS! error(s^).
)
pause
cmd /k
