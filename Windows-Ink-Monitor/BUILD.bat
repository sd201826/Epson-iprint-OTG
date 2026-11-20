@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK is required to build this app.
  echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet publish EpsonL3210Monitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 pause
