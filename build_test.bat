@echo off
echo Building LYBT.Backend.sln...
dotnet build LYBT.Backend.sln --no-restore
echo.
echo Build completed with exit code: %ERRORLEVEL%