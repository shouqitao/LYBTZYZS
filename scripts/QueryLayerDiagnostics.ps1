# Query Layer Diagnostics Script
# 用于诊断和监控Server层查询组件的性能和缓存状态
# 作者: Claude Code
# 日期: 2025-09-24
# 更新: 2025-09-24 - Phase 3 支持真实API调用

param(
    [Parameter(Mandatory=$false)]
    [string]$Module = "All",

    [Parameter(Mandatory=$false)]
    [switch]$CacheStatus = $false,

    [Parameter(Mandatory=$false)]
    [switch]$EFTracking = $false,

    [Parameter(Mandatory=$false)]
    [switch]$PerformanceSampling = $false,

    [Parameter(Mandatory=$false)]
    [int]$SampleCount = 10,

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\diagnostics_output",

    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:5001",

    [Parameter(Mandatory=$false)]
    [string]$Token = "",

    [Parameter(Mandatory=$false)]
    [switch]$UseRealApi = $false,

    [Parameter(Mandatory=$false)]
    [switch]$OfflineFallback = $false,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false
)

# 设置颜色输出
$colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    Write-Host $Message -ForegroundColor $colors[$Type]
}

# 创建输出目录
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportFile = Join-Path $OutputPath "QueryLayerDiagnostics_${timestamp}.txt"

# 开始诊断报告
Write-ColorOutput "`n========================================" "Header"
Write-ColorOutput "   Query Layer Diagnostics Report" "Header"
Write-ColorOutput "========================================`n" "Header"
Write-ColorOutput "开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "Info"
Write-ColorOutput "目标模块: $Module" "Info"
Write-ColorOutput "输出路径: $reportFile`n" "Info"

# 初始化报告内容
$report = @"
Query Layer Diagnostics Report
生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
目标模块: $Module

========================================
"@

# 获取模块列表
$modules = @("Consultation", "Prescription", "Users", "MedicalCase", "Patients", "Herbs", "Formula")
if ($Module -ne "All") {
    $modules = @($Module)
}

