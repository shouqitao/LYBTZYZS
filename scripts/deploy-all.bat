@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 一键部署工具

echo.
echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 一键部署工具
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
echo 📅 部署时间: %DATETIME%
echo.

:: 步骤1: 构建和发布
echo 🔄 第1步: 构建和发布应用程序...
call "%~dp0publish-production.bat"
if %ERRORLEVEL% neq 0 (
    echo ❌ 构建失败，部署中止
    pause
    exit /b 1
)

:: 步骤2: 生成配置向导
echo.
echo 🛠️  第2步: 配置生产环境参数...
echo.

:: 数据库配置
set /p "DB_SERVER=请输入数据库服务器地址 (默认: localhost): "
if "%DB_SERVER%"=="" set "DB_SERVER=localhost"

set /p "DB_NAME=请输入数据库名称 (默认: LYBTDB_Production): "
if "%DB_NAME%"=="" set "DB_NAME=LYBTDB_Production"

set /p "USE_INTEGRATED_AUTH=使用Windows集成认证? (Y/N, 默认: Y): "
if "%USE_INTEGRATED_AUTH%"=="" set "USE_INTEGRATED_AUTH=Y"

if /i "%USE_INTEGRATED_AUTH%"=="N" (
    set /p "DB_USER=数据库用户名: "
    set /p "DB_PASSWORD=数据库密码: "
    set "CONNECTION_STRING=Server=%DB_SERVER%;Database=%DB_NAME%;User Id=%DB_USER%;Password=%DB_PASSWORD%;TrustServerCertificate=true;"
) else (
    set "CONNECTION_STRING=Server=%DB_SERVER%;Database=%DB_NAME%;Integrated Security=true;TrustServerCertificate=true;"
)

:: JWT密钥生成（简单版本）
echo.
echo 🔐 正在生成JWT密钥...
set "JWT_SECRET=%RANDOM%%RANDOM%%RANDOM%%RANDOM%%RANDOM%%RANDOM%%RANDOM%%RANDOM%"

:: 管理员密码
set /p "ADMIN_PASSWORD=请设置管理员默认密码 (默认: Admin@123456): "
if "%ADMIN_PASSWORD%"=="" set "ADMIN_PASSWORD=Admin@123456"

:: 服务端口
set /p "SERVER_PORT=请设置服务端口 (默认: 5000): "
if "%SERVER_PORT%"=="" set "SERVER_PORT=5000"

:: 步骤3: 生成配置文件
echo.
echo 📝 第3步: 生成生产配置文件...

(
echo {
echo   "ConnectionStrings": {
echo     "DefaultConnection": "%CONNECTION_STRING%"
echo   },
echo   "JwtOptions": {
echo     "Secret": "%JWT_SECRET%",
echo     "Issuer": "LYBT.WebAPI",
echo     "Audience": "LYBT.Client",
echo     "ExpireMinutes": 480,
echo     "ClockSkewSeconds": 300
echo   },
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information",
echo       "Microsoft.AspNetCore": "Warning",
echo       "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
echo       "LYBT.Infrastructure.Database.DatabaseInitializationService": "Information"
echo     }
echo   },
echo   "AllowedHosts": "*",
echo   "AuthOptions": {
echo     "MaxFailedLoginAttempts": 5,
echo     "AccountLockoutDuration": "00:30:00",
echo     "EnableDetailedLoginLogging": false
echo   },
echo   "UserOptions": {
echo     "DefaultUserPassword": "User@123456",
echo     "EnableUserCache": true,
echo     "MaxBatchOperationSize": 50,
echo     "EnableDetailedAuditLogging": false
echo   },
echo   "SysAdminOptions": {
echo     "DefaultPassword": "%ADMIN_PASSWORD%",
echo     "RequirePasswordChangeOnFirstLogin": true,
echo     "EnableAccountLockout": true
echo   }
echo }
) > "%PUBLISH_DIR%\appsettings.Production.json"

:: 步骤4: 生成启动脚本
echo 📋 第4步: 生成启动脚本...

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
echo echo 📖 Swagger文档: http://localhost:%SERVER_PORT%/swagger
echo echo 🌐 API地址: http://localhost:%SERVER_PORT%/api
echo echo.
echo.
echo :: 设置环境变量
echo set ASPNETCORE_ENVIRONMENT=Production
echo set ASPNETCORE_URLS=http://localhost:%SERVER_PORT%
echo.
echo :: 显示启动信息
echo echo 📊 数据库: %DB_NAME% ^(服务器: %DB_SERVER%^)
echo echo 🔐 JWT过期时间: 8小时
echo echo ⚡ 端口: %SERVER_PORT%
echo echo.
echo.
echo :: 启动应用程序
echo LYBT.WebAPI.exe
echo.
echo if %%ERRORLEVEL%% neq 0 ^(
echo     echo.
echo     echo ❌ 应用程序启动失败！
echo     echo 💡 请检查配置和数据库连接
echo     pause
echo ^)
) > "%PUBLISH_DIR%\start-production.bat"

