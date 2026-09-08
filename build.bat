@echo off
rem Build PyoroGL as a single self-contained Windows executable.
rem
rem Usage:
rem   build.bat            (win-x64)
rem   build.bat linux-x64  (cross-compile, usually unnecessary)
rem
rem Output: dist\win-x64\WarHook.exe, runtime DLLs, Assets and Content folders.
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
    -p:PublishTrimmed=true ^
    -p:TrimMode=partial ^
    -p:EnableCompressionInSingleFile=true ^
    -o dist\%RID%
if errorlevel 1 goto :fail

rem Publish copies all runtime assets and required native libraries from the project.
echo ==^> Done: dist\%RID%\
dir dist\%RID%
goto :eof

:fail
echo BUILD FAILED
exit /b 1
