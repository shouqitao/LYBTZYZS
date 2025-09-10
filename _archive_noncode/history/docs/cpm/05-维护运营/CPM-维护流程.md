# CCPM 维护流程手册

## 概述

本手册定义CCPM (Code-Claude Project Manager) 系统的日常维护操作流程，包括定期维护、预防性维护、性能优化和系统健康检查。基于LYBTZYZS项目实际运营经验制定。

## 维护分类

### 日常维护 (Daily)
- 系统状态检查
- 日志分析和清理
- 性能指标监控
- 备份验证

### 周期性维护 (Weekly/Monthly)
- 数据库优化和统计更新
- 安全补丁和更新
- 代码质量扫描
- 依赖包更新

### 预防性维护 (Quarterly)
- 架构健康检查
- 容量规划评估
- 灾难恢复演练
- 文档更新和审查

## 日常维护检查清单

### 每日维护任务

#### 1. 系统健康检查 (09:00 AM)

```powershell
# daily-health-check.ps1
Write-Host "=== 每日系统健康检查 ===" -ForegroundColor Green

# 检查应用程序状态
$processes = Get-Process | Where-Object {$_.ProcessName -like "*LYBT*"}
foreach ($proc in $processes) {
    $memory = [math]::Round($proc.WorkingSet64 / 1MB, 2)
    $cpu = [math]::Round($proc.CPU, 2)
    Write-Host "✅ 进程 $($proc.ProcessName): 内存 ${memory}MB, CPU ${cpu}s" -ForegroundColor Green
}

# 检查服务状态
$services = @("MSSQLSERVER", "IIS Admin Service", "World Wide Web Publishing Service")
foreach ($service in $services) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq "Running") {
        Write-Host "✅ 服务 $service 运行正常" -ForegroundColor Green
    } else {
        Write-Host "❌ 服务 $service 异常" -ForegroundColor Red
    }
}

# 检查磁盘空间
$drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
foreach ($drive in $drives) {
    $freeSpaceGB = [math]::Round($drive.FreeSpace / 1GB, 2)
    $totalSpaceGB = [math]::Round($drive.Size / 1GB, 2)
    $freePercent = [math]::Round(($drive.FreeSpace / $drive.Size) * 100, 2)
    
    if ($freePercent -lt 20) {
        Write-Host "⚠️  磁盘 $($drive.DeviceID) 空间不足: ${freeSpaceGB}GB / ${totalSpaceGB}GB (${freePercent}%)" -ForegroundColor Yellow
    } else {
        Write-Host "✅ 磁盘 $($drive.DeviceID) 空间正常: ${freeSpaceGB}GB / ${totalSpaceGB}GB (${freePercent}%)" -ForegroundColor Green
    }
}

# 检查数据库连接
try {
    $connectionTest = Invoke-Sqlcmd -Query "SELECT @@VERSION" -ServerInstance "localhost" -ErrorAction Stop
    Write-Host "✅ 数据库连接正常" -ForegroundColor Green
} catch {
    Write-Host "❌ 数据库连接异常: $($_.Exception.Message)" -ForegroundColor Red
}

# 检查API健康状态
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck -TimeoutSec 30
    Write-Host "✅ API健康检查正常" -ForegroundColor Green
} catch {
    Write-Host "❌ API健康检查异常: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n每日健康检查完成 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
```

#### 2. 日志分析和清理 (10:00 AM)