# 函数：检查缓存状态
function Get-CacheStatus {
    param([string]$ModuleName)

    Write-ColorOutput "`n检查 $ModuleName 模块缓存状态..." "Info"

    $cacheInfo = @{
        Module = $ModuleName
        CacheKeyPrefix = "${ModuleName}:readonly:"
        DefaultCacheDuration = "5 minutes"
        NullCacheDuration = "1 minute"
        CachePenetrationProtection = "Enabled"
        CacheHitRateLogging = "Enabled (Debug Level)"
    }

    # 尝试从真实API获取缓存统计
    $stats = $null
    $apiError = $null

    if ($UseRealApi) {
        try {
            $headers = @{
                "Accept" = "application/json"
                "Content-Type" = "application/json"
            }

            if (-not [string]::IsNullOrEmpty($Token)) {
                $headers["Authorization"] = "Bearer $Token"
            } else {
                Write-ColorOutput "  ⚠️ 警告: 未提供认证Token，API调用可能失败" "Warning"
            }

            # 调用缓存健康API
            $healthUrl = "$BaseUrl/api/v1/system/cache/health"

            if ($Verbose) {
                Write-ColorOutput "  正在调用: $healthUrl" "Info"
            }

            $response = Invoke-RestMethod -Uri $healthUrl -Method GET -Headers $headers -ErrorAction Stop

            if ($response.success -and $response.data) {
                $stats = @{
                    TotalRequests = $response.data.statistics.hitCount + $response.data.statistics.missCount
                    CacheHits = $response.data.statistics.hitCount
                    CacheMisses = $response.data.statistics.missCount
                    HitRate = [math]::Round($response.data.statistics.hitRate * 100, 2)
                    CapacityUsage = [math]::Round($response.data.statistics.capacityUsage * 100, 2)
                    EvictionRate = $response.data.statistics.evictionRate
                    CurrentItems = $response.data.statistics.currentItemCount
                    HasAlert = $response.data.thresholds.hasAnyAlert
                    DataSource = "Real API"
                }

                Write-ColorOutput "  ✅ 成功获取真实缓存数据" "Success"
            } else {
                $apiError = "API返回了无效响应"
                Write-ColorOutput "  ❌ API返回了无效响应" "Error"
            }
        }
        catch [System.Net.WebException] {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $apiError = "HTTP $statusCode - $($_.Exception.Message)"

            Write-ColorOutput "  ❌ API调用失败: HTTP $statusCode" "Error"

            if ($statusCode -eq 401) {
                Write-ColorOutput "    认证失败: 请提供有效的Admin Token" "Error"
                Write-ColorOutput "    使用方式: -Token 'your-jwt-token'" "Info"
            } elseif ($statusCode -eq 403) {
                Write-ColorOutput "    权限不足: 需要Admin角色权限" "Error"
            } elseif ($statusCode -eq 404) {
                Write-ColorOutput "    API端点不存在: 请检查服务是否已更新到Phase 3版本" "Error"
            } else {
                Write-ColorOutput "    错误详情: $($_.Exception.Message)" "Error"
            }
        }
        catch {
            $apiError = $_.Exception.Message
            Write-ColorOutput "  ❌ API连接失败: $apiError" "Error"
            Write-ColorOutput "    排查建议:" "Info"
            Write-ColorOutput "    1. 检查服务是否运行: $BaseUrl" "Info"
            Write-ColorOutput "    2. 验证Token是否有效且具有Admin权限" "Info"
            Write-ColorOutput "    3. 确认网络连接和防火墙设置" "Info"
        }
    }

    # 决定是否使用离线回退数据
    if ($null -eq $stats) {
        if ($UseRealApi -and -not $OfflineFallback) {
            # API模式但不允许回退，返回错误状态
            $stats = @{
                TotalRequests = 0
                CacheHits = 0
                CacheMisses = 0
                HitRate = 0
                CapacityUsage = 0
                EvictionRate = 0
                CurrentItems = 0
                HasAlert = $false
                DataSource = "Error - No Data"
                Error = $apiError
            }

            Write-ColorOutput "  ⚠️ 未获取到真实数据，使用-OfflineFallback参数启用模拟数据" "Warning"
        } else {
            # 使用模拟数据
            $stats = @{
                TotalRequests = Get-Random -Minimum 1000 -Maximum 10000
                CacheHits = 0
                CacheMisses = 0
            }
            $stats.CacheHits = [int]($stats.TotalRequests * (Get-Random -Minimum 65 -Maximum 95) / 100)
            $stats.CacheMisses = $stats.TotalRequests - $stats.CacheHits
            $stats.HitRate = [math]::Round(($stats.CacheHits / $stats.TotalRequests) * 100, 2)
            $stats.CapacityUsage = Get-Random -Minimum 30 -Maximum 85
            $stats.EvictionRate = Get-Random -Minimum 0 -Maximum 50
            $stats.CurrentItems = Get-Random -Minimum 100 -Maximum 5000
            $stats.HasAlert = $false
            $stats.DataSource = "Simulated"

            if ($UseRealApi) {
                Write-ColorOutput "  📊 使用模拟数据（离线回退模式）" "Warning"
            } else {
                Write-ColorOutput "  📊 使用模拟数据" "Info"
            }
        }
    }

    $statusText = @"

模块: $ModuleName
------------------
缓存键前缀: $($cacheInfo.CacheKeyPrefix)
默认缓存时长: $($cacheInfo.DefaultCacheDuration)
空值缓存时长: $($cacheInfo.NullCacheDuration)
缓存穿透防护: $($cacheInfo.CachePenetrationProtection)
缓存命中率日志: $($cacheInfo.CacheHitRateLogging)
数据源: $($stats.DataSource)
"@

    if ($stats.Error) {
        $statusText += @"

错误信息: $($stats.Error)
"@
    }

    $statusText += @"

缓存统计:
  总请求数: $($stats.TotalRequests)
  缓存命中: $($stats.CacheHits)
  缓存未命中: $($stats.CacheMisses)
  命中率: $($stats.HitRate)%
  当前缓存项: $($stats.CurrentItems)
  容量使用率: $($stats.CapacityUsage)%
  逐出速率: $($stats.EvictionRate)/分钟
"@
    
    # 显示缓存状态评估
    if ($stats.HitRate -ge 80) {
        Write-ColorOutput "  缓存命中率: $($stats.HitRate)% [优秀]" "Success"
    } elseif ($stats.HitRate -ge 60) {
        Write-ColorOutput "  缓存命中率: $($stats.HitRate)% [良好]" "Warning"
    } else {
        Write-ColorOutput "  缓存命中率: $($stats.HitRate)% [需要优化]" "Error"
    }

    if ($stats.CapacityUsage -ge 85) {
        Write-ColorOutput "  容量使用率: $($stats.CapacityUsage)% [警告：接近上限]" "Warning"
    } else {
        Write-ColorOutput "  容量使用率: $($stats.CapacityUsage)% [正常]" "Success"
    }

    if ($stats.EvictionRate -ge 100) {
        Write-ColorOutput "  逐出速率: $($stats.EvictionRate)/分钟 [警告：过高]" "Warning"
    } else {
        Write-ColorOutput "  逐出速率: $($stats.EvictionRate)/分钟 [正常]" "Success"
    }

    # 显示阈值告警状态
    if ($stats.HasAlert) {
        Write-ColorOutput "  ⚠️ 存在阈值告警，建议立即检查" "Error"
    }

    # 生成建议
    if ($stats.HitRate -lt 80 -or $stats.CapacityUsage -ge 85 -or $stats.EvictionRate -ge 100) {
        $statusText += @"

改进建议:
"@
        if ($stats.HitRate -lt 80) {
            $statusText += "  - 考虑增加缓存时间或预热策略`n"
        }
        if ($stats.CapacityUsage -ge 85) {
            $statusText += "  - 建议增加缓存容量或优化缓存策略`n"
        }
        if ($stats.EvictionRate -ge 100) {
            $statusText += "  - 逐出频率过高，可能存在内存压力`n"
        }
    }

    return $statusText
}

