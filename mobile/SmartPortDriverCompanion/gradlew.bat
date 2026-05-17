@echo off
where gradle >nul 2>nul
if %ERRORLEVEL% EQU 0 (
  gradle -p "%~dp0" %*
  exit /b %ERRORLEVEL%
)
echo Gradle is not installed and the wrapper JAR is not vendored in this repository.
echo Install Gradle 8.14.4 or run the GitHub Actions APK workflow.
exit /b 127
