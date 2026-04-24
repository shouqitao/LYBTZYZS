@echo off
cd /d C:\Services\LYBT-API
set ASPNETCORE_ENVIRONMENT=Production
start "LYBT-API" /B "C:\Program Files\dotnet\dotnet.exe" LYBT.WebAPI.dll