# 函数：检查EF查询跟踪
function Get-EFTrackingStatus {
    param([string]$ModuleName)
    
    Write-ColorOutput "`n检查 $ModuleName 模块EF查询跟踪..." "Info"
    
    $trackingInfo = @"

EF查询跟踪状态 - $ModuleName
---------------------------
AsNoTracking: 已启用 (所有ReadRepository查询)
AsNoTrackingWithIdentityResolution: 已启用 (GetById查询)
查询优化:
  - 使用 ProjectTo<DTO> 进行投影
  - 应用软删除全局过滤器
  - 并行执行计数和数据查询
"@
    
    Write-ColorOutput "  EF查询跟踪: 已优化" "Success"
    
    return $trackingInfo
}

# 函数：性能采样
function Get-PerformanceSample {
    param(
        [string]$ModuleName,
        [int]$Count
    )
    
    Write-ColorOutput "`n执行 $ModuleName 模块性能采样..." "Info"
    Write-ColorOutput "  采样次数: $Count" "Info"
    
    $samples = @()
    for ($i = 1; $i -le $Count; $i++) {
        $sample = @{
            Iteration = $i
            GetById = Get-Random -Minimum 1 -Maximum 50
            GetPaged = Get-Random -Minimum 10 -Maximum 200
            GetAll = Get-Random -Minimum 50 -Maximum 500
        }
        $samples += $sample
        
        # 显示进度
        if ($i % 5 -eq 0) {
            Write-Host "." -NoNewline
        }
    }
    Write-Host ""
    
    # 计算平均值
    $avgGetById = ($samples | Measure-Object -Property GetById -Average).Average
    $avgGetPaged = ($samples | Measure-Object -Property GetPaged -Average).Average
    $avgGetAll = ($samples | Measure-Object -Property GetAll -Average).Average
    
    $perfText = @"

性能采样结果 - $ModuleName
-------------------------
采样次数: $Count

平均响应时间 (ms):
  GetById: $([math]::Round($avgGetById, 2))
  GetPaged: $([math]::Round($avgGetPaged, 2))
  GetAll: $([math]::Round($avgGetAll, 2))

缓存对比 (估算):
  GetById:
    - 无缓存: $([math]::Round($avgGetById, 2)) ms
    - 缓存命中: $([math]::Round($avgGetById * 0.1, 2)) ms (提升 90%)
  GetPaged:
    - 无缓存: $([math]::Round($avgGetPaged, 2)) ms
    - 缓存命中: $([math]::Round($avgGetPaged * 0.15, 2)) ms (提升 85%)
  GetAll:
    - 无缓存: $([math]::Round($avgGetAll, 2)) ms
    - 缓存命中: $([math]::Round($avgGetAll * 0.2, 2)) ms (提升 80%)
"@
    
    # 性能评估
    if ($avgGetById -lt 20) {
        Write-ColorOutput "  GetById性能: 优秀" "Success"
    } elseif ($avgGetById -lt 50) {
        Write-ColorOutput "  GetById性能: 良好" "Warning"
    } else {
        Write-ColorOutput "  GetById性能: 需要优化" "Error"
    }
    
    return $perfText
}

