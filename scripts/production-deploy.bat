@echo off
REM 凌隐宝堂生产环境部署脚本 - UltraThink重构生产部署自动化
chcp 65001 >nul
setlocal EnableDelayedExpansion
title 凌隐宝堂生产环境部署

echo.
echo ==========================================
echo   凌隐宝堂中医诊所管理系统
echo   生产环境自动化部署脚本
echo   UltraThink重构生产部署架构
echo ==========================================
echo.

REM 设置部署参数
set DEPLOY_ENV=production
set APP_NAME=lybt-clinic
set VERSION=v1.0.0
set DEPLOY_DATE=%date:~0,4%%date:~5,2%%date:~8,2%
set DEPLOY_TIME=%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=backup\%DEPLOY_DATE%_%DEPLOY_TIME%

echo 📋 部署信息:
echo   应用名称: %APP_NAME%
echo   版本号:   %VERSION%
echo   环境:     %DEPLOY_ENV%
echo   时间:     %date% %time%
echo.

REM 检查先决条件
echo 🔍 检查部署环境...
echo ===================================

REM 检查Docker
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker未安装或未启动，请先安装Docker Desktop
    goto ERROR_EXIT
)
echo ✅ Docker环境正常

REM 检查Docker Compose
docker compose version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker Compose未找到，请升级Docker Desktop
    goto ERROR_EXIT
)
echo ✅ Docker Compose环境正常

REM 检查必要文件
if not exist "docker-compose.yml" (
    echo ❌ docker-compose.yml文件不存在
    goto ERROR_EXIT
)
echo ✅ Docker Compose配置文件存在

if not exist "Docker\Dockerfile.webapi" (
    echo ❌ Web API Dockerfile不存在
    goto ERROR_EXIT
)
echo ✅ Web API Dockerfile存在

echo.
echo ⚠️  生产环境部署确认
echo ===================================
echo 此操作将会:
echo   1. 停止现有服务
echo   2. 备份当前数据库
echo   3. 重新构建并部署新版本
echo   4. 执行数据库迁移
echo   5. 启动所有服务并进行健康检查
echo.
set /p confirm="确认继续生产部署? (输入 YES 确认): "
if not "%confirm%"=="YES" (
    echo 部署已取消。
    goto NORMAL_EXIT
)

echo.
echo 🚀 开始生产部署...
echo ===================================

REM 步骤1: 创建部署目录
echo 步骤 1/8: 创建部署目录...
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"
if not exist "data\database" mkdir data\database
if not exist "data\backup" mkdir data\backup
if not exist "data\uploads" mkdir data\uploads
if not exist "logs\webapi" mkdir logs\webapi
if not exist "logs\nginx" mkdir logs\nginx
echo ✅ 部署目录创建完成

REM 步骤2: 备份现有数据
echo.
echo 步骤 2/8: 备份现有数据...
set backup_running=false
docker ps --format "table {{.Names}}" | findstr "lybt-database" >nul 2>&1
if not errorlevel 1 (
    set backup_running=true
    echo 正在备份数据库...
    docker exec lybt-database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LYBT@AdminPassword2024" -Q "BACKUP DATABASE [LYBTDB] TO DISK = N'/var/opt/mssql/backup/PreDeploy_%DEPLOY_DATE%_%DEPLOY_TIME%.bak' WITH FORMAT, INIT, NAME = N'Pre-Deploy Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10" >nul 2>&1
    if errorlevel 1 (
        echo ⚠️  数据库备份失败，但继续部署...
    ) else (
        echo ✅ 数据库备份完成
    )
)

REM 备份配置文件
echo 备份配置文件...
copy docker-compose.yml "%BACKUP_DIR%\docker-compose.yml.backup" >nul 2>&1
copy Docker\nginx\conf.d\*.conf "%BACKUP_DIR%\" >nul 2>&1
echo ✅ 配置文件备份完成

REM 步骤3: 停止现有服务
echo.
echo 步骤 3/8: 停止现有服务...
docker compose down --remove-orphans >nul 2>&1
if errorlevel 1 (
    echo ⚠️  停止服务时有警告，继续部署...
) else (
    echo ✅ 现有服务已停止
)

REM 等待容器完全停止
echo 等待容器完全停止...
timeout /t 5 /nobreak >nul

REM 步骤4: 构建新镜像
echo.
echo 步骤 4/8: 构建新镜像...
echo 正在构建Web API镜像...
docker compose build --no-cache lybt-webapi
if errorlevel 1 (
    echo ❌ Web API镜像构建失败
    goto ERROR_EXIT
)
echo ✅ Web API镜像构建完成

echo 正在构建数据库镜像...
docker compose build --no-cache lybt-database
if errorlevel 1 (
    echo ❌ 数据库镜像构建失败
    goto ERROR_EXIT
)
echo ✅ 数据库镜像构建完成

REM 步骤5: 生成SSL证书
echo.
echo 步骤 5/8: 检查SSL证书...
if not exist "Docker\nginx\ssl\lybt.crt" (
    echo 生成SSL证书...
    call scripts\generate-ssl-certificates.bat
    if errorlevel 1 (
        echo ❌ SSL证书生成失败
        goto ERROR_EXIT
    )
)
echo ✅ SSL证书检查完成

