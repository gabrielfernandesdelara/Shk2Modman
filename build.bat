@echo off
rem Compila o Shrek2ModManager.exe com o compilador C# que ja vem no Windows
rem (.NET Framework 4.8). Nao precisa instalar Visual Studio nem SDK.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Nao achei o compilador em %CSC%
  exit /b 1
)
cd /d "%~dp0"
if not exist dist mkdir dist
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ ^
  /win32manifest:src\app.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll ^
  /out:dist\Shrek2ModManager.exe ^
  src\Program.cs src\MainForm.cs src\PatchEngine.cs src\ModLoader.cs src\SteamLocator.cs src\Settings.cs src\Texts.cs
if errorlevel 1 exit /b 1
if not exist dist\mods mkdir dist\mods
copy /y mods\*.json dist\mods\ >nul
echo OK: dist\Shrek2ModManager.exe
