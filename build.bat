@echo off
rem Build PyoroGL as a single self-contained Windows executable.
rem
rem Usage:
rem   build.bat            (win-x64)
rem   build.bat linux-x64  (cross-compile, usually unnecessary)
rem
rem Output: dist\win-x64\MonogameTest.exe + the Assets folder it loads at runtime.
setlocal
cd /d "%~dp0"

set RID=%1
if "%RID%"=="" set RID=win-x64

echo ==^> Building %RID% (single file, self-contained)
dotnet publish PyoroGL\PyoroGL.csproj ^
    -c Release ^
    -r %RID% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishTrimmed=true ^
    -p:TrimMode=partial ^
    -p:EnableCompressionInSingleFile=true ^
    -o dist\%RID%
if errorlevel 1 goto :fail

echo ==^> Copying runtime assets
if exist dist\%RID%\Assets rmdir /s /q dist\%RID%\Assets
xcopy /e /i /y PyoroGL\Assets dist\%RID%\Assets >nul
if exist dist\%RID%\Content rmdir /s /q dist\%RID%\Content
xcopy /e /i /y PyoroGL\Content dist\%RID%\Content >nul
for /r dist\%RID%\Content %%f in (*.spritefont *.mgcb *.png) do if exist "%%f" del "%%f"
rem Keep only what the game loads at runtime.
for %%f in (backdropwithstars.png background.png screen.png screenshot.png sprite0_0.png reference.png select.png tonguesprite.png tongueparts_0.png tonguecollision.png) do (
    if exist dist\%RID%\Assets\%%f del dist\%RID%\Assets\%%f
)
for %%f in (dist\%RID%\Assets\*.psd) do if exist "%%f" del "%%f"

echo ==^> Done: dist\%RID%\
dir dist\%RID%
goto :eof

:fail
echo BUILD FAILED
exit /b 1
