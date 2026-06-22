@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\FileBrowserDesktop.csproj"
set "PUBLISH=%ROOT%src\bin\Release\net8.0-windows\win-x64\publish"
set "DIST=%ROOT%dist"
set "STAGE=%DIST%\stage\FileBrowserDesktop"
set "ZIP=%DIST%\FileBrowserDesktop-win-x64-framework-dependent.zip"

if not exist "%DIST%" mkdir "%DIST%"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained false
if errorlevel 1 exit /b 1

if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%"

xcopy "%PUBLISH%\*" "%STAGE%\" /E /I /Y >nul
del /s /q "%STAGE%\*.pdb" >nul 2>nul
copy "%ROOT%RunFileBrowserDesktop.cmd" "%STAGE%\" >nul
copy "%ROOT%README.md" "%STAGE%\" >nul
copy "%ROOT%INSTALL.md" "%STAGE%\" >nul
copy "%ROOT%LICENSE" "%STAGE%\" >nul
copy "%ROOT%CHANGELOG.md" "%STAGE%\" >nul
copy "%ROOT%CONTRIBUTING.md" "%STAGE%\" >nul
copy "%ROOT%SECURITY.md" "%STAGE%\" >nul
copy "%ROOT%SUPPORT.md" "%STAGE%\" >nul
copy "%ROOT%SERVER_SETUP.md" "%STAGE%\" >nul
copy "%ROOT%PACKAGING.md" "%STAGE%\" >nul

if exist "%ZIP%" del "%ZIP%"
tar.exe -a -c -f "%ZIP%" -C "%STAGE%" .
if errorlevel 1 (
  echo Failed to create release zip. Make sure tar.exe is available on PATH.
  exit /b 1
)

echo.
echo Created:
echo %ZIP%
