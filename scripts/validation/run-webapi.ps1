# P3 Record-Only Smoke Validation - WebAPI 启动脚本
# 目标：为冒烟验证提供稳定的本地WebAPI服务

param(
    [switch]$Clean = $false,
    [switch]$Rebuild = $false,
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 脚本配置
$PROJECT_ROOT = Split-Path -Path $PSScriptRoot -Parent | Split-Path -Parent
$WEBAPI_PROJECT = Join-Path $PROJECT_ROOT "src\Server\Services\LYBT.WebAPI"
$WEBAPI_CSPROJ = Join-Path $WEBAPI_PROJECT "LYBT.WebAPI.csproj"
$VALIDATION_LOG = Join-Path $PSScriptRoot "webapi-startup.log"

# WebAPI配置
$API_URL = "https://localhost:7001"
$HTTP_URL = "http://localhost:5001"
$HEALTH_ENDPOINT = "$API_URL/api/v1/health"
$SWAGGER_ENDPOINT = "$API_URL/swagger"

Write-Host "=== P3 Record-Only Smoke Validation - WebAPI 启动脚本 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "目标: 为冒烟验证启动本地WebAPI服务" -ForegroundColor Gray
Write-Host ""

# 初始化日志文件
"=== WebAPI 启动日志 - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $VALIDATION_LOG -Encoding UTF8

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Write-Host $logEntry
    $logEntry | Out-File -FilePath $VALIDATION_LOG -Append -Encoding UTF8
}

function Test-WebApiHealth {
    param([string]$Url, [int]$TimeoutSec = 10)
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec $TimeoutSec -ErrorAction Stop
        return $response.success -eq $true
    }
    catch {
        return $false
    }
}

function Stop-ExistingWebApi {
    Write-Log "检查并停止现有WebAPI进程..." "INFO"
    
    $processes = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Log "发现 $($processes.Count) 个现有WebAPI进程，正在停止..." "WARN"
        $processes | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-Log "已停止现有WebAPI进程" "INFO"
    }
    
    # 检查端口占用
    $portCheck = netstat -an | findstr ":7001"
    if ($portCheck) {
        Write-Log "端口7001仍被占用，尝试释放..." "WARN"
        # 查找并结束占用7001端口的进程
        $port7001 = netstat -ano | findstr ":7001" | findstr "LISTENING"
        if ($port7001) {
            $pid = ($port7001 -split '\s+')[-1]
            if ($pid -match '^\d+$') {
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                Write-Log "已结束占用端口7001的进程 PID: $pid" "INFO"
            }
        }
    }
}

