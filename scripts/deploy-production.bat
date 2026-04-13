@echo off
REM ================================================
REM 凌隐宝堂中医诊所系统 - 生产环境部署脚本
REM UltraThink Stage 5.3.3 - 自动化部署
REM ================================================

setlocal enabledelayedexpansion

REM 设置基础变量
set PROJECT_NAME=LYBT
set VERSION=1.0.0
set BUILD_CONFIG=Release
set PLATFORM=x64
set TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%

REM 设置路径
set ROOT_DIR=%~dp0..
set BACKEND_DIR=%ROOT_DIR%\src\Backend
set FRONTEND_DIR=%ROOT_DIR%\src\Frontend\Desktop
set OUTPUT_DIR=%ROOT_DIR%\BIN\Production_%TIMESTAMP%
set PACKAGE_DIR=%ROOT_DIR%\Packages
set LOGS_DIR=%ROOT_DIR%\DeploymentLogs

REM 创建必要的目录
echo [1/10] 创建输出目录...
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%PACKAGE_DIR%" mkdir "%PACKAGE_DIR%"
if not exist "%LOGS_DIR%" mkdir "%LOGS_DIR%"

REM 设置日志文件
set LOG_FILE=%LOGS_DIR%\deploy_%TIMESTAMP%.log
echo 部署开始: %date% %time% >> "%LOG_FILE%"

REM ================================================
REM 步骤1: 清理旧构建
REM ================================================
echo [2/10] 清理旧构建...
echo 清理旧构建... >> "%LOG_FILE%"

dotnet clean %ROOT_DIR%\LYBTZYZS.sln -c %BUILD_CONFIG% >> "%LOG_FILE%" 2>&1
if %errorlevel% neq 0 (
    echo 错误: 清理失败
    echo 清理失败 >> "%LOG_FILE%"
    goto :error
)

REM ================================================
REM 步骤2: 还原依赖包
REM ================================================
echo [3/10] 还原NuGet包...
echo 还原NuGet包... >> "%LOG_FILE%"

dotnet restore %ROOT_DIR%\LYBTZYZS.sln >> "%LOG_FILE%" 2>&1
if %errorlevel% neq 0 (
    echo 错误: 还原包失败
    echo 还原包失败 >> "%LOG_FILE%"
    goto :error
)

REM ================================================
REM 步骤3: 构建后端项目
REM ================================================
echo [4/10] 构建后端服务...
echo 构建后端服务... >> "%LOG_FILE%"

dotnet publish %BACKEND_DIR%\Services\LYBT.WebAPI\LYBT.WebAPI.csproj ^
    -c %BUILD_CONFIG% ^
    -r win-%PLATFORM% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%\Backend" >> "%LOG_FILE%" 2>&1

if %errorlevel% neq 0 (
    echo 错误: 后端构建失败
    echo 后端构建失败 >> "%LOG_FILE%"
    goto :error
)

REM ================================================
REM 步骤4: 构建前端项目
REM ================================================
echo [5/10] 构建前端应用...
echo 构建前端应用... >> "%LOG_FILE%"

dotnet publish %FRONTEND_DIR%\Shell\LYBT.WPF.Client.Shell.csproj ^
    -c %BUILD_CONFIG% ^
    -r win-%PLATFORM% ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%\Frontend" >> "%LOG_FILE%" 2>&1

if %errorlevel% neq 0 (
    echo 错误: 前端构建失败
    echo 前端构建失败 >> "%LOG_FILE%"
    goto :error
)

REM ================================================
REM 步骤5: 复制配置文件
REM ================================================
echo [6/10] 复制配置文件...
echo 复制配置文件... >> "%LOG_FILE%"

REM 后端配置
copy "%BACKEND_DIR%\Services\LYBT.WebAPI\appsettings.json" "%OUTPUT_DIR%\Backend\" >> "%LOG_FILE%" 2>&1
copy "%BACKEND_DIR%\Services\LYBT.WebAPI\appsettings.Production.json" "%OUTPUT_DIR%\Backend\" >> "%LOG_FILE%" 2>&1

REM 前端配置
if not exist "%OUTPUT_DIR%\Frontend\Config" mkdir "%OUTPUT_DIR%\Frontend\Config"
echo {} > "%OUTPUT_DIR%\Frontend\Config\user.config.json"

REM ================================================
REM 步骤6: 复制资源文件
REM ================================================
echo [7/10] 复制资源文件...
echo 复制资源文件... >> "%LOG_FILE%"

REM 复制图标和图片资源
xcopy /E /I /Y "%FRONTEND_DIR%\Assets" "%OUTPUT_DIR%\Frontend\Assets" >> "%LOG_FILE%" 2>&1

REM 复制数据库脚本
if not exist "%OUTPUT_DIR%\Database" mkdir "%OUTPUT_DIR%\Database"
xcopy /E /I /Y "%ROOT_DIR%\Database\Scripts" "%OUTPUT_DIR%\Database\Scripts" >> "%LOG_FILE%" 2>&1

REM ================================================
REM 步骤7: 生成版本信息
REM ================================================
echo [8/10] 生成版本信息...
echo 生成版本信息... >> "%LOG_FILE%"

(
    echo {
    echo   "version": "%VERSION%",
    echo   "buildNumber": "%TIMESTAMP%",
    echo   "buildDate": "%date% %time%",
    echo   "buildConfig": "%BUILD_CONFIG%",
    echo   "platform": "%PLATFORM%"
    echo }
) > "%OUTPUT_DIR%\version.json"

REM ================================================
REM 步骤8: 创建安装包
REM ================================================
echo [9/10] 创建安装包...
echo 创建安装包... >> "%LOG_FILE%"

set PACKAGE_NAME=%PROJECT_NAME%_%VERSION%_%TIMESTAMP%.zip
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%PACKAGE_DIR%\%PACKAGE_NAME%' -CompressionLevel Optimal" >> "%LOG_FILE%" 2>&1

if %errorlevel% neq 0 (
    echo 警告: 创建压缩包失败，但部署继续
    echo 创建压缩包失败 >> "%LOG_FILE%"
)

REM ================================================
REM 步骤9: 生成部署清单
REM ================================================
echo [10/10] 生成部署清单...
echo 生成部署清单... >> "%LOG_FILE%"

(
    echo ========================================
    echo 部署清单
    echo ========================================
    echo 项目: %PROJECT_NAME%
    echo 版本: %VERSION%
    echo 构建号: %TIMESTAMP%
    echo 配置: %BUILD_CONFIG%
    echo 平台: %PLATFORM%
    echo ----------------------------------------
    echo 输出目录: %OUTPUT_DIR%
    echo 安装包: %PACKAGE_DIR%\%PACKAGE_NAME%
    echo ----------------------------------------
    echo 部署时间: %date% %time%
    echo ========================================
) > "%OUTPUT_DIR%\DEPLOYMENT_MANIFEST.txt"

REM ================================================
REM 完成
REM ================================================
echo.
echo ================================================
echo 部署成功完成！
echo ================================================
echo 输出目录: %OUTPUT_DIR%
echo 安装包: %PACKAGE_DIR%\%PACKAGE_NAME%
echo 日志文件: %LOG_FILE%
echo ================================================
echo.

echo 部署成功: %date% %time% >> "%LOG_FILE%"

REM 打开输出目录
explorer "%OUTPUT_DIR%"

goto :end

:error
echo.
echo ================================================
echo 部署失败！
echo ================================================
echo 请查看日志文件: %LOG_FILE%
echo ================================================
echo.
echo 部署失败: %date% %time% >> "%LOG_FILE%"
exit /b 1

:end
endlocal
exit /b 0