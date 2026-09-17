@echo off
setlocal DisableDelayedExpansion

rem Use portable storage next to this launcher, independently of the calling directory.
pushd "%~dp0"
if errorlevel 1 goto folder_error
if not exist "osu!.exe" goto missing_client
if not exist "framework.ini" (
    > "framework.ini" echo WindowMode = Windowed
    if errorlevel 1 goto storage_error
)

rem Prevent the official updater from replacing this personal practice client.
set "OSU_EXTERNAL_UPDATE_PROVIDER=Phantom Misses personal fork"
set "OSU_WEBSOCKET_SERVER="
echo Phantom Misses portable client. Close other osu! clients before starting.
"osu!.exe" %*
set "result=%errorlevel%"
if not "%result%"=="0" (
    echo The client exited with code %result%. Check the logs folder for details.
    pause
)
popd
exit /b %result%

:missing_client
echo Extract the entire Windows ZIP before running this launcher.
goto failed

:storage_error
echo This folder is not writable. Extract the package into your user folder.
goto failed

:folder_error
echo Could not open the portable client folder.
pause
exit /b 1

:failed
pause
popd
exit /b 1
