@echo off
setlocal
set "DOTNET_URL=https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
set "APP=%~dp0FileBrowserDesktop.exe"

if not exist "%APP%" (
  set "APP=%~dp0src\bin\Release\net8.0-windows\win-x64\publish\FileBrowserDesktop.exe"
)

where dotnet >nul 2>nul
if errorlevel 1 goto missing_dotnet

dotnet --list-runtimes | findstr /B /C:"Microsoft.WindowsDesktop.App 8." >nul 2>nul
if errorlevel 1 goto missing_dotnet

if not exist "%APP%" (
  echo FileBrowserDesktop.exe was not found.
  echo Build it with:
  echo   dotnet publish "%~dp0src\FileBrowserDesktop.csproj" -c Release -r win-x64 --self-contained false
  pause
  exit /b 1
)
start "" "%APP%"
exit /b 0

:missing_dotnet
echo File Browser Desktop requires the .NET 8 Desktop Runtime.
echo.
echo Install it from:
echo   %DOTNET_URL%
echo.
echo After installing, run this launcher again.
echo.
pause
exit /b 1
