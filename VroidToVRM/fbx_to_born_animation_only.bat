@echo off
setlocal enabledelayedexpansion

:: FBX to bone animation only FBX converter (Blender background mode)
::
:: Usage:
::   fbx_to_born_animation_only.bat                  - convert test07.fbx
::   fbx_to_born_animation_only.bat file.fbx         - drag one or more FBX files
::   fbx_to_born_animation_only.bat "FBX folder"     - convert all FBX files in folder
::   fbx_to_born_animation_only.bat file.fbx out.fbx - convert one file to specific output

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
pause & exit /b 1

:found_blender
echo [INFO] Blender: !BLENDER!

:: --- Python script path ---
set SCRIPT=%~dp0fbx_to_born_animation_only.py
if not exist "!SCRIPT!" (
    echo [ERROR] fbx_to_born_animation_only.py not found: !SCRIPT!
    pause & exit /b 1
)

set /a COUNT=0
set /a ERRORS=0

:: No argument -> use sample file
if "%~1"=="" (
    set SRC=%~dp0test07.fbx
    set DST=%~dp0test07_bone_only.fbx
    goto :run_one
)

:: Folder mode
if exist "%~f1\" goto :mode_folder

:: One file + explicit output
if /i "%~x1"==".fbx" if not "%~2"=="" if /i "%~x2"==".fbx" (
    set SRC=%~f1
    set DST=%~f2
    goto :run_one
)

:: File mode: process each dragged .fbx file
:mode_files
if "%~1"=="" goto :done
if /i "%~x1"==".fbx" (
    set SRC=%~f1
    set DST=%~dp1%~n1_bone_only.fbx
    call :convert_one
)
shift
goto :mode_files

:mode_folder
set INPUT_DIR=%~f1
for %%F in ("!INPUT_DIR!\*.fbx") do (
    set SRC=%%~fF
    set DST=%%~dpnF_bone_only.fbx
    call :convert_one
)
goto :done

:run_one
call :convert_one
goto :done

:convert_one
if not exist "!SRC!" (
    echo [ERROR] Input not found: !SRC!
    set /a ERRORS+=1
    exit /b 0
)
echo [!COUNT!] !SRC!
echo      -^> !DST!
"!BLENDER!" -b --python "!SCRIPT!" -- "!SRC!" "!DST!"
if !errorlevel! neq 0 (
    echo      [WARN] failed: !SRC!
    set /a ERRORS+=1
) else (
    set /a COUNT+=1
)
exit /b 0

:done
echo.
if !COUNT! == 0 (
    echo [INFO] No FBX files converted. error(s^): !ERRORS!
) else (
    echo Done: !COUNT! file(s^), !ERRORS! error(s^).
)
pause
exit /b !ERRORS!