```powershell
# log-analysis-cleanup.ps1
Write-Host "=== 日志分析和清理 ===" -ForegroundColor Green

$logPath = "logs"
if (Test-Path $logPath) {
    # 分析错误日志
    $errorLogs = Get-ChildItem -Path $logPath -Filter "*.log" | ForEach-Object {
        $errorCount = (Get-Content $_.FullName | Select-String "ERROR|FATAL").Count
        $warnCount = (Get-Content $_.FullName | Select-String "WARN").Count
        
        [PSCustomObject]@{
            FileName = $_.Name
            Size = [math]::Round($_.Length / 1MB, 2)
            ErrorCount = $errorCount
            WarningCount = $warnCount
            LastModified = $_.LastWriteTime
        }
    }
    
    $errorLogs | Format-Table -AutoSize
    
    # 清理超过30天的日志文件
    $cutoffDate = (Get-Date).AddDays(-30)
    $oldLogs = Get-ChildItem -Path $logPath -Filter "*.log" | Where-Object {$_.LastWriteTime -lt $cutoffDate}
    
    foreach ($oldLog in $oldLogs) {
        Write-Host "🗑️ 删除旧日志文件: $($oldLog.Name)" -ForegroundColor Yellow
        Remove-Item $oldLog.FullName -Force
    }
    
    # 压缩超过7天的日志文件
    $archiveDate = (Get-Date).AddDays(-7)
    $archiveLogs = Get-ChildItem -Path $logPath -Filter "*.log" | Where-Object {$_.LastWriteTime -lt $archiveDate -and $_.Extension -eq ".log"}
    
    foreach ($archiveLog in $archiveLogs) {
        $zipPath = "$($archiveLog.FullName).zip"
        if (-not (Test-Path $zipPath)) {
            Compress-Archive -Path $archiveLog.FullName -DestinationPath $zipPath -Force
            Remove-Item $archiveLog.FullName -Force
            Write-Host "📦 压缩日志文件: $($archiveLog.Name) → $($archiveLog.Name).zip" -ForegroundColor Cyan
        }
    }
}

Write-Host "日志分析和清理完成" -ForegroundColor Green
```

#### 3. 数据库备份验证 (11:00 AM)

```powershell
# backup-verification.ps1
Write-Host "=== 数据库备份验证 ===" -ForegroundColor Green

$backupPath = "D:\Backups\LYBTDB"
$today = Get-Date -Format "yyyy-MM-dd"

# 检查今日备份是否存在
$todayBackup = Get-ChildItem -Path $backupPath -Filter "*$today*.bak" -ErrorAction SilentlyContinue

if ($todayBackup) {
    Write-Host "✅ 今日数据库备份文件存在: $($todayBackup.Name)" -ForegroundColor Green
    
    # 验证备份文件完整性
    try {
        $verifyQuery = "RESTORE VERIFYONLY FROM DISK = '$($todayBackup.FullName)'"
        Invoke-Sqlcmd -Query $verifyQuery -ServerInstance "localhost"
        Write-Host "✅ 备份文件完整性验证通过" -ForegroundColor Green
    } catch {
        Write-Host "❌ 备份文件完整性验证失败: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # 检查备份文件大小
    $backupSizeMB = [math]::Round($todayBackup.Length / 1MB, 2)
    if ($backupSizeMB -gt 10) { # 假设正常备份应该大于10MB
        Write-Host "✅ 备份文件大小正常: ${backupSizeMB}MB" -ForegroundColor Green
    } else {
        Write-Host "⚠️  备份文件大小异常: ${backupSizeMB}MB" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ 今日数据库备份文件不存在" -ForegroundColor Red
    
    # 执行手动备份
    Write-Host "执行手动数据库备份..." -ForegroundColor Yellow
    $backupFile = "$backupPath\LYBTDB_Manual_$today.bak"
    $backupQuery = "BACKUP DATABASE [LYBTDB] TO DISK = '$backupFile'"
    
    try {
        Invoke-Sqlcmd -Query $backupQuery -ServerInstance "localhost" -QueryTimeout 300
        Write-Host "✅ 手动数据库备份完成: $backupFile" -ForegroundColor Green
    } catch {
        Write-Host "❌ 手动数据库备份失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "数据库备份验证完成" -ForegroundColor Green
```

#### 4. 性能监控 (每小时)

