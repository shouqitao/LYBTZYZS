@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 生产发布器

echo.
echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 生产发布器
echo ====================================================
echo.

:: 设置变量
set "PROJECT_ROOT=%~dp0.."
set "WEBAPI_DIR=%PROJECT_ROOT%\src\Backend\Services\LYBT.WebAPI"
set "PUBLISH_DIR=%PROJECT_ROOT%\publish"
set "DATETIME=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set "DATETIME=%DATETIME: =0%"

echo 📂 项目根目录: %PROJECT_ROOT%
echo 🎯 发布目录: %PUBLISH_DIR%
echo 📅 发布时间: %DATETIME%
echo.

:: 检查项目目录
if not exist "%WEBAPI_DIR%" (
    echo ❌ 错误: 找不到WebAPI项目目录
    echo    期望路径: %WEBAPI_DIR%
    pause
    exit /b 1
)

:: 创建发布目录
if not exist "%PUBLISH_DIR%" (
    echo 📁 创建发布目录...
    mkdir "%PUBLISH_DIR%"
)

:: 清理旧的发布文件
echo 🧹 清理旧的发布文件...
if exist "%PUBLISH_DIR%\*" (
    rmdir /s /q "%PUBLISH_DIR%\wwwroot" 2>nul
    del /q "%PUBLISH_DIR%\*.dll" 2>nul
    del /q "%PUBLISH_DIR%\*.exe" 2>nul
    del /q "%PUBLISH_DIR%\*.json" 2>nul
    del /q "%PUBLISH_DIR%\*.pdb" 2>nul
)

echo.
echo 🔄 开始发布应用程序...
echo ⏳ 这可能需要几分钟时间，请耐心等待...
echo.

:: 发布应用程序
dotnet publish "%WEBAPI_DIR%" ^
    --configuration Release ^
    --output "%PUBLISH_DIR%" ^
    --self-contained false ^
    --runtime win-x64 ^
    --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ 发布失败！
    echo 💡 请检查项目是否可以正常编译
    pause
    exit /b 1
)

echo.
echo ✅ 发布完成！
echo.
echo 📁 发布文件位置: %PUBLISH_DIR%
echo 🚀 可执行文件: %PUBLISH_DIR%\LYBT.WebAPI.exe
echo.

:: 创建启动脚本
echo 📝 创建生产环境启动脚本...
(
echo @echo off
echo chcp 65001 ^>nul
echo title 凌隐宝堂中医诊所管理系统 - 生产环境
echo.
echo echo ====================================================
echo echo    凌隐宝堂中医诊所管理系统 - 生产环境
echo echo ====================================================
echo echo.
echo echo 🚀 正在启动服务器...
echo echo 💡 提示: 按 Ctrl+C 可以停止服务器
echo echo 📖 Swagger文档: http://localhost:5000/swagger
echo echo.
echo.
echo :: 设置生产环境变量
echo set ASPNETCORE_ENVIRONMENT=Production
echo set ASPNETCORE_URLS=http://localhost:5000
echo.
echo :: 启动应用程序
echo LYBT.WebAPI.exe
echo.
echo pause
) > "%PUBLISH_DIR%\start-production.bat"

:: 复制配置文件模板
echo 📋 创建配置文件模板...
if exist "%WEBAPI_DIR%\appsettings.json" (
    copy "%WEBAPI_DIR%\appsettings.json" "%PUBLISH_DIR%\appsettings.Production.json" >nul
)

echo.
echo 🎉 发布完成！
echo.
echo 📁 文件清单:
echo    ├─ LYBT.WebAPI.exe (主程序)
echo    ├─ start-production.bat (启动脚本)
echo    ├─ appsettings.Production.json (生产配置)
echo    └─ ... (其他依赖文件)
echo.
echo 🔧 使用说明:
echo    1. 编辑 appsettings.Production.json 配置数据库连接
echo    2. 双击 start-production.bat 启动服务器
echo    3. 访问 http://localhost:5000/swagger 查看API文档
echo.

set /p "OPEN_FOLDER=是否打开发布文件夹? (Y/N): "
if /i "%OPEN_FOLDER%"=="Y" (
    explorer "%PUBLISH_DIR%"
)

pause