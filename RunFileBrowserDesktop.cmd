@echo off
setlocal
set "DOTNET_URL=https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\FileBrowserDesktop.csproj"
set "TARGET_FRAMEWORK=net8.0-windows10.0.17763.0"
set "PUBLISH=%ROOT%src\bin\Release\%TARGET_FRAMEWORK%\win-x64\publish"
set "APP=%ROOT%FileBrowserDesktop.exe"

if not exist "%APP%" (
  set "APP=%PUBLISH%\FileBrowserDesktop.exe"
)
if not exist "%APP%" (
  set "APP=%ROOT%src\bin\Release\%TARGET_FRAMEWORK%\win-x64\FileBrowserDesktop.exe"
)
if not exist "%APP%" (
  set "APP=%ROOT%src\bin\Debug\%TARGET_FRAMEWORK%\FileBrowserDesktop.exe"
)

where dotnet >nul 2>nul
if errorlevel 1 goto missing_dotnet

dotnet --list-runtimes | findstr /B /C:"Microsoft.NETCore.App 8." >nul 2>nul
if errorlevel 1 goto missing_dotnet

if not exist "%APP%" (
  echo FileBrowserDesktop.exe was not found. Publishing Release build...
  echo.
  dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained false
  if errorlevel 1 (
    echo.
    echo Publish failed.
    pause
    exit /b 1
  )
  set "APP=%PUBLISH%\FileBrowserDesktop.exe"
)

if not exist "%APP%" (
  echo.
  echo FileBrowserDesktop.exe was still not found after publish:
  echo   %APP%
  pause
  exit /b 1
)
start "" "%APP%"
exit /b 0

:missing_dotnet
echo File Browser Desktop requires the .NET 8 Runtime.
echo.
echo Install it from:
echo   %DOTNET_URL%
echo.
echo After installing, run this launcher again.
echo.
pause
exit /b 1