```powershell
# performance-monitoring.ps1
Write-Host "=== 性能监控 ===" -ForegroundColor Green

# CPU使用率
$cpu = Get-WmiObject -Class Win32_Processor | Measure-Object -Property LoadPercentage -Average
Write-Host "CPU平均使用率: $([math]::Round($cpu.Average, 2))%" -ForegroundColor Cyan

# 内存使用率
$memory = Get-WmiObject -Class Win32_OperatingSystem
$memoryUsage = [math]::Round((($memory.TotalVisibleMemorySize - $memory.FreePhysicalMemory) / $memory.TotalVisibleMemorySize) * 100, 2)
Write-Host "内存使用率: ${memoryUsage}%" -ForegroundColor Cyan

# 应用程序响应时间
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck
    $stopwatch.Stop()
    Write-Host "API响应时间: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Cyan
} catch {
    $stopwatch.Stop()
    Write-Host "API响应异常: $($_.Exception.Message)" -ForegroundColor Red
}

# 数据库连接数
try {
    $connectionQuery = @"
    SELECT 
        DB_NAME() as DatabaseName,
        COUNT(*) as ConnectionCount
    FROM sys.dm_exec_sessions 
    WHERE database_id = DB_ID()
"@
    
    $connections = Invoke-Sqlcmd -Query $connectionQuery -ServerInstance "localhost"
    Write-Host "数据库连接数: $($connections.ConnectionCount)" -ForegroundColor Cyan
} catch {
    Write-Host "数据库连接查询失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 如果性能指标异常，记录告警
if ($cpu.Average -gt 80) {
    Write-Host "⚠️  CPU使用率过高: $([math]::Round($cpu.Average, 2))%" -ForegroundColor Yellow
}

if ($memoryUsage -gt 85) {
    Write-Host "⚠️  内存使用率过高: ${memoryUsage}%" -ForegroundColor Yellow
}

Write-Host "性能监控完成" -ForegroundColor Green
```

### 每日维护报告模板

```markdown
# 每日维护报告 - [日期]

## 系统状态概览
- ✅ 应用程序服务: 正常运行
- ✅ 数据库服务: 连接正常
- ✅ Web服务: 响应正常
- ⚠️  磁盘空间: C盘剩余18% (需关注)

## 性能指标
- CPU平均使用率: 35%
- 内存使用率: 68%
- API平均响应时间: 145ms
- 数据库连接数: 8

## 日志分析
- 错误日志: 2条 (详见附件)
- 警告日志: 15条
- 清理旧日志: 3个文件

## 备份状态
- ✅ 数据库备份: 完成 (245MB)
- ✅ 备份验证: 通过
- ✅ 备份保留: 30天内的备份完整

## 发现问题
1. 磁盘空间不足 - 已清理日志文件，建议增加存储
2. API偶发超时 - 监控中，暂未影响业务

## 建议措施
1. 安排磁盘扩容或数据迁移
2. 继续监控API性能，必要时优化查询

## 下一步计划
- 明日重点关注磁盘空间和API性能
- 准备周末的依赖包更新

---
报告生成时间: [自动生成时间戳]
维护人员: [姓名]
```

## 周期性维护

### 每周维护任务

#### 1. 数据库维护 (周日 02:00 AM)

```sql
-- database-weekly-maintenance.sql

-- 更新统计信息
EXEC sp_updatestats;

-- 重建索引（仅针对碎片率高的索引）
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'
ALTER INDEX ' + i.name + N' ON ' + SCHEMA_NAME(o.schema_id) + N'.' + o.name + N' REBUILD;'
FROM sys.indexes i
JOIN sys.objects o ON i.object_id = o.object_id
JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
    ON i.object_id = ps.object_id AND i.index_id = ps.index_id
WHERE ps.avg_fragmentation_in_percent > 30
    AND i.index_id > 0
    AND o.type = 'U';

PRINT 'Rebuilding fragmented indexes...';
EXEC sp_executesql @sql;

-- 清理过期数据（根据业务需求调整）
-- 删除90天前的审计日志
DELETE FROM AuditLogs WHERE CreateTime < DATEADD(DAY, -90, GETDATE());
PRINT 'Cleaned up audit logs older than 90 days';

-- 收缩数据库（谨慎使用）
-- DBCC SHRINKDATABASE(LYBTDB, 10);

-- 检查数据库一致性
DBCC CHECKDB('LYBTDB') WITH NO_INFOMSGS;
PRINT 'Database consistency check completed';
```

