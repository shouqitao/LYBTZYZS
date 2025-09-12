# P4 Release - WebAPI停止脚本
# 功能：优雅停止WebAPI服务，清理资源

param(
    [switch]$Force = $false,
    [int]$Timeout = 30,
    [switch]$All = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== P4 Release WebAPI 停止服务 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "模式: $(if($Force) {'强制停止'} else {'优雅停止'})" -ForegroundColor Gray
Write-Host ""

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    
    $color = switch ($Status) {
        "SUCCESS" { "Green" }
        "WARNING" { "Yellow" }
        "ERROR" { "Red" }
        "INFO" { "White" }
        default { "Gray" }
    }
    
    $icon = switch ($Status) {
        "SUCCESS" { "✅" }
        "WARNING" { "⚠️" }
        "ERROR" { "❌" }
        "INFO" { "ℹ️" }
        default { "📋" }
    }
    
    Write-Host "$icon $Message" -ForegroundColor $color
}

function Stop-WebAPIProcesses {
    param([bool]$ForceStop = $false, [int]$TimeoutSeconds = 30)
    
    # 查找WebAPI进程
    $processes = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
    
    if (-not $processes) {
        Write-Status "未发现运行中的WebAPI进程" "INFO"
        return $true
    }
    
    Write-Status "发现 $($processes.Count) 个WebAPI进程" "INFO"
    
    foreach ($process in $processes) {
        Write-Host "  进程ID: $($process.Id), 启动时间: $($process.StartTime), CPU时间: $($process.TotalProcessorTime)" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    if ($ForceStop) {
        # 强制停止
        Write-Status "执行强制停止..." "WARNING"
        try {
            $processes | Stop-Process -Force
            Write-Status "所有WebAPI进程已强制停止" "SUCCESS"
            return $true
        } catch {
            Write-Status "强制停止失败: $($_.Exception.Message)" "ERROR"
            return $false
        }
    } else {
        # 优雅停止
        Write-Status "执行优雅停止..." "INFO"
        
        try {
            # 发送终止信号
            $processes | Stop-Process
            
            # 等待进程结束
            Write-Status "等待进程优雅退出 (超时: $TimeoutSeconds 秒)..." "INFO"
            
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $allStopped = $false
            
            while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds -and -not $allStopped) {
                Start-Sleep -Milliseconds 500
                $remainingProcesses = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
                
                if (-not $remainingProcesses) {
                    $allStopped = $true
                    Write-Status "所有进程已优雅停止" "SUCCESS"
                } else {
                    Write-Host "." -NoNewline -ForegroundColor Yellow
                }
            }
            
            if (-not $allStopped) {
                Write-Host ""
                Write-Status "优雅停止超时，切换到强制停止..." "WARNING"
                
                $remainingProcesses = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
                if ($remainingProcesses) {
                    $remainingProcesses | Stop-Process -Force
                    Write-Status "剩余进程已强制停止" "SUCCESS"
                }
            }
            
            return $true
            
        } catch {
            Write-Status "优雅停止失败: $($_.Exception.Message)" "ERROR"
            
            # 回退到强制停止
            Write-Status "回退到强制停止..." "WARNING"
            try {
                $processes | Stop-Process -Force
                Write-Status "强制停止成功" "SUCCESS"
                return $true
            } catch {
                Write-Status "强制停止也失败: $($_.Exception.Message)" "ERROR"
                return $false
            }
        }
    }
}

function Stop-RelatedProcesses {
    Write-Status "检查相关进程..." "INFO"
    
    # 检查dotnet进程中的WebAPI
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*LYBT.WebAPI*" -or $_.MainWindowTitle -like "*LYBT.WebAPI*"
    }
    
    if ($dotnetProcesses) {
        Write-Status "发现 $($dotnetProcesses.Count) 个相关dotnet进程" "INFO"
        
        foreach ($process in $dotnetProcesses) {
            try {
                Write-Host "  停止dotnet进程: $($process.Id)" -ForegroundColor Gray
                $process | Stop-Process -Force
            } catch {
                Write-Status "停止dotnet进程失败: $($_.Exception.Message)" "WARNING"
            }
        }
        Write-Status "相关dotnet进程已停止" "SUCCESS"
    } else {
        Write-Status "未发现相关dotnet进程" "INFO"
    }
}

function Clear-PortBinding {
    param([int[]]$Ports = @(5001, 7001))
    
    Write-Status "检查端口占用..." "INFO"
    
    foreach ($port in $Ports) {
        try {
            $listener = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
            $endpoints = $listener.GetActiveTcpListeners()
            $portInUse = $endpoints | Where-Object { $_.Port -eq $port }
            
            if ($portInUse) {
                Write-Status "端口 $port 仍被占用" "WARNING"
                
                # 尝试找到占用端口的进程
                $netstat = netstat -ano | findstr ":$port "
                if ($netstat) {
                    Write-Host "端口占用详情:" -ForegroundColor Gray
                    $netstat | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                }
            } else {
                Write-Status "端口 $port 已释放" "SUCCESS"
            }
        } catch {
            Write-Status "检查端口 $port 异常: $($_.Exception.Message)" "WARNING"
        }
    }
}

function Show-ProcessSummary {
    Write-Host ""
    Write-Status "进程状态总结" "INFO"
    
    # 检查WebAPI进程
    $webApiProcesses = Get-Process -Name "LYBT.WebAPI" -ErrorAction SilentlyContinue
    if ($webApiProcesses) {
        Write-Status "⚠️  仍有 $($webApiProcesses.Count) 个WebAPI进程运行" "WARNING"
        foreach ($process in $webApiProcesses) {
            Write-Host "    PID: $($process.Id), 状态: $($process.Responding)" -ForegroundColor Yellow
        }
    } else {
        Write-Status "✅ 所有WebAPI进程已停止" "SUCCESS"
    }
    
    # 检查相关dotnet进程
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -like "*LYBT.WebAPI*"
    }
    
    if ($dotnetProcesses) {
        Write-Status "⚠️  仍有 $($dotnetProcesses.Count) 个相关dotnet进程运行" "WARNING"
    } else {
        Write-Status "✅ 相关dotnet进程已清理" "SUCCESS"
    }
}

# 主执行逻辑
try {
    Write-Status "开始停止WebAPI服务..." "INFO"
    
    # 停止WebAPI进程
    $stopSuccess = Stop-WebAPIProcesses -ForceStop $Force -TimeoutSeconds $Timeout
    
    if (-not $stopSuccess) {
        Write-Status "停止WebAPI进程失败" "ERROR"
        exit 1
    }
    
    # 停止相关进程
    if ($All) {
        Stop-RelatedProcesses
    }
    
    # 等待资源清理
    Write-Status "等待资源清理..." "INFO"
    Start-Sleep -Seconds 2
    
    # 检查端口释放
    Clear-PortBinding
    
    # 显示总结
    Show-ProcessSummary
    
    Write-Host ""
    Write-Status "WebAPI服务停止完成" "SUCCESS"
    
    # 提供重启建议
    Write-Host ""
    Write-Host "🔧 常用命令:" -ForegroundColor Yellow
    Write-Host "  重新启动: .\scripts\run-webapi.ps1" -ForegroundColor Gray
    Write-Host "  健康检查: .\scripts\health-check.ps1" -ForegroundColor Gray
    Write-Host "  查看进程: Get-Process -Name '*LYBT*'" -ForegroundColor Gray
    
} catch {
    Write-Status "停止脚本异常: $($_.Exception.Message)" "ERROR"
    Write-Host "错误详情: $($_.Exception)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "脚本执行完成 - $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan