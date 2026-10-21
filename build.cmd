@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

rem ---------------------------------------------------------------------------
rem  build.cmd - compila o CP2077 Performance
rem
rem  Sem argumentos  : publica self-contained em dist\
rem                     (executavel unico, nao precisa de runtime instalado)
rem  framework-dependent : publica em dist\framework-dependent\
rem                     (executavel pequeno, exige .NET 10 Desktop Runtime)
rem ---------------------------------------------------------------------------

set "PROJ=app\CP2077Performance.App\CP2077Performance.App.csproj"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERRO] o SDK do .NET nao esta no PATH. Instale o .NET 10 SDK.
  exit /b 1
)

if /i "%~1"=="framework-dependent" goto :framework

echo [1/3] restaurando pacotes...
dotnet restore "%PROJ%" || exit /b 1

echo [2/3] publicando self-contained...
dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=none -p:InvariantGlobalization=false ^
  -o "dist" || exit /b 1

echo [3/3] conferindo o artefato...
if not exist "dist\CP2077-Performance.exe" (
  echo [ERRO] o executavel nao foi gerado.
  exit /b 1
)
echo.
echo pronto: dist\CP2077-Performance.exe
goto :fim

:framework
echo [1/3] restaurando pacotes...
dotnet restore "%PROJ%" || exit /b 1

echo [2/3] publicando framework-dependent...
dotnet publish "%PROJ%" -c Release -p:DebugType=none -p:InvariantGlobalization=false ^
  -o "dist\framework-dependent" || exit /b 1

echo [3/3] conferindo o artefato...
if not exist "dist\framework-dependent\CP2077-Performance.exe" (
  echo [ERRO] o executavel nao foi gerado.
  exit /b 1
)
echo.
echo pronto: dist\framework-dependent\CP2077-Performance.exe
goto :fim

:fim
endlocal
exit /b 0