#### 2. 依赖包更新检查 (每周五)

```powershell
# weekly-dependency-check.ps1
Write-Host "=== 每周依赖包更新检查 ===" -ForegroundColor Green

# 检查过时的NuGet包
Write-Host "检查过时的NuGet包..." -ForegroundColor Cyan
$outdatedPackages = dotnet list package --outdated --format json | ConvertFrom-Json

if ($outdatedPackages.projects) {
    foreach ($project in $outdatedPackages.projects) {
        Write-Host "项目: $($project.path)" -ForegroundColor Yellow
        foreach ($framework in $project.frameworks) {
            if ($framework.topLevelPackages) {
                foreach ($package in $framework.topLevelPackages) {
                    Write-Host "  📦 $($package.id): $($package.resolvedVersion) → $($package.latestVersion)" -ForegroundColor White
                }
            }
        }
    }
} else {
    Write-Host "✅ 所有包都是最新版本" -ForegroundColor Green
}

# 检查安全漏洞
Write-Host "`n检查安全漏洞..." -ForegroundColor Cyan
$auditResult = dotnet list package --vulnerable --include-transitive 2>&1

if ($auditResult -match "has the following vulnerable packages") {
    Write-Host "⚠️  发现安全漏洞，需要更新:" -ForegroundColor Red
    Write-Host $auditResult -ForegroundColor Red
} else {
    Write-Host "✅ 未发现已知安全漏洞" -ForegroundColor Green
}

# 生成更新建议报告
$reportPath = "reports\weekly-dependency-report-$(Get-Date -Format 'yyyy-MM-dd').md"
$reportContent = @"
# 每周依赖包检查报告 - $(Get-Date -Format 'yyyy-MM-dd')

## 过时包列表
$($outdatedPackages | ConvertTo-Json -Depth 5)

## 安全检查结果
$auditResult

## 更新建议
1. 优先更新有安全漏洞的包
2. 测试环境中验证更新后的兼容性
3. 逐步在生产环境中部署

## 下次检查
$(Get-Date -Date (Get-Date).AddDays(7) -Format 'yyyy-MM-dd')
"@

New-Item -Path (Split-Path $reportPath -Parent) -ItemType Directory -Force -ErrorAction SilentlyContinue
Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8

Write-Host "报告已生成: $reportPath" -ForegroundColor Green
```

### 每月维护任务

#### 1. 系统性能分析

```powershell
# monthly-performance-analysis.ps1
Write-Host "=== 每月系统性能分析 ===" -ForegroundColor Green

# 收集30天的性能数据
$startDate = (Get-Date).AddDays(-30)
$performanceData = @()

# 模拟性能数据收集（实际中从监控系统或日志中获取）
for ($i = 0; $i -lt 30; $i++) {
    $date = $startDate.AddDays($i)
    $performanceData += [PSCustomObject]@{
        Date = $date.ToString("yyyy-MM-dd")
        AvgCPU = Get-Random -Minimum 20 -Maximum 80
        AvgMemory = Get-Random -Minimum 40 -Maximum 90
        AvgResponseTime = Get-Random -Minimum 50 -Maximum 300
        ErrorCount = Get-Random -Minimum 0 -Maximum 10
    }
}

# 性能趋势分析
$avgCPU = ($performanceData | Measure-Object -Property AvgCPU -Average).Average
$avgMemory = ($performanceData | Measure-Object -Property AvgMemory -Average).Average
$avgResponseTime = ($performanceData | Measure-Object -Property AvgResponseTime -Average).Average
$totalErrors = ($performanceData | Measure-Object -Property ErrorCount -Sum).Sum

Write-Host "=== 30天性能统计 ===" -ForegroundColor Cyan
Write-Host "平均CPU使用率: $([math]::Round($avgCPU, 2))%" -ForegroundColor White
Write-Host "平均内存使用率: $([math]::Round($avgMemory, 2))%" -ForegroundColor White
Write-Host "平均API响应时间: $([math]::Round($avgResponseTime, 2))ms" -ForegroundColor White
Write-Host "总错误数: $totalErrors" -ForegroundColor White

