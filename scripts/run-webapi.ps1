# P4 Release - WebAPI一键启动脚本
# 功能：自动启动WebAPI服务，支持自包含和框架依赖两种模式

param(
    [switch]$SelfContained = $true,
    [switch]$FrameworkDependent = $false,
    [string]$Port = "5001",
    [string]$Environment = "Production",
    [switch]$Wait = $false,
    [switch]$Health = $true
)

$ErrorActionPreference = "Stop"

# 脚本配置
$PROJECT_ROOT = Split-Path $PSScriptRoot -Parent
$SELF_CONTAINED_PATH = Join-Path $PROJECT_ROOT "out\webapi-self"
$FRAMEWORK_DEP_PATH = Join-Path $PROJECT_ROOT "out\webapi-fx"

Write-Host "=== P4 Release WebAPI 一键启动 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "环境: $Environment" -ForegroundColor Gray
Write-Host "端口: $Port" -ForegroundColor Gray
Write-Host ""

# 检查端口是否被占用
function Test-Port {
    param([int]$Port)
    
    try {
        $listener = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
        $endpoints = $listener.GetActiveTcpListeners()
        return $endpoints | Where-Object { $_.Port -eq $Port }
    }
    catch {
        return $false
    }
}

# 停止现有进程
function Stop-ExistingWebAPI {
    Write-Host "检查现有WebAPI进程..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "发现 $($processes.Count) 个现有进程，正在停止..." -ForegroundColor Yellow
        $processes | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-Host "✅ 现有进程已停止" -ForegroundColor Green
    } else {
        Write-Host "✅ 无现有进程" -ForegroundColor Green
    }
}

# 健康检查
function Test-WebAPIHealth {
    param([string]$BaseUrl)
    
    Write-Host "执行健康检查..." -ForegroundColor Yellow
    
    $healthEndpoints = @(
        "$BaseUrl/health",
        "$BaseUrl/health/ready",
        "$BaseUrl/api/v1/health",
        "$BaseUrl/api/v1/health/detailed"
    )
    
    $results = @()
    foreach ($endpoint in $healthEndpoints) {
        try {
            $response = Invoke-RestMethod -Uri $endpoint -Method Get -TimeoutSec 10
            $results += @{
                Endpoint = $endpoint
                Status = "✅ 健康"
                Response = $response
            }
            Write-Host "✅ $endpoint - 健康" -ForegroundColor Green
        }
        catch {
            $results += @{
                Endpoint = $endpoint
                Status = "❌ 异常"
                Error = $_.Exception.Message
            }
            Write-Host "❌ $endpoint - 异常: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    return $results
}

# 主执行逻辑
try {
    # 停止现有进程
    Stop-ExistingWebAPI
    
    # 检查端口占用
    if (Test-Port -Port [int]$Port) {
        Write-Warning "端口 $Port 仍被占用，请手动检查或更换端口"
        exit 1
    }
    
    # 确定运行模式和路径
    $useFrameworkDependent = $FrameworkDependent -or (!$SelfContained)
    
    if ($useFrameworkDependent) {
        Write-Host "🚀 启动模式: 框架依赖版本" -ForegroundColor Cyan
        $deployPath = $FRAMEWORK_DEP_PATH
        $executable = "LYBT.WebAPI.exe"
    } else {
        Write-Host "🚀 启动模式: 自包含版本" -ForegroundColor Cyan
        $deployPath = $SELF_CONTAINED_PATH
        $executable = "LYBT.WebAPI.exe"
    }
    
    # 验证部署路径
    if (-not (Test-Path $deployPath)) {
        Write-Error "部署路径不存在: $deployPath"
        Write-Host "请先运行发布命令生成WebAPI产物" -ForegroundColor Red
        exit 1
    }
    
    $executablePath = Join-Path $deployPath $executable
    if (-not (Test-Path $executablePath)) {
        Write-Error "可执行文件不存在: $executablePath"
        exit 1
    }
    
    Write-Host "📂 部署路径: $deployPath" -ForegroundColor Gray
    Write-Host "🔧 可执行文件: $executable" -ForegroundColor Gray
    Write-Host ""
    
    # 设置环境变量
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    
    Write-Host "🌍 环境变量设置:" -ForegroundColor Gray
    Write-Host "  ASPNETCORE_ENVIRONMENT = $Environment" -ForegroundColor Gray
    Write-Host "  ASPNETCORE_URLS = http://localhost:$Port" -ForegroundColor Gray
    Write-Host ""
    
    # 启动WebAPI
    Write-Host "🚀 启动WebAPI服务..." -ForegroundColor Green
    
    $processArgs = @{
        FilePath = $executablePath
        WorkingDirectory = $deployPath
        WindowStyle = "Normal"
        PassThru = $true
    }
    
    $process = Start-Process @processArgs
    
    if ($process) {
        Write-Host "✅ WebAPI进程已启动 (PID: $($process.Id))" -ForegroundColor Green
        Write-Host "🌐 服务地址: http://localhost:$Port" -ForegroundColor Green
        Write-Host "📊 Swagger文档: http://localhost:$Port/swagger" -ForegroundColor Green
        Write-Host ""
        
        # 等待服务启动
        Write-Host "⏳ 等待服务就绪..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
        
        # 执行健康检查
        if ($Health) {
            $healthResults = Test-WebAPIHealth -BaseUrl "http://localhost:$Port"
            
            Write-Host ""
            Write-Host "📊 健康检查结果:" -ForegroundColor Cyan
            foreach ($result in $healthResults) {
                Write-Host "  $($result.Status) $($result.Endpoint)" -ForegroundColor $(if ($result.Status.StartsWith("✅")) { "Green" } else { "Red" })
            }
        }
        
        Write-Host ""
        Write-Host "🎉 WebAPI服务启动完成！" -ForegroundColor Green
        Write-Host ""
        Write-Host "常用命令:" -ForegroundColor Yellow
        Write-Host "  停止服务: Stop-Process -Name 'LYBT.WebAPI' -Force" -ForegroundColor Gray
        Write-Host "  查看进程: Get-Process -Name 'LYBT.WebAPI'" -ForegroundColor Gray
        Write-Host "  测试API: Invoke-RestMethod http://localhost:$Port/health" -ForegroundColor Gray
        
        # 可选等待
        if ($Wait) {
            Write-Host ""
            Write-Host "按任意键退出..." -ForegroundColor Yellow
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
        
    } else {
        Write-Error "❌ WebAPI启动失败"
        exit 1
    }
    
} catch {
    Write-Error "❌ 脚本执行异常: $($_.Exception.Message)"
    Write-Host "错误详情: $($_.Exception)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "脚本执行完成 - $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan