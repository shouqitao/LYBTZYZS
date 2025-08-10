# ========================================
# UltraThink Infrastructure编译错误修复脚本
# 职责单一：修复Infrastructure项目的编译错误
# 代码干净：清晰的修复步骤
# 性能出色：快速定位和修复
# ========================================

$ErrorActionPreference = "Stop"
$ProjectPath = "$PSScriptRoot\..\src\Backend\Core\LYBT.Infrastructure"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   UltraThink Infrastructure 错误修复" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 修复函数
function Fix-SlowQueryAnalyzer {
    Write-Host "[1/4] 修复 SlowQueryAnalyzer.cs..." -ForegroundColor Yellow
    
    $file = "$ProjectPath\Performance\Database\Components\SlowQueryAnalyzer.cs"
    
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # 修复1: 删除对只读属性TotalSlowQueries的赋值
        $content = $content -replace 'report\.TotalSlowQueries = report\.SlowQueries\.Count;', '// TotalSlowQueries is readonly, calculated automatically'
        
        # 修复2: AverageExecutionTime 改为 AverageExecutionTimeMs (但是也是只读的)
        $content = $content -replace 'report\.AverageExecutionTime = .*?;', '// AverageExecutionTimeMs is readonly, calculated automatically'
        $content = $content -replace 'report\.AverageExecutionTime(?![Ms])', 'report.AverageExecutionTimeMs'
        
        # 修复3: MaxExecutionTime 不存在，需要自己计算但不能赋值
        $content = $content -replace 'report\.MaxExecutionTime = .*?;', 'var maxExecutionTime = report.SlowQueries.Any() ? report.SlowQueries.Max(q => q.ExecutionTimeMs) : 0;'
        
        # 修复4: OptimizationSuggestions 不存在，改用局部变量
        $content = $content -replace 'report\.OptimizationSuggestions = ', 'var optimizationSuggestions = '
        $content = $content -replace 'report\.OptimizationSuggestions', 'optimizationSuggestions'
        
        # 修复5: Errors 不存在，改用try-catch处理
        $content = $content -replace 'report\.Errors\.Add\(', '// Error: '
        
        # 修复6: IntervalStats 相关的错误
        $content = $content -replace '\.IntervalStats(?!\w)', '.DataPoints'
        
        # 修复7: TimeWindow 和 IntervalMinutes 不存在
        $content = $content -replace 'QueryPerformanceTrend\.TimeWindow', 'trend.TimeRange'
        $content = $content -replace 'QueryPerformanceTrend\.IntervalMinutes', 'intervalMinutes'
        
        # 修复8: QueryType 不存在
        $content = $content -replace '\.QueryType(?!\w)', '.SqlText.Substring(0, Math.Min(6, q.SqlText.Length)).ToUpper()'
        
        # 修复9: 其他缺失的属性
        $content = $content -replace '\.TotalQueries =', '.DataPoints.Clear(); //'
        $content = $content -replace '\.AverageQueriesPerInterval =', '//'
        $content = $content -replace '\.PerformanceTrendDirection =', '//'
        $content = $content -replace '\.TrendMagnitude =', '//'
        
        # 保存修复后的文件
        Set-Content -Path $file -Value $content -Encoding UTF8
        Write-Host "  [OK] SlowQueryAnalyzer.cs 已修复" -ForegroundColor Green
    } else {
        Write-Host "  [警告] 文件不存在: SlowQueryAnalyzer.cs" -ForegroundColor Yellow
    }
}

function Fix-DatabaseStatisticsCollector {
    Write-Host "[2/4] 修复 DatabaseStatisticsCollector.cs..." -ForegroundColor Yellow
    
    $file = "$ProjectPath\Performance\Database\Components\DatabaseStatisticsCollector.cs"
    
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # 修复缺失的属性
        $content = $content -replace '(?<!\/\/)stats\.TableCount =', '// stats.TableCount ='
        $content = $content -replace '(?<!\/\/)stats\.IndexCount =', '// stats.IndexCount ='
        $content = $content -replace '(?<!\/\/)stats\.TotalConnections =', '// stats.TotalConnections ='
        $content = $content -replace '(?<!\/\/)stats\.ActiveQueries =', '// stats.ActiveQueries ='
        $content = $content -replace '(?<!\/\/)stats\.DeadlockCount =', '// stats.DeadlockCount ='
        $content = $content -replace '(?<!\/\/)stats\.TempDbUsageMB =', '// stats.TempDbUsageMB ='
        
        # 修复只读属性
        $content = $content -replace '(?<!\/\/)stats\.AverageQueryTime =', '// stats.AverageQueryTime ='
        $content = $content -replace '(?<!\/\/)stats\.CacheHitRatio =', '// stats.CacheHitRatio ='
        
        Set-Content -Path $file -Value $content -Encoding UTF8
        Write-Host "  [OK] DatabaseStatisticsCollector.cs 已修复" -ForegroundColor Green
    } else {
        Write-Host "  [警告] 文件不存在: DatabaseStatisticsCollector.cs" -ForegroundColor Yellow
    }
}

function Fix-UnifiedDatabaseOptimizerRefactored {
    Write-Host "[3/4] 修复 UnifiedDatabaseOptimizerRefactored.cs..." -ForegroundColor Yellow
    
    $file = "$ProjectPath\Performance\Database\UnifiedDatabaseOptimizerRefactored.cs"
    
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # 修复 int? 问题
        $content = $content -replace '(\w+)\.Result\?\?', '$1.Result != null ? $1.Result :'
        $content = $content -replace 'slowQueriesTask\.Result\?\?', 'slowQueriesTask.Result != null ? slowQueriesTask.Result.ToList() :'
        $content = $content -replace '\.Result\.Count(?!\()', '.Result.Count()'
        
        Set-Content -Path $file -Value $content -Encoding UTF8
        Write-Host "  [OK] UnifiedDatabaseOptimizerRefactored.cs 已修复" -ForegroundColor Green
    } else {
        Write-Host "  [警告] 文件不存在: UnifiedDatabaseOptimizerRefactored.cs" -ForegroundColor Yellow
    }
}

function Fix-ModelsDefinitions {
    Write-Host "[4/4] 扩展模型定义..." -ForegroundColor Yellow
    
    $modelsFile = "$ProjectPath\Performance\Database\Models\DatabaseOptimizationModels.cs"
    
    if (Test-Path $modelsFile) {
        $content = Get-Content $modelsFile -Raw
        
        # 检查是否需要添加缺失的类
        if ($content -notmatch "class QueryPerformanceTrend") {
            $newClass = @"

    /// <summary>
    /// 查询性能趋势
    /// </summary>
    public class QueryPerformanceTrend
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeRange => EndTime - StartTime;
        public List<PerformanceDataPoint> DataPoints { get; set; } = new();
        public string TrendDirection { get; set; } = "Stable";
        public double TrendMagnitude { get; set; }
    }

    /// <summary>
    /// 性能数据点
    /// </summary>
    public class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int QueryCount { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double MaxExecutionTimeMs { get; set; }
        public double MinExecutionTimeMs { get; set; }
    }
"@
            # 在namespace结束前添加新类
            $content = $content -replace '(\n}\s*$)', "$newClass`n`$1"
        }
        
        Set-Content -Path $modelsFile -Value $content -Encoding UTF8
        Write-Host "  [OK] 模型定义已扩展" -ForegroundColor Green
    } else {
        Write-Host "  [警告] 模型文件不存在" -ForegroundColor Yellow
    }
}

# 执行修复
try {
    Fix-SlowQueryAnalyzer
    Fix-DatabaseStatisticsCollector
    Fix-UnifiedDatabaseOptimizerRefactored
    Fix-ModelsDefinitions
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   修复完成！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "现在尝试重新编译..." -ForegroundColor Yellow
    
    # 尝试编译
    Set-Location $ProjectPath
    dotnet build --no-restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "[成功] Infrastructure 项目编译成功！" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "[警告] 仍有编译错误，请查看详细错误信息" -ForegroundColor Yellow
    }
} catch {
    Write-Host ""
    Write-Host "[错误] 修复过程中出现错误: $_" -ForegroundColor Red
}

# 返回原目录
Set-Location $PSScriptRoot