:: 步骤5: 生成安装指南
echo 📖 第5步: 生成安装指南...

(
echo # 凌隐宝堂中医诊所管理系统 - 生产环境部署指南
echo.
echo ## 部署信息
echo - 部署时间: %DATETIME%
echo - 数据库服务器: %DB_SERVER%
echo - 数据库名称: %DB_NAME%
echo - 服务端口: %SERVER_PORT%
echo - 管理员密码: %ADMIN_PASSWORD%
echo.
echo ## 安装步骤
echo.
echo ### 1. 环境要求
echo - Windows Server 2016 或更高版本
echo - .NET 8.0 Runtime ^(可从 https://dotnet.microsoft.com/download 下载^)
echo - SQL Server 2017 或更高版本 ^(或 SQL Server Express^)
echo.
echo ### 2. 数据库配置
echo 1. 确保SQL Server服务正在运行
echo 2. 创建数据库 `%DB_NAME%`
echo 3. 确保应用程序有访问数据库的权限
echo.
echo ### 3. 启动应用程序
echo 1. 双击 `start-production.bat` 启动服务器
echo 2. 等待数据库自动初始化完成
echo 3. 访问 http://localhost:%SERVER_PORT%/swagger 查看API文档
echo.
echo ### 4. 首次登录
echo - 管理员账号: admin
echo - 管理员密码: %ADMIN_PASSWORD%
echo - 登录后请及时修改密码
echo.
echo ### 5. 故障排除
echo - 检查端口 %SERVER_PORT% 是否被占用
echo - 检查数据库连接字符串是否正确
echo - 查看应用程序日志文件
echo.
echo ## 文件说明
echo - `LYBT.WebAPI.exe` - 主应用程序
echo - `appsettings.Production.json` - 生产环境配置
echo - `start-production.bat` - 启动脚本
echo - `install-guide.md` - 本安装指南
echo.
echo ## 安全建议
echo 1. 定期备份数据库
echo 2. 使用HTTPS ^(需要SSL证书^)
echo 3. 定期更新JWT密钥
echo 4. 启用防火墙保护
echo 5. 定期检查安全日志
) > "%PUBLISH_DIR%\install-guide.md"

:: 步骤6: 创建Windows服务安装脚本 (可选)
echo 🔧 第6步: 生成Windows服务安装脚本...

(
echo @echo off
echo title 安装凌隐宝堂系统为Windows服务
echo.
echo echo 正在安装为Windows服务...
echo.
echo :: 使用sc命令创建服务
echo sc create "LYBTWebAPI" binPath= "%%~dp0LYBT.WebAPI.exe" DisplayName= "凌隐宝堂中医诊所管理系统" start= auto
echo.
echo if %%ERRORLEVEL%% equ 0 ^(
echo     echo ✅ 服务安装成功！
echo     echo 💡 使用以下命令管理服务:
echo     echo    - 启动服务: sc start LYBTWebAPI
echo     echo    - 停止服务: sc stop LYBTWebAPI
echo     echo    - 删除服务: sc delete LYBTWebAPI
echo ^) else ^(
echo     echo ❌ 服务安装失败！
echo     echo 💡 请以管理员身份运行此脚本
echo ^)
echo.
echo pause
) > "%PUBLISH_DIR%\install-as-service.bat"

echo.
echo 🎉 部署完成！
echo.
echo 📁 部署文件位置: %PUBLISH_DIR%
echo.
echo 📋 生成的文件:
echo    ├─ LYBT.WebAPI.exe (主程序)
echo    ├─ start-production.bat (启动脚本)
echo    ├─ appsettings.Production.json (生产配置)
echo    ├─ install-guide.md (安装指南)
echo    └─ install-as-service.bat (服务安装脚本)
echo.
echo 🚀 快速启动:
echo    1. 双击 start-production.bat
echo    2. 访问 http://localhost:%SERVER_PORT%/swagger
echo.
echo 👤 管理员登录:
echo    用户名: admin
echo    密码: %ADMIN_PASSWORD%
echo.

set /p "OPEN_FOLDER=是否打开部署文件夹? (Y/N): "
if /i "%OPEN_FOLDER%"=="Y" (
    explorer "%PUBLISH_DIR%"
)

set /p "START_NOW=是否立即启动应用程序? (Y/N): "
if /i "%START_NOW%"=="Y" (
    cd /d "%PUBLISH_DIR%"
    start start-production.bat
)

pause