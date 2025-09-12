# P4 Release - WebAPI健康检查脚本
# 功能：全面检查WebAPI服务健康状态，支持详细报告和监控

param(
    [string]$BaseUrl = "http://localhost:5001",
    [switch]$Detailed = $false,
    [switch]$Continuous = $false,
    [int]$Interval = 30,
    [switch]$Export = $false,
    [string]$OutputPath = "health-report.json"
)

$ErrorActionPreference = "Stop"

Write-Host "=== P4 Release WebAPI 健康检查 ===" -ForegroundColor Cyan
Write-Host "时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "目标: $BaseUrl" -ForegroundColor Gray
Write-Host "模式: $(if($Continuous) {'持续监控'} else {'单次检查'})" -ForegroundColor Gray
Write-Host ""

# 健康检查结果存储
$script:HealthReport = @{
    Timestamp = Get-Date
    BaseUrl = $BaseUrl
    Overall = @{
        Status = "Unknown"
        Score = 0
        Issues = @()
    }
    Endpoints = @{}
    Performance = @{}
    System = @{}
    Recommendations = @()
}

# API端点配置
$HealthEndpoints = @(
    @{ Name = "基础健康"; Url = "$BaseUrl/health"; Critical = $true },
    @{ Name = "就绪检查"; Url = "$BaseUrl/health/ready"; Critical = $true },
    @{ Name = "API健康"; Url = "$BaseUrl/api/v1/health"; Critical = $true },
    @{ Name = "详细健康"; Url = "$BaseUrl/api/v1/health/detailed"; Critical = $false },
    @{ Name = "Swagger文档"; Url = "$BaseUrl/swagger"; Critical = $false },
    @{ Name = "API版本"; Url = "$BaseUrl/api/v1/auth/version"; Critical = $false }
)

# 业务API端点
$BusinessEndpoints = @(
    @{ Name = "认证健康"; Url = "$BaseUrl/api/v1/auth/health"; Method = "GET" },
    @{ Name = "用户健康"; Url = "$BaseUrl/api/v1/users/health"; Method = "GET" },
    @{ Name = "患者健康"; Url = "$BaseUrl/api/v1/patients/health"; Method = "GET" },
    @{ Name = "中药材健康"; Url = "$BaseUrl/api/v1/herbs/health"; Method = "GET" }
)

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

function Test-EndpointHealth {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [bool]$Critical = $false
    )
    
    $result = @{
        Name = $Name
        Url = $Url
        Status = "Unknown"
        ResponseTime = 0
        StatusCode = 0
        Error = $null
        Critical = $Critical
        Timestamp = Get-Date
    }
    
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec 10 -UseBasicParsing
        
        $stopwatch.Stop()
        $result.ResponseTime = $stopwatch.ElapsedMilliseconds
        $result.StatusCode = [int]$response.StatusCode
        
        if ($response.StatusCode -eq 200) {
            $result.Status = "Healthy"
            Write-Status "$Name - 健康 ($($result.ResponseTime)ms)" "SUCCESS"
        } else {
            $result.Status = "Degraded"
            $result.Error = "HTTP $($response.StatusCode)"
            Write-Status "$Name - 降级 (HTTP $($response.StatusCode), $($result.ResponseTime)ms)" "WARNING"
        }
        
    } catch {
        $result.Status = "Unhealthy"
        $result.Error = $_.Exception.Message
        Write-Status "$Name - 不健康: $($_.Exception.Message)" "ERROR"
    }
    
    return $result
}

function Test-SystemResources {
    Write-Host ""
    Write-Status "系统资源检查" "INFO"
    
    $systemInfo = @{}
    
    try {
        # CPU使用率
        $cpu = Get-WmiObject -Class Win32_Processor | Measure-Object -Property LoadPercentage -Average
        $systemInfo.CPU = @{
            Usage = [math]::Round($cpu.Average, 2)
            Status = if ($cpu.Average -lt 80) { "Good" } elseif ($cpu.Average -lt 95) { "Warning" } else { "Critical" }
        }
        Write-Status "CPU使用率: $($systemInfo.CPU.Usage)%" $(if($systemInfo.CPU.Status -eq "Good") {"SUCCESS"} elseif($systemInfo.CPU.Status -eq "Warning") {"WARNING"} else {"ERROR"})
        
        # 内存使用率
        $os = Get-WmiObject -Class Win32_OperatingSystem
        $totalMemory = [math]::Round($os.TotalVisibleMemorySize / 1MB, 2)
        $freeMemory = [math]::Round($os.FreePhysicalMemory / 1MB, 2)
        $usedMemory = $totalMemory - $freeMemory
        $memoryUsage = [math]::Round(($usedMemory / $totalMemory) * 100, 2)
        
        $systemInfo.Memory = @{
            Total = $totalMemory
            Used = $usedMemory
            Free = $freeMemory
            Usage = $memoryUsage
            Status = if ($memoryUsage -lt 80) { "Good" } elseif ($memoryUsage -lt 90) { "Warning" } else { "Critical" }
        }
        Write-Status "内存使用: $($systemInfo.Memory.Usage)% ($($systemInfo.Memory.Used)GB/$($systemInfo.Memory.Total)GB)" $(if($systemInfo.Memory.Status -eq "Good") {"SUCCESS"} elseif($systemInfo.Memory.Status -eq "Warning") {"WARNING"} else {"ERROR"})
        
        # 磁盘空间
        $disk = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 } | Select-Object -First 1
        $diskUsage = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 2)
        $systemInfo.Disk = @{
            Usage = $diskUsage
            FreeSpaceGB = [math]::Round($disk.FreeSpace / 1GB, 2)
            TotalSpaceGB = [math]::Round($disk.Size / 1GB, 2)
            Status = if ($diskUsage -lt 80) { "Good" } elseif ($diskUsage -lt 90) { "Warning" } else { "Critical" }
        }
        Write-Status "磁盘空间: $($systemInfo.Disk.Usage)% (剩余 $($systemInfo.Disk.FreeSpaceGB)GB)" $(if($systemInfo.Disk.Status -eq "Good") {"SUCCESS"} elseif($systemInfo.Disk.Status -eq "Warning") {"WARNING"} else {"ERROR"})
        
    } catch {
        Write-Status "系统资源检查异常: $($_.Exception.Message)" "ERROR"
        $systemInfo.Error = $_.Exception.Message
    }
    
    return $systemInfo
}

function Test-NetworkConnectivity {
    Write-Host ""
    Write-Status "网络连接检查" "INFO"
    
    $networkInfo = @{}
    
    try {
        # 测试基础连通性
        $tcpTest = Test-NetConnection -ComputerName "localhost" -Port 5001 -InformationLevel Quiet
        $networkInfo.LocalConnection = @{
            Status = if ($tcpTest) { "Connected" } else { "Failed" }
            Port = 5001
        }
        Write-Status "本地端口连接 (5001): $(if($tcpTest) {'通畅'} else {'失败'})" $(if($tcpTest) {"SUCCESS"} else {"ERROR"})
        
        # DNS解析测试
        $dnsTest = Resolve-DnsName -Name "localhost" -ErrorAction SilentlyContinue
        $networkInfo.DNSResolution = @{
            Status = if ($dnsTest) { "Success" } else { "Failed" }
        }
        Write-Status "DNS解析 (localhost): $(if($dnsTest) {'成功'} else {'失败'})" $(if($dnsTest) {"SUCCESS"} else {"WARNING"})
        
    } catch {
        Write-Status "网络检查异常: $($_.Exception.Message)" "ERROR"
        $networkInfo.Error = $_.Exception.Message
    }
    
    return $networkInfo
}

function Get-HealthScore {
    param($Results)
    
    $totalEndpoints = $Results.Count
    $healthyEndpoints = ($Results | Where-Object { $_.Status -eq "Healthy" }).Count
    $criticalEndpoints = ($Results | Where-Object { $_.Critical -and $_.Status -ne "Healthy" }).Count
    
    # 基础分数：健康端点比例
    $baseScore = [math]::Round(($healthyEndpoints / $totalEndpoints) * 100)
    
    # 关键端点故障扣分
    $penalty = $criticalEndpoints * 25
    
    # 性能评分
    $avgResponseTime = ($Results | Where-Object { $_.ResponseTime -gt 0 } | Measure-Object -Property ResponseTime -Average).Average
    $performancePenalty = if ($avgResponseTime -gt 2000) { 10 } elseif ($avgResponseTime -gt 1000) { 5 } else { 0 }
    
    $finalScore = [math]::Max(0, $baseScore - $penalty - $performancePenalty)
    
    return @{
        Score = $finalScore
        Grade = if ($finalScore -ge 90) { "A" } elseif ($finalScore -ge 80) { "B" } elseif ($finalScore -ge 70) { "C" } elseif ($finalScore -ge 60) { "D" } else { "F" }
        HealthyEndpoints = $healthyEndpoints
        TotalEndpoints = $totalEndpoints
        CriticalIssues = $criticalEndpoints
        AverageResponseTime = [math]::Round($avgResponseTime, 0)
    }
}

function Show-HealthSummary {
    param($Results, $SystemInfo, $NetworkInfo)
    
    Write-Host ""
    Write-Host "=== 健康检查总结 ===" -ForegroundColor Cyan
    
    # 计算健康评分
    $scoreInfo = Get-HealthScore -Results $Results
    $script:HealthReport.Overall.Score = $scoreInfo.Score
    
    # 总体状态
    $overallStatus = if ($scoreInfo.CriticalIssues -eq 0 -and $scoreInfo.Score -ge 80) { 
        "Healthy" 
    } elseif ($scoreInfo.CriticalIssues -eq 0 -and $scoreInfo.Score -ge 60) { 
        "Degraded" 
    } else { 
        "Unhealthy" 
    }
    
    $script:HealthReport.Overall.Status = $overallStatus
    
    Write-Host ""
    Write-Status "总体状态: $overallStatus" $(if($overallStatus -eq "Healthy") {"SUCCESS"} elseif($overallStatus -eq "Degraded") {"WARNING"} else {"ERROR"})
    Write-Status "健康评分: $($scoreInfo.Score)/100 (等级: $($scoreInfo.Grade))" "INFO"
    Write-Status "端点状态: $($scoreInfo.HealthyEndpoints)/$($scoreInfo.TotalEndpoints) 健康" "INFO"
    Write-Status "平均响应: $($scoreInfo.AverageResponseTime)ms" "INFO"
    
    if ($scoreInfo.CriticalIssues -gt 0) {
        Write-Status "关键问题: $($scoreInfo.CriticalIssues) 个" "ERROR"
        $script:HealthReport.Overall.Issues += "关键端点故障: $($scoreInfo.CriticalIssues) 个"
    }
    
    # 系统资源状态
    if ($SystemInfo.CPU -and $SystemInfo.CPU.Status -ne "Good") {
        $script:HealthReport.Overall.Issues += "CPU使用率过高: $($SystemInfo.CPU.Usage)%"
    }
    if ($SystemInfo.Memory -and $SystemInfo.Memory.Status -ne "Good") {
        $script:HealthReport.Overall.Issues += "内存使用率过高: $($SystemInfo.Memory.Usage)%"
    }
    if ($SystemInfo.Disk -and $SystemInfo.Disk.Status -ne "Good") {
        $script:HealthReport.Overall.Issues += "磁盘空间不足: $($SystemInfo.Disk.Usage)%"
    }
    
    # 建议
    Write-Host ""
    Write-Host "🔧 改进建议:" -ForegroundColor Yellow
    
    if ($scoreInfo.Score -lt 80) {
        Write-Host "  • 检查并修复失败的健康检查端点" -ForegroundColor Gray
        $script:HealthReport.Recommendations += "修复失败的健康检查端点"
    }
    
    if ($scoreInfo.AverageResponseTime -gt 1000) {
        Write-Host "  • 优化API响应性能，目标<1000ms" -ForegroundColor Gray
        $script:HealthReport.Recommendations += "优化API响应性能"
    }
    
    if ($SystemInfo.CPU -and $SystemInfo.CPU.Usage -gt 70) {
        Write-Host "  • 监控CPU使用率，考虑性能优化" -ForegroundColor Gray
        $script:HealthReport.Recommendations += "监控和优化CPU使用率"
    }
    
    if ($SystemInfo.Memory -and $SystemInfo.Memory.Usage -gt 70) {
        Write-Host "  • 检查内存使用情况，防止内存泄漏" -ForegroundColor Gray
        $script:HealthReport.Recommendations += "检查内存使用和可能的内存泄漏"
    }
}

