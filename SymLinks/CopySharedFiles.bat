@echo off
REM ============================================================================
REM Main script to update shared files for all projects
REM
REM This script calls the ProcessSharedFiles.bat script with the
REM appropriate definition file and target directory for each project.
REM
REM Usage: Double‑click or run normally (no admin rights required)
REM ============================================================================

echo Updating shared files for all projects...
echo.

REM Navigate to the SymLinks directory
cd /d "%~dp0" || (
    echo Failed to navigate to SymLinks directory
    pause
    exit /b 1
)

REM Process BoatControlPanel shared files
echo Processing BoatControlPanel shared files...
call ProcessSharedFiles.bat SymLinkDefinitionsBoatControlPanel.txt ..\BoatControlPanel
if errorlevel 1 (
    echo Failed to update BoatControlPanel shared files
    pause
    exit /b 1
)
echo.

REM Process SmartFuseBox shared files
echo Processing SmartFuseBox shared files...
call ProcessSharedFiles.bat SymLinkDefinitionsSmartFuseBox.txt ..\SmartFuseBox
if errorlevel 1 (
    echo Failed to update SmartFuseBox shared files
    pause
    exit /b 1
)
echo.

echo All projects updated successfully!
echo.
pause
exit /b 0