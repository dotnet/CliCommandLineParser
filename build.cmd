@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0build\Build.ps1""" -restore -build %*"
exit /b %ErrorLevel%