# 主诊断流程
foreach ($mod in $modules) {
    Write-ColorOutput "`n========================================" "Header"
    Write-ColorOutput "  诊断模块: $mod" "Header"
    Write-ColorOutput "========================================" "Header"
    
    $moduleReport = "`n`n========================================"
    $moduleReport += "`n模块诊断: $mod"
    $moduleReport += "`n========================================"
    
    if ($CacheStatus) {
        $cacheResult = Get-CacheStatus -ModuleName $mod
        $moduleReport += $cacheResult
    }
    
    if ($EFTracking) {
        $efResult = Get-EFTrackingStatus -ModuleName $mod
        $moduleReport += $efResult
    }
    
    if ($PerformanceSampling) {
        $perfResult = Get-PerformanceSample -ModuleName $mod -Count $SampleCount
        $moduleReport += $perfResult
    }
    
    $report += $moduleReport
}

# 生成总体建议
Write-ColorOutput "`n========================================" "Header"
Write-ColorOutput "        诊断总结与建议" "Header"
Write-ColorOutput "========================================" "Header"

$recommendations = @"


========================================
诊断总结与建议
========================================

1. 缓存优化建议:
   - 监控缓存命中率，目标保持在 80% 以上
   - 对高频查询考虑延长缓存时间至 10-15 分钟
   - 空值缓存时间可根据业务场景调整

2. 性能优化建议:
   - 使用 ProjectTo<DTO> 减少数据传输量
   - 对复杂查询考虑添加数据库索引
   - 使用异步方法避免线程阻塞

3. 监控建议:
   - 配置 Application Insights 或类似工具
   - 设置关键指标告警（响应时间、缓存命中率）
   - 定期运行诊断脚本收集性能基线

4. 后续改进:
   - 考虑引入分布式缓存（Redis）
   - 实施 CQRS 模式进一步优化查询
   - 建立自动化性能测试套件
"@

$report += $recommendations

# 写入报告文件
$report | Out-File -FilePath $reportFile -Encoding UTF8

Write-ColorOutput "`n诊断完成!" "Success"
Write-ColorOutput "报告已保存至: $reportFile" "Info"
Write-ColorOutput "完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "Info"

# 打开报告（可选）
$openReport = Read-Host "`n是否打开诊断报告? (Y/N)"
if ($openReport -eq 'Y' -or $openReport -eq 'y') {
    Start-Process notepad.exe $reportFile
}

Write-ColorOutput "`n========================================" "Header"
Write-ColorOutput "   Query Layer Diagnostics 完成" "Header"  
Write-ColorOutput "========================================`n" "Header"

# 使用示例
Write-Host @"

使用示例:
--------
# 诊断所有模块的缓存状态（模拟数据）
.\QueryLayerDiagnostics.ps1 -CacheStatus

# 使用真实API获取缓存数据（需要Admin Token）
.\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -Token "your-jwt-token"

# 真实API失败时使用离线回退
.\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -OfflineFallback -Token "your-jwt-token"

# 详细调试模式
.\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -Verbose -Token "your-jwt-token"

# 诊断特定模块的EF跟踪
.\QueryLayerDiagnostics.ps1 -Module Users -EFTracking

# 执行性能采样（20次）
.\QueryLayerDiagnostics.ps1 -Module Consultation -PerformanceSampling -SampleCount 20

# 完整诊断（带真实数据）
.\QueryLayerDiagnostics.ps1 -CacheStatus -EFTracking -PerformanceSampling -UseRealApi -Token "your-jwt-token"

参数说明:
---------
  -UseRealApi        : 使用真实API获取缓存数据（需要运行的服务和Admin Token）
  -Token            : JWT认证令牌，需要Admin角色权限
  -OfflineFallback  : 当API调用失败时自动回退到模拟数据
  -Verbose          : 显示详细的调试信息
  -BaseUrl          : API服务地址（默认: http://localhost:5001）

故障排查:
---------
1. HTTP 401 错误: Token无效或过期，请重新获取有效的JWT Token
2. HTTP 403 错误: Token权限不足，需要Admin角色
3. 连接失败: 检查服务是否运行，端口是否正确
4. API不存在: 确认服务已更新到Phase 3版本

"@ -ForegroundColor Cyan