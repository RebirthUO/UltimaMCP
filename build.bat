@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "RepoRoot=%~dp0"
set "Configuration=Debug"
set "ExtraArgs="

:parse
if "%~1"=="" goto gates

if /I "%~1"=="-c" goto take_config
if /I "%~1"=="-Configuration" goto take_config
if /I "%~1"=="Debug" (
  set "Configuration=Debug"
  shift
  goto parse
)
if /I "%~1"=="Release" (
  set "Configuration=Release"
  shift
  goto parse
)

set "ExtraArgs=!ExtraArgs! %1"
shift
goto parse

:take_config
if "%~2"=="" (
  echo Missing value for %~1. Expected Debug or Release. 1>&2
  exit /b 1
)
set "Configuration=%~2"
shift
shift
goto parse

:gates
where dotnet >nul 2>&1
if errorlevel 1 (
  echo dotnet was not found on PATH. Install the .NET SDK and retry. 1>&2
  exit /b 1
)

if not exist "%RepoRoot%Ultima.dll" (
  echo Ultima.dll is missing at repo root (%RepoRoot%Ultima.dll^). UltimaAPI references ..\Ultima.dll. 1>&2
  exit /b 1
)

dotnet build "%RepoRoot%UltimaAPI\UltimaAPI.csproj" -c !Configuration! !ExtraArgs!
exit /b %ERRORLEVEL%
