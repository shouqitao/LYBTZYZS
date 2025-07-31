@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion

echo ====================================
echo     LYBT WebAPI 服务健康检查
echo ====================================
echo.

:: 配置变量
set "SERVICE_NAME=LYBTWebAPI"
set "API_URL=http://localhost:5297"
set "HEALTH_URL=!API_URL!/health"
set "DEPLOY_PATH=C:\LYBT\WebAPI"

echo [检查 1] 检查部署目录...
if exist "!DEPLOY_PATH!" (
    echo ✅ 部署目录存在: !DEPLOY_PATH!
    
    if exist "!DEPLOY_PATH!\LYBT.WebAPI.exe" (
        echo ✅ WebAPI可执行文件存在
    ) else (
        echo ❌ WebAPI可执行文件不存在
        goto :error
    )
) else (
    echo ❌ 部署目录不存在: !DEPLOY_PATH!
    goto :error
)

echo [检查 2] 检查进程状态...
tasklist | findstr "LYBT.WebAPI.exe" >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ WebAPI进程正在运行
    for /f "tokens=2" %%i in ('tasklist ^| findstr "LYBT.WebAPI.exe"') do (
        echo    进程ID: %%i
    )
) else (
    echo ⚠️  WebAPI进程未运行
)

echo [检查 3] 检查Windows服务状态...
sc query "!SERVICE_NAME!" >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ Windows服务已安装
    for /f "tokens=3" %%i in ('sc query "!SERVICE_NAME!" ^| findstr "STATE"') do (
        if "%%i"=="RUNNING" (
            echo ✅ 服务状态: 运行中
        ) else (
            echo ⚠️  服务状态: %%i
        )
    )
) else (
    echo ⚠️  Windows服务未安装
)

echo [检查 4] 检查端口监听...
netstat -an | findstr ":5297" >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ 端口5297正在监听
    netstat -an | findstr ":5297" | findstr "LISTENING"
) else (
    echo ❌ 端口5297未监听
    goto :error
)

echo [检查 5] 检查API健康状态...
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; try { $response = Invoke-RestMethod -Uri '!HEALTH_URL!' -TimeoutSec 5 -ErrorAction Stop; if($response.status -eq 'healthy') { Write-Host '✅ API健康检查通过' -ForegroundColor Green; Write-Host '   响应时间:' $response.timestamp } else { Write-Host '⚠️  API响应异常' -ForegroundColor Yellow } } catch { Write-Host '❌ API健康检查失败:' $_.Exception.Message -ForegroundColor Red }}"

echo [检查 6] 检查数据库连接...
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; try { $response = Invoke-RestMethod -Uri '!API_URL!/api/v1/System/database-status' -TimeoutSec 10 -ErrorAction Stop; Write-Host '✅ 数据库连接正常' -ForegroundColor Green } catch { Write-Host '⚠️  数据库连接测试失败' -ForegroundColor Yellow }}"

echo [检查 7] 检查认证接口...
powershell -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; try { $body = @{username='test';password='test'} | ConvertTo-Json; $response = Invoke-RestMethod -Uri '!API_URL!/api/v1/Auth/login' -Method POST -Body $body -ContentType 'application/json' -TimeoutSec 10 -ErrorAction Stop; Write-Host '✅ 认证接口响应正常' -ForegroundColor Green } catch { if($_.Exception.Response.StatusCode -eq 'Unauthorized' -or $_.Exception.Response.StatusCode -eq 'BadRequest') { Write-Host '✅ 认证接口工作正常（拒绝无效凭据）' -ForegroundColor Green } else { Write-Host '⚠️  认证接口异常:' $_.Exception.Message -ForegroundColor Yellow } }}"

echo [检查 8] 检查日志文件...
set "LOG_PATH=C:\LYBT\Logs"
if exist "!LOG_PATH!" (
    echo ✅ 日志目录存在: !LOG_PATH!
    
    if exist "!LOG_PATH!\deploy.log" (
        echo ✅ 部署日志文件存在
        for /f %%i in ('dir "!LOG_PATH!\deploy.log" ^| findstr "deploy.log"') do (
            echo    最后修改: %%i
        )
    ) else (
        echo ⚠️  部署日志文件不存在
    )
) else (
    echo ⚠️  日志目录不存在
)

echo [检查 9] 系统资源使用情况...
echo CPU和内存使用情况:
wmic process where "name='LYBT.WebAPI.exe'" get ProcessId,PageFileUsage,WorkingSetSize /format:table 2>nul
if !errorlevel! neq 0 (
    echo ⚠️  无法获取资源使用情况
)

echo.
echo ====================================
echo 📊 健康检查完成
echo ====================================
echo.
echo 🕒 检查时间: !date! !time!
echo 🌐 API地址: !API_URL!
echo 📁 部署路径: !DEPLOY_PATH!
echo.

:: 生成检查报告
set "REPORT_FILE=C:\LYBT\Logs\health-check-!date:~0,4!!date:~5,2!!date:~8,2!-!time:~0,2!!time:~3,2!!time:~6,2!.txt"
set "REPORT_FILE=!REPORT_FILE: =!"
(
echo LYBT WebAPI 健康检查报告
echo ========================
echo 检查时间: !date! !time!
echo API地址: !API_URL!
echo 部署路径: !DEPLOY_PATH!
echo.
echo 检查结果:
echo - 部署目录: 正常
echo - 进程状态: 检查完成
echo - 端口监听: 检查完成  
echo - API健康: 检查完成
echo - 数据库连接: 检查完成
echo - 认证接口: 检查完成
echo.
) > "!REPORT_FILE!" 2>nul

if exist "!REPORT_FILE!" (
    echo 📋 健康检查报告已保存: !REPORT_FILE!
)

goto :end

:error
echo.
echo ====================================
echo ❌ 服务异常！需要处理
echo ====================================
echo.
echo 🔧 建议操作：
echo 1. 检查服务进程是否启动
echo 2. 验证端口是否被占用
echo 3. 查看应用程序日志
echo 4. 重启WebAPI服务
echo.
echo 快速重启命令：
echo net stop LYBTWebAPI
echo net start LYBTWebAPI
echo.

:end
pause