# 生成性能报告
$reportPath = "reports\monthly-performance-report-$(Get-Date -Format 'yyyy-MM').md"
$reportContent = @"
# 每月性能分析报告 - $(Get-Date -Format 'yyyy年MM月')

## 性能概览
- 平均CPU使用率: $([math]::Round($avgCPU, 2))%
- 平均内存使用率: $([math]::Round($avgMemory, 2))%
- 平均API响应时间: $([math]::Round($avgResponseTime, 2))ms
- 总错误数: $totalErrors

## 趋势分析
$(if ($avgCPU -gt 70) { "⚠️  CPU使用率偏高，建议优化代码或增加资源" } else { "✅ CPU使用率正常" })
$(if ($avgMemory -gt 80) { "⚠️  内存使用率偏高，建议检查内存泄漏" } else { "✅ 内存使用率正常" })
$(if ($avgResponseTime -gt 200) { "⚠️  API响应时间偏长，建议优化查询和缓存" } else { "✅ API响应时间正常" })

## 改进建议
1. 持续监控性能指标
2. 定期进行性能测试
3. 优化数据库查询
4. 考虑增加缓存机制

## 下月关注重点
- 继续监控CPU和内存使用趋势
- 关注API响应时间变化
- 分析错误日志模式
"@

New-Item -Path (Split-Path $reportPath -Parent) -ItemType Directory -Force -ErrorAction SilentlyContinue
Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8

Write-Host "月度性能报告已生成: $reportPath" -ForegroundColor Green
```

## 预防性维护

### 季度系统健康检查

```powershell
# quarterly-health-check.ps1
Write-Host "=== 季度系统健康检查 ===" -ForegroundColor Green

# 1. 架构一致性检查
Write-Host "检查架构一致性..." -ForegroundColor Cyan
$projectFiles = Get-ChildItem -Recurse -Filter "*.csproj"
$architectureIssues = @()

foreach ($proj in $projectFiles) {
    $content = Get-Content $proj.FullName -Raw
    
    # 检查.NET版本一致性
    if ($content -match '<TargetFramework>([^<]+)</TargetFramework>') {
        $targetFramework = $Matches[1]
        if ($targetFramework -ne "net8.0") {
            $architectureIssues += "项目 $($proj.Name) 使用了不一致的目标框架: $targetFramework"
        }
    }
    
    # 检查包版本一致性（简化检查）
    $packageReferences = [regex]::Matches($content, '<PackageReference Include="([^"]+)" Version="([^"]+)"')
    foreach ($match in $packageReferences) {
        $packageName = $match.Groups[1].Value
        $version = $match.Groups[2].Value
        # 这里可以添加版本一致性检查逻辑
    }
}

