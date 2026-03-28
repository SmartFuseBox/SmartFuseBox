@echo off
setlocal enabledelayedexpansion
REM ============================================================================
REM Copy shared files between projects and Shared
REM ============================================================================

set "DEFINITIONS_FILE=%~1"
set "TARGET_DIR=%~2"

REM Validate parameters
if "%DEFINITIONS_FILE%"=="" (
    echo ERROR: Definitions file not specified
    exit /b 1
)

if "%TARGET_DIR%"=="" (
    echo ERROR: Target directory not specified
    exit /b 1
)

REM Verify definitions file exists
if not exist "%DEFINITIONS_FILE%" (
    echo ERROR: Definitions file not found: %DEFINITIONS_FILE%
    exit /b 1
)

REM Verify target directory exists
if not exist "%TARGET_DIR%" (
    echo ERROR: Target directory not found: %TARGET_DIR%
    exit /b 1
)

echo Copying shared files using: %DEFINITIONS_FILE%
echo Target directory: %TARGET_DIR%
echo.

REM Format: <relativeTargetPath>|<sourcePath>
for /f "usebackq tokens=1,2 delims=|" %%a in ("%DEFINITIONS_FILE%") do (
    set "TARGET_FILE=%TARGET_DIR%\%%a"
    set "SOURCE_FILE=%%b"

    echo Copying "!SOURCE_FILE!" → "!TARGET_FILE!"

    REM Ensure target directory exists
    for %%D in ("!TARGET_FILE!") do (
        if not exist "%%~dpD" (
            mkdir "%%~dpD" >nul 2>&1
        )
    )

    copy /Y "!SOURCE_FILE!" "!TARGET_FILE!" >nul
    if errorlevel 1 (
        echo ERROR: Failed to copy "!SOURCE_FILE!"
        exit /b 1
    )
)

echo.
echo All shared files copied successfully!
echo.
exit /b 0