@echo off
REM Postman/Newman Integration Test Runner
REM Usage: scripts\run-postman-tests.bat
REM Prerequisites: npm install -g newman newman-reporter-htmlextra

powershell -ExecutionPolicy Bypass -File "%~dp0run-postman-tests.ps1"
exit /b %errorlevel%
