@echo off
REM 凌隐宝堂Docker容器管理脚本 - UltraThink重构容器化管理
chcp 65001 >nul
title 凌隐宝堂Docker管理器

echo.
echo ==========================================
echo   凌隐宝堂中医诊所管理系统 - Docker管理器
echo   UltraThink重构容器化部署管理
echo ==========================================
echo.

:MAIN_MENU
echo 请选择操作:
echo.
echo 【环境管理】
echo   1. 快速启动所有服务
echo   2. 停止所有服务
echo   3. 重启所有服务
echo   4. 查看服务状态
echo.
echo 【开发模式】
echo   5. 启动开发环境 (API + 数据库 + Redis)
echo   6. 启动完整环境 (包含监控)
echo   7. 仅启动数据库和缓存
echo.
echo 【维护操作】
echo   8. 查看容器日志
echo   9. 清理容器和数据卷
echo  10. 重新构建镜像
echo  11. 备份数据库
echo  12. 恢复数据库
echo.
echo 【监控调试】
echo  13. 查看性能指标
echo  14. 执行健康检查
echo  15. 生成部署报告
echo.
echo  16. 打开管理面板
echo   0. 退出
echo.
set /p choice="请输入选择 (0-16): "

if "%choice%"=="1" goto START_ALL
if "%choice%"=="2" goto STOP_ALL
if "%choice%"=="3" goto RESTART_ALL
if "%choice%"=="4" goto STATUS
if "%choice%"=="5" goto START_DEV
if "%choice%"=="6" goto START_FULL
if "%choice%"=="7" goto START_DATA
if "%choice%"=="8" goto VIEW_LOGS
if "%choice%"=="9" goto CLEANUP
if "%choice%"=="10" goto REBUILD
if "%choice%"=="11" goto BACKUP_DB
if "%choice%"=="12" goto RESTORE_DB
if "%choice%"=="13" goto VIEW_METRICS
if "%choice%"=="14" goto HEALTH_CHECK
if "%choice%"=="15" goto DEPLOY_REPORT
if "%choice%"=="16" goto OPEN_PANELS
if "%choice%"=="0" goto EXIT

echo 无效选择，请重试。
pause
goto MAIN_MENU

:START_ALL
echo.
echo 🚀 启动所有服务...
echo ===================================
echo 正在检查Docker环境...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ 错误: Docker未安装或未启动
    pause
    goto MAIN_MENU
)

echo 正在检查Docker Compose...
docker compose version >nul 2>&1
if errorlevel 1 (
    echo ❌ 错误: Docker Compose未安装
    pause
    goto MAIN_MENU
)

echo 创建必要的目录...
if not exist "data\database" mkdir data\database
if not exist "data\backup" mkdir data\backup
if not exist "data\uploads" mkdir data\uploads
if not exist "logs\webapi" mkdir logs\webapi
if not exist "logs\nginx" mkdir logs\nginx

echo 启动容器组...
docker compose up -d
if errorlevel 1 (
    echo ❌ 容器启动失败，请检查配置
    pause
    goto MAIN_MENU
)

echo ✅ 所有服务启动成功！
echo.
echo 📊 服务访问地址:
echo   - Web API:      https://localhost
echo   - Swagger文档:  https://localhost/swagger
echo   - Grafana监控:  http://localhost:3000 (admin/LYBT@Grafana2024)
echo   - Seq日志:      http://localhost:5341 (admin/lybt@2024)
echo   - Prometheus:   http://localhost:9090
echo   - Portainer:    http://localhost:9000
echo.
pause
goto MAIN_MENU

:STOP_ALL
echo.
echo 🛑 停止所有服务...
docker compose down
echo ✅ 所有服务已停止！
pause
goto MAIN_MENU

:RESTART_ALL
echo.
echo 🔄 重启所有服务...
docker compose restart
if errorlevel 1 (
    echo ❌ 重启失败
    pause
    goto MAIN_MENU
)
echo ✅ 所有服务已重启！
pause
goto MAIN_MENU

:STATUS
echo.
echo 📊 服务状态检查...
echo ===================================
docker compose ps
echo.
echo 🔍 容器资源使用情况:
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"
echo.
pause
goto MAIN_MENU

:START_DEV
echo.
echo 🔧 启动开发环境...
docker compose up -d lybt-database lybt-redis lybt-webapi
echo ✅ 开发环境启动完成！
echo   - API地址: http://localhost:5000
echo   - 数据库: localhost:1433
echo   - Redis: localhost:6379
pause
goto MAIN_MENU

:START_FULL
echo.
echo 🚀 启动完整环境 (包含监控)...
docker compose up -d
echo ✅ 完整环境启动成功！
pause
goto MAIN_MENU

:START_DATA
echo.
echo 💾 启动数据服务...
docker compose up -d lybt-database lybt-redis
echo ✅ 数据服务启动完成！
pause
goto MAIN_MENU

:VIEW_LOGS
echo.
echo 📋 选择要查看日志的服务:
echo   1. Web API
echo   2. 数据库
echo   3. Redis
echo   4. Nginx
echo   5. 所有服务
echo.
set /p log_choice="请选择 (1-5): "

if "%log_choice%"=="1" docker compose logs -f --tail=50 lybt-webapi
if "%log_choice%"=="2" docker compose logs -f --tail=50 lybt-database
if "%log_choice%"=="3" docker compose logs -f --tail=50 lybt-redis
if "%log_choice%"=="4" docker compose logs -f --tail=50 lybt-nginx
if "%log_choice%"=="5" docker compose logs -f --tail=50

