@echo off
if exist "%~dp0artifacts\JuanTool\JuanTool.exe" (
    start "" "%~dp0artifacts\JuanTool\JuanTool.exe"
) else (
    echo Run powershell -ExecutionPolicy Bypass -File scripts\build.ps1 first.
    pause
)