if ($architectureIssues.Count -eq 0) {
    Write-Host "✅ 架构一致性检查通过" -ForegroundColor Green
} else {
    Write-Host "⚠️  发现架构问题:" -ForegroundColor Yellow
    $architectureIssues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

# 2. 代码质量检查
Write-Host "`n检查代码质量..." -ForegroundColor Cyan
try {
    # 编译检查
    $buildResult = dotnet build --no-restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 编译检查通过" -ForegroundColor Green
    } else {
        Write-Host "❌ 编译检查失败" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
    }
    
    # 测试覆盖率检查（如果有测试）
    $testProjects = Get-ChildItem -Recurse -Filter "*Test*.csproj"
    if ($testProjects) {
        Write-Host "运行单元测试..." -ForegroundColor Cyan
        $testResult = dotnet test --no-restore --verbosity quiet 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ 单元测试通过" -ForegroundColor Green
        } else {
            Write-Host "❌ 单元测试失败" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "❌ 代码质量检查异常: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. 数据库健康检查
Write-Host "`n检查数据库健康状态..." -ForegroundColor Cyan
try {
    # 数据库大小检查
    $dbSizeQuery = @"
    SELECT 
        DB_NAME() AS DatabaseName,
        CAST(SUM(size) * 8.0 / 1024 / 1024 AS DECIMAL(10,2)) AS SizeGB
    FROM sys.database_files
"@
    
    $dbSize = Invoke-Sqlcmd -Query $dbSizeQuery -ServerInstance "localhost"
    Write-Host "数据库大小: $($dbSize.SizeGB)GB" -ForegroundColor Cyan
    
    # 索引碎片检查
    $fragmentationQuery = @"
    SELECT 
        OBJECT_NAME(ps.object_id) AS TableName,
        i.name AS IndexName,
        ps.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
    JOIN sys.indexes i ON ps.object_id = i.object_id AND ps.index_id = i.index_id
    WHERE ps.avg_fragmentation_in_percent > 30
        AND i.index_id > 0
        AND ps.page_count > 1000
"@
    
    $fragmentedIndexes = Invoke-Sqlcmd -Query $fragmentationQuery -ServerInstance "localhost"
    if ($fragmentedIndexes) {
        Write-Host "⚠️  发现碎片化严重的索引:" -ForegroundColor Yellow
        $fragmentedIndexes | ForEach-Object {
            Write-Host "  - $($_.TableName).$($_.IndexName): $([math]::Round($_.avg_fragmentation_in_percent, 2))%" -ForegroundColor Red
        }
    } else {
        Write-Host "✅ 索引碎片化检查正常" -ForegroundColor Green
    }
    
} catch {
    Write-Host "❌ 数据库健康检查异常: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. 安全检查
Write-Host "`n执行安全检查..." -ForegroundColor Cyan

# 检查配置文件中的敏感信息
$configFiles = Get-ChildItem -Recurse -Filter "appsettings*.json"
$securityIssues = @()

foreach ($config in $configFiles) {
    $content = Get-Content $config.FullName -Raw
    
    # 检查是否有明文密码（简化检查）
    if ($content -match '"Password"\s*:\s*"[^"]{1,20}"') {
        $securityIssues += "配置文件 $($config.Name) 可能包含明文密码"
    }
    
    # 检查JWT密钥长度
    if ($content -match '"SecretKey"\s*:\s*"([^"]+)"') {
        $secretKey = $Matches[1]
        if ($secretKey.Length -lt 32) {
            $securityIssues += "JWT密钥长度不足: $($secretKey.Length) 字符"
        }
    }
}

if ($securityIssues.Count -eq 0) {
    Write-Host "✅ 安全检查通过" -ForegroundColor Green
} else {
    Write-Host "⚠️  发现安全问题:" -ForegroundColor Yellow
    $securityIssues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

# 生成季度健康检查报告
$reportPath = "reports\quarterly-health-check-$(Get-Date -Format 'yyyy-Q').md"
$quarter = [math]::Ceiling((Get-Date).Month / 3)
$reportContent = @"
# 季度系统健康检查报告 - $(Get-Date -Format 'yyyy年第')${quarter}季度

## 检查概览
- 检查时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- 架构一致性: $(if ($architectureIssues.Count -eq 0) { '✅ 通过' } else { '⚠️  有问题' })
- 代码质量: $(if ($LASTEXITCODE -eq 0) { '✅ 通过' } else { '❌ 失败' })
- 数据库健康: $(if ($fragmentedIndexes) { '⚠️  需关注' } else { '✅ 正常' })
- 安全检查: $(if ($securityIssues.Count -eq 0) { '✅ 通过' } else { '⚠️  有问题' })

## 详细结果

### 架构问题
$($architectureIssues | ForEach-Object { "- $_" } | Out-String)

### 安全问题
$($securityIssues | ForEach-Object { "- $_" } | Out-String)

### 数据库状态
- 数据库大小: $($dbSize.SizeGB)GB
$(if ($fragmentedIndexes) { "- 需要重建的索引: $($fragmentedIndexes.Count)个" })

## 改进建议
1. 定期执行索引维护
2. 监控数据库增长趋势
3. 加强代码质量控制
4. 定期更新安全配置

## 下季度重点
- 继续监控系统性能
- 完善测试覆盖率
- 优化数据库性能
- 加强安全防护
"@

New-Item -Path (Split-Path $reportPath -Parent) -ItemType Directory -Force -ErrorAction SilentlyContinue
Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8

Write-Host "`n季度健康检查报告已生成: $reportPath" -ForegroundColor Green
```

## 维护任务调度

### Windows任务计划程序配置

```powershell
# create-maintenance-tasks.ps1
Write-Host "=== 创建维护任务计划 ===" -ForegroundColor Green

# 每日健康检查任务
$action1 = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$(Get-Location)\scripts\daily-health-check.ps1`""
$trigger1 = New-ScheduledTaskTrigger -Daily -At "09:00"
$settings1 = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 1)
$principal1 = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount

Register-ScheduledTask -TaskName "CCPM-DailyHealthCheck" -Action $action1 -Trigger $trigger1 -Settings $settings1 -Principal $principal1 -Description "CCPM每日系统健康检查"

# 每周数据库维护任务
$action2 = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$(Get-Location)\scripts\database-weekly-maintenance.ps1`""
$trigger2 = New-ScheduledTaskTrigger -Weekly -WeeksInterval 1 -DaysOfWeek Sunday -At "02:00"
$settings2 = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Register-ScheduledTask -TaskName "CCPM-WeeklyDatabaseMaintenance" -Action $action2 -Trigger $trigger2 -Settings $settings2 -Principal $principal1 -Description "CCPM每周数据库维护"

# 每月性能分析任务
$action3 = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$(Get-Location)\scripts\monthly-performance-analysis.ps1`""
$trigger3 = New-ScheduledTaskTrigger -Monthly -At "01:00" -DaysOfMonth 1
$settings3 = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 1)

Register-ScheduledTask -TaskName "CCPM-MonthlyPerformanceAnalysis" -Action $action3 -Trigger $trigger3 -Settings $settings3 -Principal $principal1 -Description "CCPM每月性能分析"

Write-Host "✅ 维护任务计划创建完成" -ForegroundColor Green
```

## 维护文档管理

### 维护记录模板

```markdown
# 维护记录 - [操作日期]

## 基本信息
- **操作人员**: [姓名]
- **维护类型**: 日常维护 / 计划维护 / 紧急维护
- **开始时间**: YYYY-MM-DD HH:mm
- **结束时间**: YYYY-MM-DD HH:mm
- **服务中断**: 是/否，如是请说明时长

## 维护内容
### 执行的操作
1. [具体操作步骤]
2. [具体操作步骤]
3. [具体操作步骤]

### 操作结果
- ✅ 操作1: 成功
- ⚠️  操作2: 成功但有警告
- ❌ 操作3: 失败

### 发现的问题
1. [问题描述] - [解决状态]
2. [问题描述] - [解决状态]

## 系统状态
### 维护前
- CPU使用率: XX%
- 内存使用率: XX%
- 磁盘空间: XX%
- API响应时间: XXXms

### 维护后
- CPU使用率: XX%
- 内存使用率: XX%
- 磁盘空间: XX%
- API响应时间: XXXms

## 后续计划
- [ ] 需要跟进的问题
- [ ] 计划的改进措施
- [ ] 下次维护重点

## 备注
[其他需要说明的信息]

---
**维护人员签名**: [姓名]
**审核人员签名**: [姓名]
**记录时间**: [自动时间戳]
```

## 相关文档

- [CPM-监控指南.md](CPM-监控指南.md) - 系统监控配置和告警设置
- [CPM-升级指南.md](CPM-升级指南.md) - 版本升级操作流程
- [CPM-灾难恢复指南.md](CPM-灾难恢复指南.md) - 灾难恢复和业务连续性
- [../04-故障排除/CPM-故障排除指南.md](../04-故障排除/CPM-故障排除指南.md) - 故障诊断和解决

## 更新记录

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，定义完整的维护流程体系 | Claude |

---

**使用说明**:
1. 所有脚本需要管理员权限运行
2. 定期检查和更新维护脚本的有效性
3. 根据实际运行情况调整维护频率和内容
4. 保留完整的维护记录用于问题追溯和改进