function Export-HealthReport {
    param([string]$OutputPath)
    
    try {
        $script:HealthReport | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding UTF8
        Write-Status "健康报告已导出: $OutputPath" "SUCCESS"
    } catch {
        Write-Status "导出健康报告失败: $($_.Exception.Message)" "ERROR"
    }
}

# 主执行逻辑
function Invoke-HealthCheck {
    Write-Host "🔍 开始健康检查..." -ForegroundColor Green
    Write-Host ""
    
    # API端点健康检查
    Write-Status "API端点检查" "INFO"
    $endpointResults = @()
    
    foreach ($endpoint in $HealthEndpoints) {
        $result = Test-EndpointHealth -Name $endpoint.Name -Url $endpoint.Url -Critical $endpoint.Critical
        $endpointResults += $result
        $script:HealthReport.Endpoints[$endpoint.Name] = $result
    }
    
    # 详细模式：检查业务API
    if ($Detailed) {
        Write-Host ""
        Write-Status "业务API检查" "INFO"
        
        foreach ($endpoint in $BusinessEndpoints) {
            $result = Test-EndpointHealth -Name $endpoint.Name -Url $endpoint.Url -Method $endpoint.Method -Critical $false
            $endpointResults += $result
            $script:HealthReport.Endpoints[$endpoint.Name] = $result
        }
    }
    
    # 系统资源检查
    $systemInfo = Test-SystemResources
    $script:HealthReport.System = $systemInfo
    
    # 网络连接检查
    $networkInfo = Test-NetworkConnectivity
    $script:HealthReport.Performance = $networkInfo
    
    # 显示总结
    Show-HealthSummary -Results $endpointResults -SystemInfo $systemInfo -NetworkInfo $networkInfo
    
    # 导出报告
    if ($Export) {
        Export-HealthReport -OutputPath $OutputPath
    }
    
    return $script:HealthReport.Overall.Status
}

# 主执行流程
try {
    if ($Continuous) {
        Write-Host "🔄 启动持续监控模式 (间隔: $Interval 秒)" -ForegroundColor Yellow
        Write-Host "按 Ctrl+C 停止监控" -ForegroundColor Gray
        Write-Host ""
        
        while ($true) {
            $status = Invoke-HealthCheck
            Write-Host ""
            Write-Host "下次检查: $(Get-Date -Date (Get-Date).AddSeconds($Interval) -Format 'HH:mm:ss')" -ForegroundColor Gray
            Write-Host "=" * 60 -ForegroundColor DarkGray
            Start-Sleep -Seconds $Interval
        }
    } else {
        $status = Invoke-HealthCheck
        
        # 根据健康状态设置退出码
        $exitCode = switch ($status) {
            "Healthy" { 0 }
            "Degraded" { 1 }
            "Unhealthy" { 2 }
            default { 3 }
        }
        
        Write-Host ""
        Write-Host "健康检查完成 - $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
        exit $exitCode
    }
    
} catch {
    Write-Status "健康检查异常: $($_.Exception.Message)" "ERROR"
    Write-Host "错误详情: $($_.Exception)" -ForegroundColor Red
    exit 1
}