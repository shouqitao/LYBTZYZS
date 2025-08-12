@echo off
REM 凌隐宝堂监控告警系统停止脚本 - UltraThink重构监控架构
echo ========================================
echo   凌隐宝堂监控告警系统停止脚本
echo   UltraThink重构监控架构
echo ========================================
echo.

REM 检查Docker是否已安装
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] Docker未安装或未在PATH中
    pause
    exit /b 1
)

REM 检查配置文件
if not exist "docker-compose.monitoring.yml" (
    echo [错误] docker-compose.monitoring.yml文件不存在
    echo 请确保在项目根目录下运行此脚本
    pause
    exit /b 1
)

echo [信息] 正在检查监控系统状态...
docker-compose -f docker-compose.monitoring.yml ps

echo.
set /p confirm="确认停止所有监控服务？(Y/N): "
if /i not "%confirm%"=="Y" (
    echo 操作已取消
    pause
    exit /b 0
)

echo.
echo [停止] 正在停止监控告警系统...

REM 停止监控栈
docker-compose -f docker-compose.monitoring.yml down

if %errorlevel% neq 0 (
    echo [错误] 停止监控系统时发生错误
    pause
    exit /b 1
)

echo.
echo [完成] 监控告警系统已停止

echo.
set /p removeVolumes="是否删除数据卷？(这将清除所有监控数据) (Y/N): "
if /i "%removeVolumes%"=="Y" (
    echo [清理] 正在删除监控数据卷...
    docker-compose -f docker-compose.monitoring.yml down -v
    echo [完成] 监控数据已清理
) else (
    echo [保留] 监控数据已保留，下次启动时将恢复历史数据
)

echo.
set /p removeImages="是否删除监控系统镜像？(节省磁盘空间) (Y/N): "
if /i "%removeImages%"=="Y" (
    echo [清理] 正在删除监控系统镜像...
    docker image rm prom/prometheus:v2.47.0 >nul 2>&1
    docker image rm prom/alertmanager:v0.26.0 >nul 2>&1
    docker image rm grafana/grafana:10.1.2 >nul 2>&1
    docker image rm prom/node-exporter:v1.6.1 >nul 2>&1
    docker image rm gcr.io/cadvisor/cadvisor:v0.47.2 >nul 2>&1
    docker image rm docker.elastic.co/elasticsearch/elasticsearch:8.9.2 >nul 2>&1
    docker image rm docker.elastic.co/logstash/logstash:8.9.2 >nul 2>&1
    docker image rm docker.elastic.co/kibana/kibana:8.9.2 >nul 2>&1
    docker image rm docker.elastic.co/beats/filebeat:8.9.2 >nul 2>&1
    docker image rm jaegertracing/all-in-one:1.48 >nul 2>&1
    docker image rm consul:1.16.1 >nul 2>&1
    docker image rm nginx:alpine >nul 2>&1
    echo [完成] 监控系统镜像已删除
) else (
    echo [保留] 镜像已保留，下次启动将更快
)

echo.
echo [清理] 正在清理未使用的Docker资源...
docker system prune -f >nul 2>&1

echo.
echo ========================================
echo   监控系统停止完成
echo ========================================
echo.
echo 状态总结:
echo   - 所有监控服务容器已停止
if /i "%removeVolumes%"=="Y" (
    echo   - 监控数据已清理
) else (
    echo   - 监控数据已保留
)
if /i "%removeImages%"=="Y" (
    echo   - 镜像已删除，节省磁盘空间
) else (
    echo   - 镜像已保留，便于快速重启
)
echo.
echo 如需重新启动监控系统，请运行: scripts\start-monitoring.bat
echo.

pause