pause
goto MAIN_MENU

:CLEANUP
echo.
echo ⚠️  警告: 此操作将删除所有容器和数据！
echo 这个操作不可逆，所有数据将丢失！
echo.
set /p confirm="确认清理? (输入 YES 确认): "
if not "%confirm%"=="YES" (
    echo 操作已取消。
    pause
    goto MAIN_MENU
)

echo 🗑️  清理容器和数据卷...
docker compose down -v --remove-orphans
docker system prune -f
docker volume prune -f
echo ✅ 清理完成！
pause
goto MAIN_MENU

:REBUILD
echo.
echo 🔨 重新构建镜像...
echo 选择构建选项:
echo   1. 仅重新构建Web API
echo   2. 仅重新构建数据库
echo   3. 重新构建所有镜像
echo.
set /p rebuild_choice="请选择 (1-3): "

if "%rebuild_choice%"=="1" (
    docker compose build --no-cache lybt-webapi
) else if "%rebuild_choice%"=="2" (
    docker compose build --no-cache lybt-database
) else if "%rebuild_choice%"=="3" (
    docker compose build --no-cache
) else (
    echo 无效选择。
    pause
    goto MAIN_MENU
)

echo ✅ 镜像重新构建完成！
pause
goto MAIN_MENU

:BACKUP_DB
echo.
echo 💾 备份数据库...
set backup_file=backup\LYBTDB_%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%.bak
echo 创建备份: %backup_file%

docker exec lybt-database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LYBT@AdminPassword2024" -Q "BACKUP DATABASE [LYBTDB] TO DISK = N'/var/opt/mssql/backup/%backup_file%' WITH FORMAT, INIT, NAME = N'LYBTDB-Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

if errorlevel 1 (
    echo ❌ 备份失败
) else (
    echo ✅ 数据库备份成功: %backup_file%
)
pause
goto MAIN_MENU

:RESTORE_DB
echo.
echo 📂 可用备份文件:
docker exec lybt-database ls -la /var/opt/mssql/backup/
echo.
set /p backup_name="请输入备份文件名: "

echo 🔄 恢复数据库...
docker exec lybt-database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LYBT@AdminPassword2024" -Q "RESTORE DATABASE [LYBTDB] FROM DISK = N'/var/opt/mssql/backup/%backup_name%' WITH REPLACE, STATS = 10"

if errorlevel 1 (
    echo ❌ 恢复失败
) else (
    echo ✅ 数据库恢复成功
)
pause
goto MAIN_MENU

:VIEW_METRICS
echo.
echo 📊 查看性能指标...
echo ===================================
echo 正在获取API性能数据...

curl -k -s "https://localhost/api/v1/performance/health-check" > temp_health.json 2>nul
if exist temp_health.json (
    echo ✅ API健康检查通过
    type temp_health.json
    del temp_health.json
) else (
    echo ❌ 无法获取API健康数据
)

echo.
echo 容器资源使用:
docker stats --no-stream
pause
goto MAIN_MENU

:HEALTH_CHECK
echo.
echo 🏥 执行系统健康检查...
echo ===================================

echo 检查容器状态...
docker compose ps

echo.
echo 检查服务端点...
echo 检查 Web API...
curl -k -s -o nul -w "API健康状态: %%{http_code}\n" "https://localhost/api/v1/performance/health-check"

echo 检查数据库连接...
docker exec lybt-database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LYBT@AdminPassword2024" -Q "SELECT 1 as HealthCheck" -h -1

echo 检查Redis连接...
docker exec lybt-redis redis-cli -a "LYBT@Redis2024" ping

echo.
echo ✅ 健康检查完成！
pause
goto MAIN_MENU

:DEPLOY_REPORT
echo.
echo 📋 生成部署报告...
echo ===================================
set report_file=deployment_report_%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%.txt

echo 凌隐宝堂系统部署报告 > %report_file%
echo 生成时间: %date% %time% >> %report_file%
echo. >> %report_file%

echo == 容器状态 == >> %report_file%
docker compose ps >> %report_file%
echo. >> %report_file%

echo == 资源使用 == >> %report_file%
docker stats --no-stream >> %report_file%
echo. >> %report_file%

echo == 网络信息 == >> %report_file%
docker network ls >> %report_file%
echo. >> %report_file%

echo == 数据卷信息 == >> %report_file%
docker volume ls >> %report_file%

echo ✅ 部署报告已生成: %report_file%
pause
goto MAIN_MENU

:OPEN_PANELS
echo.
echo 🌐 打开管理面板...
echo ===================================
echo 正在打开浏览器...

start "" "https://localhost/swagger"
timeout /t 2 /nobreak >nul
start "" "http://localhost:3000"
timeout /t 2 /nobreak >nul
start "" "http://localhost:5341"
timeout /t 2 /nobreak >nul
start "" "http://localhost:9000"

echo ✅ 管理面板已在浏览器中打开！
pause
goto MAIN_MENU

:EXIT
echo.
echo 👋 感谢使用凌隐宝堂Docker管理器！
echo.
pause
exit

REM 错误处理
:ERROR
echo.
echo ❌ 发生错误，请检查Docker环境和配置。
pause
goto MAIN_MENU