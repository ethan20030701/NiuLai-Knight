@echo off
setlocal
rem ============================================================
rem  Hollow Knight mod toggle for Slay the Spire 2
rem  Enabled  = mod folders live in the game's mods directory
rem  Disabled = mod folders parked in mods_disabled (this project)
rem  Restart the game after toggling - mods load at startup only.
rem ============================================================
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods"
set "PARK=C:\Users\asus\HollowKnight\mods_disabled"

if exist "%GAME%\HollowKnight" goto disable
if exist "%PARK%\HollowKnight" goto enable
echo Nothing to toggle: mod not found in either location.
pause
exit /b 1

:disable
echo Disabling Hollow Knight mod (moves HollowKnight + BaseLib out of the game)...
move "%GAME%\HollowKnight" "%PARK%\HollowKnight" >nul || goto fail
move "%GAME%\BaseLib" "%PARK%\BaseLib" >nul || goto fail
echo.
echo  [OFF] Mod disabled. Restart Slay the Spire 2 to play vanilla / multiplayer.
pause
exit /b 0

:enable
echo Enabling Hollow Knight mod (moves HollowKnight + BaseLib back into the game)...
move "%PARK%\HollowKnight" "%GAME%\HollowKnight" >nul || goto fail
move "%PARK%\BaseLib" "%GAME%\BaseLib" >nul || goto fail
echo.
echo  [ON] Mod enabled. Restart Slay the Spire 2 and look for The Knight.
pause
exit /b 0

:fail
echo ERROR: move failed - is the game running with files locked? Close the game first.
pause
exit /b 1
