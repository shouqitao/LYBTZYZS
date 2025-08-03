@echo off
echo 启动凌隐宝堂中医诊所诊疗系统
echo ==================================

REM 启动 WebAPI
echo 启动 WebAPI 服务...
cd /d "%~dp0src\Backend\Services\LYBT.WebAPI"
start "LYBT WebAPI" cmd /k "dotnet run --urls https://localhost:7001"

REM 等待 WebAPI 启动
echo 等待 WebAPI 启动...
timeout /t 10 /nobreak > nul

REM 启动 WPF 客户端
echo 启动 WPF 客户端...
cd /d "%~dp0src\Frontend\Desktop\Shell"
start "LYBT WPF Client" dotnet run

echo.
echo 应用程序已启动！
echo WebAPI: https://localhost:7001
echo Swagger: https://localhost:7001/swagger
echo.
echo 按任意键关闭此窗口...
pause > nul