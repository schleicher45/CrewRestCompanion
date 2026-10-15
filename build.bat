@echo off
rem ---------------------------------------------------------------
rem  Baut Plugin und Configurator und legt beides in dist\ ab.
rem  Danach kann installer\CrewRestCompanion.iss kompiliert werden.
rem ---------------------------------------------------------------
setlocal enableextensions
set "ROOT=%~dp0"
set "LOG=%ROOT%build.log"
set "DIST=%ROOT%dist"

echo === Crew Rest Companion Build === > "%LOG%"
echo Start: %DATE% %TIME% >> "%LOG%"

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo FEHLER: vswhere.exe nicht gefunden. Visual Studio installiert? >> "%LOG%"
    goto ende
)

set "MSBUILD="
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo FEHLER: MSBuild.exe nicht gefunden. >> "%LOG%"
    goto ende
)
echo MSBuild: %MSBUILD% >> "%LOG%"

taskkill /f /im CrewRestConfigurator.exe >nul 2>&1

echo. >> "%LOG%"
"%MSBUILD%" "%ROOT%CrewRestCompanion.sln" /restore /t:Rebuild /p:Configuration=Release /v:minimal /nologo >> "%LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
echo EXITCODE=%RESULT% >> "%LOG%"
if not "%RESULT%"=="0" goto ende

if not exist "%DIST%" mkdir "%DIST%"
copy /y "%ROOT%plugin\bin\Release\CrewRestCompanion.dll" "%DIST%\" >> "%LOG%" 2>&1
copy /y "%ROOT%configurator\bin\Release\net48\CrewRestConfigurator.exe" "%DIST%\" >> "%LOG%" 2>&1
echo Ergebnis liegt in dist\ >> "%LOG%"

echo. >> "%LOG%"
echo ---------- Setup ---------- >> "%LOG%"

set "ISCC="
for %%P in (
    "%LocalAppData%\Programs\Inno Setup 7\ISCC.exe"
    "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
    "%ProgramFiles%\Inno Setup 7\ISCC.exe"
    "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
    "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    "%ProgramFiles%\Inno Setup 6\ISCC.exe"
) do if exist %%P set "ISCC=%%~P"

if not defined ISCC (
    echo Inno Setup nicht gefunden - es wurde kein Setup gebaut. >> "%LOG%"
    echo Inno Setup hier laden: https://jrsoftware.org/isdl.php >> "%LOG%"
    echo Danach diese Datei erneut ausfuehren, das Setup entsteht dann automatisch. >> "%LOG%"
    goto ende
)

echo Inno Setup: %ISCC% >> "%LOG%"
"%ISCC%" "%ROOT%installer\CrewRestCompanion.iss" >> "%LOG%" 2>&1
echo EXITCODE_SETUP=%ERRORLEVEL% >> "%LOG%"
echo Setup liegt in installer\Output >> "%LOG%"

:ende
echo Ende: %DATE% %TIME% >> "%LOG%"
type "%LOG%"
echo.
if not "%~1"=="nopause" pause