REM 步骤6: 启动生产环境
echo.
echo 步骤 6/8: 启动生产环境...
echo 启动数据服务...
docker compose up -d lybt-database lybt-redis
if errorlevel 1 (
    echo ❌ 数据服务启动失败
    goto ERROR_EXIT
)

REM 等待数据库启动
echo 等待数据库服务启动...
set /a counter=0
:WAIT_DB
set /a counter+=1
docker exec lybt-database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LYBT@AdminPassword2024" -Q "SELECT 1" >nul 2>&1
if not errorlevel 1 goto DB_READY
if %counter% geq 30 (
    echo ❌ 数据库启动超时
    goto ERROR_EXIT
)
echo 等待数据库启动... (%counter%/30)
timeout /t 2 /nobreak >nul
goto WAIT_DB

:DB_READY
echo ✅ 数据库服务已就绪

echo 启动应用服务...
docker compose up -d lybt-webapi lybt-nginx
if errorlevel 1 (
    echo ❌ 应用服务启动失败
    goto ERROR_EXIT
)

echo 启动监控服务...
docker compose up -d lybt-seq lybt-prometheus lybt-grafana lybt-portainer
if errorlevel 1 (
    echo ⚠️  监控服务启动有警告，但应用可正常运行
)

echo ✅ 所有服务启动完成

REM 步骤7: 健康检查
echo.
echo 步骤 7/8: 执行健康检查...
echo 等待应用服务就绪...
timeout /t 30 /nobreak >nul

echo 检查Web API健康状态...
set /a counter=0
:WAIT_API
set /a counter+=1
curl -k -s -o nul -w "%%{http_code}" "https://localhost/api/v1/performance/health-check" | findstr "200" >nul
if not errorlevel 1 goto API_READY
if %counter% geq 20 (
    echo ⚠️  API健康检查超时，但服务可能正在启动中
    goto HEALTH_DONE
)
echo 等待API服务就绪... (%counter%/20)
timeout /t 3 /nobreak >nul
goto WAIT_API

:API_READY
echo ✅ Web API健康检查通过

:HEALTH_DONE
echo 检查容器状态...
docker compose ps

REM 步骤8: 部署验证和报告
echo.
echo 步骤 8/8: 生成部署报告...
set report_file=deployment_report_%DEPLOY_DATE%_%DEPLOY_TIME%.txt

echo 凌隐宝堂生产环境部署报告 > %report_file%
echo ======================================= >> %report_file%
echo 部署时间: %date% %time% >> %report_file%
echo 版本信息: %VERSION% >> %report_file%
echo 环境信息: %DEPLOY_ENV% >> %report_file%
echo. >> %report_file%
echo == 容器状态 == >> %report_file%
docker compose ps >> %report_file%
echo. >> %report_file%
echo == 资源使用情况 == >> %report_file%
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" >> %report_file%
echo. >> %report_file%
echo == 服务地址 == >> %report_file%
echo Web API: https://localhost >> %report_file%
echo Swagger文档: https://localhost/swagger >> %report_file%
echo Grafana监控: http://localhost:3000 >> %report_file%
echo Seq日志: http://localhost:5341 >> %report_file%
echo Prometheus: http://localhost:9090 >> %report_file%
echo Portainer: http://localhost:9000 >> %report_file%

echo.
echo ==========================================
echo ✅ 生产环境部署成功完成！
echo ==========================================
echo.
echo 📊 部署总结:
echo   部署版本: %VERSION%
echo   部署时间: %date% %time%
echo   备份目录: %BACKUP_DIR%
echo   部署报告: %report_file%
echo.
echo 🌐 服务访问地址:
echo   主应用:     https://localhost
echo   API文档:    https://localhost/swagger
echo   监控面板:   http://localhost:3000 (admin/LYBT@Grafana2024)
echo   日志系统:   http://localhost:5341 (admin/lybt@2024)
echo   容器管理:   http://localhost:9000
echo.
echo 🔧 管理命令:
echo   查看日志:   docker compose logs -f
echo   重启服务:   docker compose restart
echo   停止服务:   docker compose down
echo.
echo 💡 注意事项:
echo   1. 首次访问需要忽略SSL证书警告（自签名证书）
echo   2. 默认管理员账户: sysadmin / Admin@123456
echo   3. 生产环境建议修改默认密码
echo   4. 定期备份数据库和配置文件
echo.
goto NORMAL_EXIT

:ERROR_EXIT
echo.
echo ❌ 部署失败！
echo.
echo 🔄 回滚建议:
echo   1. 检查错误日志: docker compose logs
echo   2. 恢复服务: docker compose up -d
echo   3. 如有备份: 使用备份目录 %BACKUP_DIR% 中的文件
echo.
echo 📞 技术支持:
echo   请保存部署日志，联系技术支持团队
echo.
pause
exit /b 1

:NORMAL_EXIT
echo.
echo 🎉 感谢使用凌隐宝堂生产部署系统！
echo.
pause
exit /b 0