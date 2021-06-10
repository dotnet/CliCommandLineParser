@echo off

@REM install the 2.1.0 runtime for running tests
powershell -ExecutionPolicy ByPass -command "& """%~dp0common\dotnet-install.ps1""" -runtime dotnet -version 2.1.0"
if %errorlevel% neq 0 exit /b %errorlevel%

powershell -ExecutionPolicy ByPass -command "& """%~dp0common\Build.ps1""" -restore -build -test -sign -pack -publish -ci %*"
exit /b %errorlevel%
