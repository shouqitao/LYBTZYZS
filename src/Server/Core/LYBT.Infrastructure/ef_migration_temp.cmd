@echo off
cd /d "%~dp0"
dotnet ef migrations add AddUserBusinessFields --startup-project ..\..\Services\LYBT.WebAPI --output-dir Migrations
pause