try {
    # 验证项目文件存在
    Write-Log "验证WebAPI项目路径..." "INFO"
    if (-not (Test-Path $WEBAPI_CSPROJ)) {
        throw "WebAPI项目文件不存在: $WEBAPI_CSPROJ"
    }
    Write-Log "✅ 项目文件验证通过: $WEBAPI_PROJECT" "INFO"
    
    # 停止现有进程
    Stop-ExistingWebApi
    
    # 清理和重建（可选）
    if ($Clean -or $Rebuild) {
        Write-Log "清理项目输出..." "INFO"
        Set-Location $PROJECT_ROOT
        & dotnet clean LYBT.Server.sln --verbosity quiet
        
        if ($Rebuild) {
            Write-Log "重新构建解决方案..." "INFO"
            & dotnet build LYBT.Server.sln --verbosity quiet --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw "解决方案构建失败"
            }
            Write-Log "✅ 解决方案构建成功" "INFO"
        }
    }
    
    # 启动WebAPI（后台进程）
    Write-Log "启动WebAPI服务..." "INFO"
    Set-Location $WEBAPI_PROJECT
    
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "dotnet"
    $startInfo.Arguments = "run --no-build --verbosity quiet"
    $startInfo.WorkingDirectory = $WEBAPI_PROJECT
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    
    $process = [System.Diagnostics.Process]::Start($startInfo)
    Write-Log "WebAPI进程已启动，PID: $($process.Id)" "INFO"
    
    # 等待服务就绪
    Write-Log "等待WebAPI服务就绪（最多${TimeoutSeconds}秒）..." "INFO"
    $startTime = Get-Date
    $isReady = $false
    
    while (((Get-Date) - $startTime).TotalSeconds -lt $TimeoutSeconds) {
        Start-Sleep -Seconds 2
        
        if (Test-WebApiHealth -Url $HEALTH_ENDPOINT -TimeoutSec 5) {
            $isReady = $true
            break
        }
        
        Write-Host "." -NoNewline
        
        # 检查进程是否还在运行
        if ($process.HasExited) {
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            Write-Log "WebAPI进程意外退出，退出代码: $($process.ExitCode)" "ERROR"
            Write-Log "标准输出: $stdout" "ERROR"
            Write-Log "错误输出: $stderr" "ERROR"
            throw "WebAPI进程启动失败"
        }
    }
    
    Write-Host "" # 换行
    
    if (-not $isReady) {
        Write-Log "WebAPI服务启动超时" "ERROR"
        $process.Kill()
        throw "WebAPI健康检查失败，服务未能在${TimeoutSeconds}秒内就绪"
    }
    
    # 验证服务状态
    Write-Log "验证WebAPI服务状态..." "INFO"
    $healthResponse = Invoke-RestMethod -Uri $HEALTH_ENDPOINT -Method Get -TimeoutSec 10
    
    Write-Log "✅ WebAPI服务启动成功！" "INFO"
    Write-Log "   - HTTPS地址: $API_URL" "INFO"
    Write-Log "   - HTTP地址:  $HTTP_URL" "INFO"
    Write-Log "   - 健康检查: $HEALTH_ENDPOINT" "INFO"
    Write-Log "   - Swagger文档: $SWAGGER_ENDPOINT" "INFO"
    Write-Log "   - 进程ID: $($process.Id)" "INFO"
    Write-Log "   - 状态: $($healthResponse.message)" "INFO"
    
    # 输出验证准备信息
    Write-Host ""
    Write-Host "=== 冒烟验证准备就绪 ===" -ForegroundColor Green
    Write-Host "WebAPI服务地址: $API_URL" -ForegroundColor Yellow
    Write-Host "健康检查地址: $HEALTH_ENDPOINT" -ForegroundColor Yellow
    Write-Host "Swagger文档: $SWAGGER_ENDPOINT" -ForegroundColor Yellow
    Write-Host "进程PID: $($process.Id)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "下一步: 运行 .\smoke.ps1 执行API冒烟测试" -ForegroundColor Cyan
    Write-Host "停止服务: 按 Ctrl+C 或运行 Stop-Process -Id $($process.Id)" -ForegroundColor Gray
    Write-Host ""
    
    # 保持运行，等待手动停止
    Write-Log "WebAPI服务运行中，等待停止信号..." "INFO"
    
    # 注册Ctrl+C处理程序
    [Console]::CancelKeyPress += {
        param($sender, $e)
        $e.Cancel = $true
        Write-Log "收到停止信号，正在关闭WebAPI服务..." "INFO"
        if (-not $process.HasExited) {
            $process.Kill()
        }
        Write-Log "WebAPI服务已停止" "INFO"
        exit 0
    }
    
    # 监控进程状态
    while (-not $process.HasExited) {
        Start-Sleep -Seconds 5
        
        # 定期健康检查
        if (-not (Test-WebApiHealth -Url $HEALTH_ENDPOINT -TimeoutSec 3)) {
            Write-Log "健康检查失败，WebAPI服务可能出现问题" "WARN"
        }
    }
    
    Write-Log "WebAPI进程已退出，退出代码: $($process.ExitCode)" "INFO"
    
} catch {
    Write-Log "脚本执行失败: $($_.Exception.Message)" "ERROR"
    Write-Host ""
    Write-Host "❌ WebAPI启动失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "详细日志: $VALIDATION_LOG" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "WebAPI服务已停止" -ForegroundColor Yellow