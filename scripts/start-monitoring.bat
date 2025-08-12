@echo off
REM 凌隐宝堂监控告警系统启动脚本 - UltraThink重构监控架构
echo ========================================
echo   凌隐宝堂监控告警系统启动脚本
echo   UltraThink重构监控架构
echo ========================================
echo.

REM 检查Docker是否已安装
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] Docker未安装或未在PATH中，请先安装Docker Desktop
    pause
    exit /b 1
)

REM 检查Docker Compose是否可用
docker-compose --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] Docker Compose未安装，请确保Docker Desktop正常运行
    pause
    exit /b 1
)

echo [信息] Docker环境检查通过
echo.

REM 检查配置文件
echo [检查] 验证监控配置文件...
if not exist "docker-compose.monitoring.yml" (
    echo [错误] docker-compose.monitoring.yml文件不存在
    pause
    exit /b 1
)

if not exist "Docker\prometheus\rules\lybt-alerts.yml" (
    echo [错误] Prometheus告警规则文件不存在
    pause
    exit /b 1
)

if not exist "Docker\alertmanager\alertmanager.yml" (
    echo [错误] AlertManager配置文件不存在
    pause
    exit /b 1
)

echo [信息] 配置文件检查通过
echo.

REM 检查端口占用
echo [检查] 检查关键端口占用情况...
netstat -an | find ":3000" | find "LISTENING" >nul
if %errorlevel% equ 0 (
    echo [警告] 端口3000已被占用（Grafana），请停止相关服务或修改端口配置
)

netstat -an | find ":9090" | find "LISTENING" >nul
if %errorlevel% equ 0 (
    echo [警告] 端口9090已被占用（Prometheus），请停止相关服务或修改端口配置
)

netstat -an | find ":5601" | find "LISTENING" >nul
if %errorlevel% equ 0 (
    echo [警告] 端口5601已被占用（Kibana），请停止相关服务或修改端口配置
)

echo.

REM 询问是否继续
echo 即将启动监控系统，包含以下组件：
echo   - Prometheus (监控指标收集): http://localhost:9090
echo   - Grafana (可视化仪表板): http://localhost:3000
echo   - AlertManager (告警管理): http://localhost:9093
echo   - Kibana (日志分析): http://localhost:5601
echo   - Elasticsearch (日志存储): http://localhost:9200
echo   - Jaeger (分布式追踪): http://localhost:16686
echo.
set /p confirm="是否继续启动？(Y/N): "
if /i not "%confirm%"=="Y" (
    echo 操作已取消
    pause
    exit /b 0
)

echo.
echo [启动] 正在启动监控告警系统...

REM 创建必要的目录
if not exist "logs" mkdir logs
if not exist "logs\webapi" mkdir logs\webapi
if not exist "logs\security" mkdir logs\security
if not exist "logs\database" mkdir logs\database
if not exist "logs\performance" mkdir logs\performance
if not exist "logs\system" mkdir logs\system

REM 启动监控栈
echo [启动] 启动监控服务容器...
docker-compose -f docker-compose.monitoring.yml up -d

REM 检查启动状态
if %errorlevel% neq 0 (
    echo [错误] 监控系统启动失败，请检查Docker日志
    pause
    exit /b 1
)

echo.
echo [等待] 等待服务启动完成...
timeout /t 30 /nobreak

REM 检查服务状态
echo [检查] 验证服务状态...
docker-compose -f docker-compose.monitoring.yml ps

echo.
echo [完成] 监控告警系统启动成功！
echo.
echo ========================================
echo   访问地址:
echo ========================================
echo   Prometheus:     http://localhost:9090
echo   Grafana:        http://localhost:3000
echo                   用户名: admin
echo                   密码: LYBT@Grafana2024
echo   AlertManager:   http://localhost:9093
echo   Kibana:         http://localhost:5601
echo   Jaeger:         http://localhost:16686
echo   Consul:         http://localhost:8500
echo ========================================
echo.
echo 提示:
echo   - 首次启动可能需要几分钟来初始化Elasticsearch
echo   - Grafana仪表板已预配置，数据源会自动连接Prometheus
echo   - 日志收集需要Web API运行并产生日志文件
echo.
echo 如需停止监控系统，请运行: scripts\stop-monitoring.bat
echo.

REM 询问是否打开浏览器
set /p openBrowser="是否打开监控界面？(Y/N): "
if /i "%openBrowser%"=="Y" (
    echo [启动] 打开监控界面...
    start http://localhost:3000
    timeout /t 2 /nobreak >nul
    start http://localhost:9090
)

echo.
echo 监控系统启动完成！
pause