@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 主控制台

:MAIN_MENU
cls
echo.
echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 主控制台
echo ====================================================
echo.
echo 🚀 开发工具:
echo   1. 启动开发服务器
echo   2. 数据库管理工具
echo.
echo 📦 发布部署:
echo   3. 发布生产版本
echo   4. 一键部署 (配置向导)
echo.
echo 🔧 维护工具:
echo   5. 构建整个解决方案
echo   6. 运行测试
echo   7. 清理构建缓存
echo.
echo 📖 帮助信息:
echo   8. 查看系统信息
echo   9. 打开项目文档
echo.
echo   0. 退出
echo.

set /p "CHOICE=请选择操作 (0-9): "

if "%CHOICE%"=="1" goto DEV_SERVER
if "%CHOICE%"=="2" goto DATABASE_MANAGER
if "%CHOICE%"=="3" goto PUBLISH
if "%CHOICE%"=="4" goto DEPLOY
if "%CHOICE%"=="5" goto BUILD_SOLUTION
if "%CHOICE%"=="6" goto RUN_TESTS
if "%CHOICE%"=="7" goto CLEAN_BUILD
if "%CHOICE%"=="8" goto SYSTEM_INFO
if "%CHOICE%"=="9" goto OPEN_DOCS
if "%CHOICE%"=="0" goto EXIT

echo 无效选项，请重新选择
timeout /t 2 >nul
goto MAIN_MENU

:DEV_SERVER
echo.
echo 🚀 启动开发服务器...
call "%~dp0start-dev.bat"
goto MAIN_MENU

:DATABASE_MANAGER
echo.
echo 🔧 打开数据库管理工具...
call "%~dp0database-manager.bat"
goto MAIN_MENU

:PUBLISH
echo.
echo 📦 发布生产版本...
call "%~dp0publish-production.bat"
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:DEPLOY
echo.
echo 🚀 一键部署 (配置向导)...
call "%~dp0deploy-all.bat"
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:BUILD_SOLUTION
cls
echo.
echo 🔨 构建整个解决方案...
cd /d "%~dp0.."
dotnet build LYBTZYZS.sln
if %ERRORLEVEL% equ 0 (
    echo ✅ 构建成功！
) else (
    echo ❌ 构建失败！
)
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:RUN_TESTS
cls
echo.
echo 🧪 运行测试...
cd /d "%~dp0.."
dotnet test
if %ERRORLEVEL% equ 0 (
    echo ✅ 测试通过！
) else (
    echo ❌ 测试失败！
)
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:CLEAN_BUILD
cls
echo.
echo 🧹 清理构建缓存...
cd /d "%~dp0.."
echo 清理bin目录...
if exist "BIN" rmdir /s /q "BIN"
echo 清理obj目录...
for /d /r . %%d in (obj) do @if exist "%%d" rd /s /q "%%d"
for /d /r . %%d in (bin) do @if exist "%%d" rd /s /q "%%d"
dotnet clean
echo ✅ 清理完成！
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:SYSTEM_INFO
cls
echo.
echo ====================================================
echo              系统信息
echo ====================================================
echo.
echo 📋 项目信息:
echo   项目名称: 凌隐宝堂中医诊所管理系统
echo   版本: 1.0.0
echo   架构: ASP.NET Core 8.0 + WPF
echo.
echo 💻 系统环境:
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo   .NET版本: 
    dotnet --version
) else (
    echo   .NET状态: ❌ 未安装
)
echo   操作系统: %OS%
echo   计算机名: %COMPUTERNAME%
echo.
echo 📁 目录结构:
echo   项目根目录: %~dp0..
echo   后端API: src\Backend\Services\LYBT.WebAPI
echo   前端WPF: src\Frontend\Desktop\Shell
echo   发布目录: publish
echo.
echo 🌐 默认端口:
echo   开发环境: http://localhost:5297
echo   生产环境: http://localhost:5000
echo   Swagger文档: /swagger
echo.
echo 📖 重要文件:
echo   开发指南: docs\development\CLAUDE.md
echo   测试报告: docs\测试报告.md
echo   配置文件: src\Backend\Services\LYBT.WebAPI\appsettings.json
echo.
echo 按任意键返回主菜单...
pause >nul
goto MAIN_MENU

:OPEN_DOCS
echo.
echo 📖 打开项目文档...
if exist "%~dp0..\docs\development\CLAUDE.md" (
    start notepad "%~dp0..\docs\development\CLAUDE.md"
)
if exist "%~dp0..\docs\测试报告.md" (
    start notepad "%~dp0..\docs\测试报告.md"
)
if exist "%~dp0..\README.md" (
    start notepad "%~dp0..\README.md"
)
echo ✅ 文档已打开
timeout /t 2 >nul
goto MAIN_MENU

:EXIT
cls
echo.
echo ====================================================
echo              感谢使用
echo    凌隐宝堂中医诊所管理系统 - 主控制台
echo ====================================================
echo.
echo 🎉 系统功能完整，运行稳定
echo 📖 如需帮助，请查看docs目录中的文档
echo 🐛 如遇问题，请检查日志文件
echo.
echo 👋 再见！
echo.
timeout /t 3 >nul
exit /b 0