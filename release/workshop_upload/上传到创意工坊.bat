@echo off
chcp 65001 >nul
echo ============================================
echo  The Knight mod - Workshop uploader
echo  v0.2.1, updates item 3796063046
echo ============================================
set /p STEAMUSER=Steam username: 
C:\steamcmd\steamcmd.exe +login %STEAMUSER% +workshop_build_item "C:\Users\asus\HollowKnight\release\workshop_upload\HollowKnight_uploader.vdf" +quit
echo.
echo Done. If it printed a publishedfileid, open the workshop item page to
echo set the title, preview image, description and add BaseLib (3737335127)
echo as a required item, then make it public.
pause
