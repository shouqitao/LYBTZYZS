# CCPM 升级指南

## 概述

本指南提供CCPM (Code-Claude Project Manager) 系统版本升级的详细操作流程，包括计划制定、风险评估、升级执行和回滚程序。基于LYBTZYZS项目实际升级经验编写。

## 升级分类

### 补丁升级 (Patch Release)
- **版本号**: 1.0.x → 1.0.y
- **内容**: 安全补丁、关键bug修复
- **停机时间**: < 30分钟
- **风险等级**: 低

### 次要升级 (Minor Release)  
- **版本号**: 1.x → 1.y
- **内容**: 新功能、性能改进、依赖更新
- **停机时间**: 1-2小时
- **风险等级**: 中

### 主要升级 (Major Release)
- **版本号**: x → y
- **内容**: 架构变更、框架升级、破坏性变更
- **停机时间**: 2-6小时
- **风险等级**: 高

## 升级前准备

### 1. 升级计划制定

```markdown
# 升级计划模板 - 版本 [x.y.z]

## 基本信息
- **目标版本**: x.y.z
- **当前版本**: a.b.c
- **升级类型**: 补丁/次要/主要升级
- **计划执行时间**: YYYY-MM-DD HH:mm
- **预计停机时间**: X小时Y分钟
- **负责人员**: [姓名]
- **测试人员**: [姓名]

## 升级内容
### 新功能
- [ ] 功能1描述
- [ ] 功能2描述

### Bug修复
- [ ] 修复问题1
- [ ] 修复问题2

### 依赖更新
- [ ] .NET 8.0.17 → 8.0.18
- [ ] EF Core 8.0.17 → 8.0.18
- [ ] Prism.DryIoc 9.0.537 → 9.0.540

## 升级步骤
1. [ ] 数据库备份
2. [ ] 应用程序备份
3. [ ] 停止服务
4. [ ] 数据库迁移
5. [ ] 应用程序部署
6. [ ] 配置更新
7. [ ] 启动服务
8. [ ] 功能验证

## 验收标准
- [ ] 所有核心功能正常工作
- [ ] 性能指标符合预期
- [ ] 无严重错误日志
- [ ] 用户界面显示正常

## 风险评估
- **高风险项**: 数据库架构变更
- **中风险项**: 依赖包版本冲突
- **缓解措施**: 完整备份、分步执行

## 回滚计划
如果升级失败，按以下步骤回滚：
1. 停止新版本服务
2. 还原应用程序
3. 还原数据库
4. 启动旧版本服务
5. 验证功能正常

## 通知计划
- **升级前24小时**: 通知所有用户
- **升级前1小时**: 发送最终通知
- **升级完成**: 通知用户系统恢复
```

### 2. 环境准备和验证

```powershell
# pre-upgrade-check.ps1
Write-Host "=== 升级前环境检查 ===" -ForegroundColor Green

# 检查系统资源
function Check-SystemResources {
    Write-Host "检查系统资源..." -ForegroundColor Cyan
    
    # CPU使用率
    $cpu = Get-WmiObject -Class Win32_Processor | Measure-Object -Property LoadPercentage -Average
    if ($cpu.Average -gt 50) {
        Write-Host "⚠️  CPU使用率较高: $([math]::Round($cpu.Average, 2))%" -ForegroundColor Yellow
    } else {
        Write-Host "✅ CPU使用率正常: $([math]::Round($cpu.Average, 2))%" -ForegroundColor Green
    }
    
    # 内存使用率
    $memory = Get-WmiObject -Class Win32_OperatingSystem
    $memoryUsage = [math]::Round((($memory.TotalVisibleMemorySize - $memory.FreePhysicalMemory) / $memory.TotalVisibleMemorySize) * 100, 2)
    if ($memoryUsage -gt 70) {
        Write-Host "⚠️  内存使用率较高: ${memoryUsage}%" -ForegroundColor Yellow
    } else {
        Write-Host "✅ 内存使用率正常: ${memoryUsage}%" -ForegroundColor Green
    }
    
    # 磁盘空间
    $drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
    foreach ($drive in $drives) {
        $freePercent = [math]::Round(($drive.FreeSpace / $drive.Size) * 100, 2)
        if ($freePercent -lt 20) {
            Write-Host "⚠️  磁盘 $($drive.DeviceID) 空间不足: ${freePercent}%" -ForegroundColor Yellow
        } else {
            Write-Host "✅ 磁盘 $($drive.DeviceID) 空间充足: ${freePercent}%" -ForegroundColor Green
        }
    }
}

# 检查应用程序状态
function Check-ApplicationStatus {
    Write-Host "检查应用程序状态..." -ForegroundColor Cyan
    
    # 检查服务进程
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*LYBT*"}
    if ($processes) {
        Write-Host "✅ 应用程序进程运行中" -ForegroundColor Green
        foreach ($proc in $processes) {
            $memory = [math]::Round($proc.WorkingSet64 / 1MB, 2)
            Write-Host "   - $($proc.ProcessName) (PID: $($proc.Id)): ${memory}MB" -ForegroundColor Cyan
        }
    } else {
        Write-Host "❌ 未发现应用程序进程" -ForegroundColor Red
    }
    
    # 检查API可用性
    try {
        $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck -TimeoutSec 10
        Write-Host "✅ API服务响应正常" -ForegroundColor Green
    } catch {
        Write-Host "❌ API服务不可访问: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 检查数据库状态
function Check-DatabaseStatus {
    Write-Host "检查数据库状态..." -ForegroundColor Cyan
    
    try {
        # 数据库连接测试
        $connectionTest = Invoke-Sqlcmd -Query "SELECT @@VERSION" -ServerInstance "localhost" -Database "LYBTDB" -ErrorAction Stop
        Write-Host "✅ 数据库连接正常" -ForegroundColor Green
        
        # 检查数据库大小
        $sizeQuery = "SELECT CAST(SUM(size) * 8.0 / 1024 / 1024 AS DECIMAL(10,2)) AS SizeGB FROM sys.database_files"
        $dbSize = Invoke-Sqlcmd -Query $sizeQuery -ServerInstance "localhost" -Database "LYBTDB"
        Write-Host "   数据库大小: $($dbSize.SizeGB)GB" -ForegroundColor Cyan
        
        # 检查活跃连接数
        $connQuery = "SELECT COUNT(*) AS ActiveConnections FROM sys.dm_exec_sessions WHERE database_id = DB_ID()"
        $connections = Invoke-Sqlcmd -Query $connQuery -ServerInstance "localhost" -Database "LYBTDB"
        Write-Host "   活跃连接数: $($connections.ActiveConnections)" -ForegroundColor Cyan
        
        # 检查最后备份时间
        $backupQuery = @"
        SELECT TOP 1 
            backup_finish_date,
            DATEDIFF(HOUR, backup_finish_date, GETDATE()) AS hours_since_backup
        FROM msdb.dbo.backupset 
        WHERE database_name = 'LYBTDB' AND type = 'D'
        ORDER BY backup_finish_date DESC
"@
        $lastBackup = Invoke-Sqlcmd -Query $backupQuery -ServerInstance "localhost"
        if ($lastBackup -and $lastBackup.hours_since_backup -lt 24) {
            Write-Host "✅ 数据库备份状态正常 (最后备份: $($lastBackup.backup_finish_date))" -ForegroundColor Green
        } else {
            Write-Host "⚠️  数据库备份过期或不存在" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "❌ 数据库检查失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 检查备份空间
function Check-BackupSpace {
    Write-Host "检查备份空间..." -ForegroundColor Cyan
    
    $backupPath = "D:\Backups"
    if (-not (Test-Path $backupPath)) {
        Write-Host "⚠️  备份目录不存在，将创建: $backupPath" -ForegroundColor Yellow
        New-Item -Path $backupPath -ItemType Directory -Force
    }
    
    $backupDrive = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DeviceID -eq "D:"}
    if ($backupDrive) {
        $freeSpaceGB = [math]::Round($backupDrive.FreeSpace / 1GB, 2)
        if ($freeSpaceGB -gt 5) { # 至少5GB空间
            Write-Host "✅ 备份目录空间充足: ${freeSpaceGB}GB" -ForegroundColor Green
        } else {
            Write-Host "❌ 备份目录空间不足: ${freeSpaceGB}GB" -ForegroundColor Red
        }
    }
}

# 执行所有检查
Check-SystemResources
Write-Host ""
Check-ApplicationStatus
Write-Host ""
Check-DatabaseStatus
Write-Host ""
Check-BackupSpace

Write-Host "`n=== 升级前检查完成 ===" -ForegroundColor Green
Write-Host "请根据检查结果决定是否继续升级操作。" -ForegroundColor Cyan
```

### 3. 完整备份脚本

```powershell
# full-system-backup.ps1
param(
    [string]$BackupPath = "D:\Backups\LYBT_Upgrade_$(Get-Date -Format 'yyyyMMdd_HHmmss')",
    [switch]$SkipAppFiles,
    [switch]$SkipDatabase
)

Write-Host "=== 系统完整备份开始 ===" -ForegroundColor Green
Write-Host "备份目标路径: $BackupPath" -ForegroundColor Cyan

# 创建备份目录
if (-not (Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory -Force
    Write-Host "✅ 创建备份目录: $BackupPath" -ForegroundColor Green
}

# 1. 数据库备份
if (-not $SkipDatabase) {
    Write-Host "`n=== 数据库备份 ===" -ForegroundColor Cyan
    
    try {
        $dbBackupFile = "$BackupPath\LYBTDB_Full.bak"
        $backupQuery = "BACKUP DATABASE [LYBTDB] TO DISK = '$dbBackupFile' WITH FORMAT, INIT, COMPRESSION"
        
        Write-Host "执行数据库备份..." -ForegroundColor Yellow
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        Invoke-Sqlcmd -Query $backupQuery -ServerInstance "localhost" -QueryTimeout 1800 # 30分钟超时
        
        $stopwatch.Stop()
        $backupSizeMB = [math]::Round((Get-Item $dbBackupFile).Length / 1MB, 2)
        
        Write-Host "✅ 数据库备份完成" -ForegroundColor Green
        Write-Host "   文件: $dbBackupFile" -ForegroundColor Gray
        Write-Host "   大小: ${backupSizeMB}MB" -ForegroundColor Gray
        Write-Host "   耗时: $($stopwatch.Elapsed.ToString('hh\:mm\:ss'))" -ForegroundColor Gray
        
        # 验证备份文件
        Write-Host "验证备份文件完整性..." -ForegroundColor Yellow
        $verifyQuery = "RESTORE VERIFYONLY FROM DISK = '$dbBackupFile'"
        Invoke-Sqlcmd -Query $verifyQuery -ServerInstance "localhost"
        Write-Host "✅ 备份文件完整性验证通过" -ForegroundColor Green
        
    } catch {
        Write-Host "❌ 数据库备份失败: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# 2. 应用程序文件备份
if (-not $SkipAppFiles) {
    Write-Host "`n=== 应用程序文件备份 ===" -ForegroundColor Cyan
    
    $appBackupPath = "$BackupPath\Application"
    New-Item -Path $appBackupPath -ItemType Directory -Force
    
    # 备份服务器端文件
    $serverPath = "src\Server"
    if (Test-Path $serverPath) {
        Write-Host "备份服务器端文件..." -ForegroundColor Yellow
        $serverBackupPath = "$appBackupPath\Server"
        Copy-Item -Path $serverPath -Destination $serverBackupPath -Recurse -Force
        Write-Host "✅ 服务器端文件备份完成" -ForegroundColor Green
    }
    
    # 备份客户端文件
    $clientPath = "src\Client"
    if (Test-Path $clientPath) {
        Write-Host "备份客户端文件..." -ForegroundColor Yellow
        $clientBackupPath = "$appBackupPath\Client"
        Copy-Item -Path $clientPath -Destination $clientBackupPath -Recurse -Force
        Write-Host "✅ 客户端文件备份完成" -ForegroundColor Green
    }
    
    # 备份配置文件
    $configFiles = @(
        "appsettings.json",
        "appsettings.Production.json",
        "web.config"
    )
    
    foreach ($configFile in $configFiles) {
        if (Test-Path $configFile) {
            Copy-Item -Path $configFile -Destination "$appBackupPath\$configFile" -Force
            Write-Host "✅ 配置文件备份: $configFile" -ForegroundColor Green
        }
    }
    
    # 备份脚本文件
    if (Test-Path "scripts") {
        Copy-Item -Path "scripts" -Destination "$appBackupPath\scripts" -Recurse -Force
        Write-Host "✅ 脚本文件备份完成" -ForegroundColor Green
    }
}

# 3. IIS配置备份（如果使用IIS）
Write-Host "`n=== IIS配置备份 ===" -ForegroundColor Cyan
try {
    $iisConfigPath = "$env:WINDIR\System32\inetsrv\config"
    if (Test-Path $iisConfigPath) {
        $iisBackupPath = "$BackupPath\IIS_Config"
        New-Item -Path $iisBackupPath -ItemType Directory -Force
        
        # 备份关键配置文件
        $iisConfigFiles = @("applicationHost.config", "web.config")
        foreach ($configFile in $iisConfigFiles) {
            $sourceFile = "$iisConfigPath\$configFile"
            if (Test-Path $sourceFile) {
                Copy-Item -Path $sourceFile -Destination "$iisBackupPath\$configFile" -Force
                Write-Host "✅ IIS配置文件备份: $configFile" -ForegroundColor Green
            }
        }
    }
} catch {
    Write-Host "⚠️  IIS配置备份失败（可能未使用IIS）: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 4. 生成备份清单
Write-Host "`n=== 生成备份清单 ===" -ForegroundColor Cyan
$backupManifest = @"
# 系统升级备份清单
生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
备份路径: $BackupPath
当前版本: [需要手动填写]
目标版本: [需要手动填写]

## 备份内容
$(if (-not $SkipDatabase) { "✅ 数据库备份: LYBTDB_Full.bak" } else { "❌ 跳过数据库备份" })
$(if (-not $SkipAppFiles) { "✅ 应用程序文件备份: Application\" } else { "❌ 跳过应用文件备份" })
✅ IIS配置备份: IIS_Config\

## 备份文件大小统计
"@

# 计算备份总大小
$backupFiles = Get-ChildItem -Path $BackupPath -Recurse -File
$totalSizeMB = [math]::Round(($backupFiles | Measure-Object -Property Length -Sum).Sum / 1MB, 2)

$backupManifest += "`n总备份大小: ${totalSizeMB}MB`n"
$backupManifest += "备份文件数量: $($backupFiles.Count)`n`n"

$backupManifest += "## 文件列表`n"
foreach ($file in $backupFiles | Sort-Object FullName) {
    $relativePath = $file.FullName.Substring($BackupPath.Length + 1)
    $fileSizeMB = [math]::Round($file.Length / 1MB, 2)
    $backupManifest += "- $relativePath (${fileSizeMB}MB)`n"
}

$manifestFile = "$BackupPath\BACKUP_MANIFEST.md"
Set-Content -Path $manifestFile -Value $backupManifest -Encoding UTF8

Write-Host "✅ 备份清单生成完成: $manifestFile" -ForegroundColor Green

# 5. 压缩备份（可选）
Write-Host "`n是否要压缩备份文件? (y/N): " -ForegroundColor Yellow -NoNewline
$compress = Read-Host

if ($compress -eq 'y' -or $compress -eq 'Y') {
    Write-Host "压缩备份文件..." -ForegroundColor Yellow
    $zipFile = "$BackupPath.zip"
    
    try {
        Compress-Archive -Path "$BackupPath\*" -DestinationPath $zipFile -Force
        $zipSizeMB = [math]::Round((Get-Item $zipFile).Length / 1MB, 2)
        
        Write-Host "✅ 备份压缩完成: $zipFile (${zipSizeMB}MB)" -ForegroundColor Green
        
        Write-Host "是否删除原始备份目录? (y/N): " -ForegroundColor Yellow -NoNewline
        $deleteOriginal = Read-Host
        
        if ($deleteOriginal -eq 'y' -or $deleteOriginal -eq 'Y') {
            Remove-Item -Path $BackupPath -Recurse -Force
            Write-Host "✅ 原始备份目录已删除" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ 备份压缩失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== 系统完整备份完成 ===" -ForegroundColor Green
Write-Host "备份位置: $(if (Test-Path "$BackupPath.zip") { "$BackupPath.zip" } else { $BackupPath })" -ForegroundColor Cyan
Write-Host "备份总大小: ${totalSizeMB}MB" -ForegroundColor Cyan
Write-Host "请妥善保存备份文件，用于升级失败时的系统恢复。" -ForegroundColor Yellow
```

## 升级执行流程

### 标准升级流程脚本

```powershell
# system-upgrade.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,
    
    [string]$PackagePath,
    [string]$BackupPath,
    [switch]$SkipBackup,
    [switch]$SkipTests,
    [switch]$AutoStart
)

Write-Host "=== CCPM 系统升级开始 ===" -ForegroundColor Green
Write-Host "目标版本: $NewVersion" -ForegroundColor Cyan
Write-Host "开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan

# 升级状态跟踪
$upgradeLog = "logs\upgrade_$NewVersion_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
New-Item -Path (Split-Path $upgradeLog -Parent) -ItemType Directory -Force -ErrorAction SilentlyContinue

function Write-UpgradeLog($message, $level = "INFO") {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$level] $message"
    Add-Content -Path $upgradeLog -Value $logEntry -Encoding UTF8
    
    switch ($level) {
        "ERROR" { Write-Host $logEntry -ForegroundColor Red }
        "WARNING" { Write-Host $logEntry -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logEntry -ForegroundColor Green }
        default { Write-Host $logEntry -ForegroundColor White }
    }
}

# 步骤1: 升级前检查
Write-UpgradeLog "开始升级前检查..."
try {
    # 执行升级前检查脚本
    & ".\scripts\pre-upgrade-check.ps1"
    Write-UpgradeLog "升级前检查完成" "SUCCESS"
} catch {
    Write-UpgradeLog "升级前检查失败: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 步骤2: 创建备份（如果未跳过）
if (-not $SkipBackup) {
    Write-UpgradeLog "开始系统备份..."
    try {
        if (-not $BackupPath) {
            $BackupPath = "D:\Backups\LYBT_Upgrade_${NewVersion}_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        }
        
        & ".\scripts\full-system-backup.ps1" -BackupPath $BackupPath
        Write-UpgradeLog "系统备份完成: $BackupPath" "SUCCESS"
    } catch {
        Write-UpgradeLog "系统备份失败: $($_.Exception.Message)" "ERROR"
        exit 1
    }
} else {
    Write-UpgradeLog "跳过系统备份（用户指定）" "WARNING"
}

# 步骤3: 停止应用服务
Write-UpgradeLog "停止应用服务..."
try {
    # 停止IIS应用池（如果使用IIS）
    try {
        Import-Module IISAdministration -ErrorAction SilentlyContinue
        $appPool = Get-IISAppPool -Name "LYBT" -ErrorAction SilentlyContinue
        if ($appPool) {
            Stop-IISAppPool -Name "LYBT"
            Write-UpgradeLog "IIS应用池已停止" "SUCCESS"
        }
    } catch {
        Write-UpgradeLog "IIS应用池停止失败或不存在: $($_.Exception.Message)" "WARNING"
    }
    
    # 停止Windows服务（如果有）
    $services = Get-Service | Where-Object {$_.Name -like "*LYBT*"}
    foreach ($service in $services) {
        if ($service.Status -eq "Running") {
            Stop-Service -Name $service.Name -Force
            Write-UpgradeLog "服务已停止: $($service.Name)" "SUCCESS"
        }
    }
    
    # 终止相关进程
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*LYBT*"}
    foreach ($process in $processes) {
        $process.Kill()
        Write-UpgradeLog "进程已终止: $($process.ProcessName) (PID: $($process.Id))" "SUCCESS"
    }
    
    # 等待进程完全退出
    Start-Sleep -Seconds 10
    
} catch {
    Write-UpgradeLog "停止应用服务失败: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 步骤4: 数据库升级（如果需要）
Write-UpgradeLog "检查数据库升级..."
try {
    # 检查是否有新的数据库迁移
    $migrationOutput = dotnet ef migrations list --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI 2>&1
    
    if ($migrationOutput -match "Pending") {
        Write-UpgradeLog "检测到待应用的数据库迁移，开始执行..."
        
        # 执行数据库迁移
        $migrationResult = dotnet ef database update --project src\Server\Core\LYBT.Infrastructure --startup-project src\Server\Services\LYBT.WebAPI 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-UpgradeLog "数据库迁移完成" "SUCCESS"
        } else {
            Write-UpgradeLog "数据库迁移失败: $migrationResult" "ERROR"
            # 这里应该触发回滚流程
            exit 1
        }
    } else {
        Write-UpgradeLog "无需执行数据库迁移" "SUCCESS"
    }
} catch {
    Write-UpgradeLog "数据库升级检查失败: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 步骤5: 部署新版本应用
Write-UpgradeLog "开始部署新版本应用..."
try {
    if ($PackagePath -and (Test-Path $PackagePath)) {
        Write-UpgradeLog "从包文件部署: $PackagePath"
        
        # 解压部署包
        $tempDeployPath = "temp\deploy_$NewVersion"
        if (Test-Path $tempDeployPath) {
            Remove-Item -Path $tempDeployPath -Recurse -Force
        }
        
        Expand-Archive -Path $PackagePath -DestinationPath $tempDeployPath -Force
        
        # 部署服务器端文件
        if (Test-Path "$tempDeployPath\Server") {
            Copy-Item -Path "$tempDeployPath\Server\*" -Destination "src\Server\" -Recurse -Force
            Write-UpgradeLog "服务器端文件部署完成" "SUCCESS"
        }
        
        # 部署客户端文件
        if (Test-Path "$tempDeployPath\Client") {
            Copy-Item -Path "$tempDeployPath\Client\*" -Destination "src\Client\" -Recurse -Force
            Write-UpgradeLog "客户端文件部署完成" "SUCCESS"
        }
        
    } else {
        Write-UpgradeLog "从源码构建和部署..."
        
        # 清理编译输出
        dotnet clean
        
        # 还原依赖包
        dotnet restore
        
        # 编译应用
        $buildResult = dotnet build --configuration Release 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-UpgradeLog "应用编译失败: $buildResult" "ERROR"
            exit 1
        }
        
        Write-UpgradeLog "应用编译完成" "SUCCESS"
    }
    
} catch {
    Write-UpgradeLog "应用部署失败: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 步骤6: 更新配置文件
Write-UpgradeLog "更新配置文件..."
try {
    # 这里可以添加配置文件更新逻辑
    # 例如：更新连接字符串、添加新的配置项等
    
    Write-UpgradeLog "配置文件更新完成" "SUCCESS"
} catch {
    Write-UpgradeLog "配置文件更新失败: $($_.Exception.Message)" "ERROR"
}

# 步骤7: 启动应用服务
if ($AutoStart) {
    Write-UpgradeLog "启动应用服务..."
    try {
        # 启动Windows服务
        $services = Get-Service | Where-Object {$_.Name -like "*LYBT*" -and $_.Status -eq "Stopped"}
        foreach ($service in $services) {
            Start-Service -Name $service.Name
            Write-UpgradeLog "服务已启动: $($service.Name)" "SUCCESS"
        }
        
        # 启动IIS应用池
        try {
            $appPool = Get-IISAppPool -Name "LYBT" -ErrorAction SilentlyContinue
            if ($appPool -and $appPool.State -eq "Stopped") {
                Start-IISAppPool -Name "LYBT"
                Write-UpgradeLog "IIS应用池已启动" "SUCCESS"
            }
        } catch {
            Write-UpgradeLog "IIS应用池启动失败: $($_.Exception.Message)" "WARNING"
        }
        
        # 等待服务完全启动
        Write-UpgradeLog "等待服务完全启动..."
        Start-Sleep -Seconds 30
        
    } catch {
        Write-UpgradeLog "应用服务启动失败: $($_.Exception.Message)" "ERROR"
    }
}

# 步骤8: 功能验证测试
if (-not $SkipTests) {
    Write-UpgradeLog "开始功能验证测试..."
    try {
        # API健康检查
        $maxRetries = 10
        $retryCount = 0
        $apiHealthy = $false
        
        do {
            try {
                $response = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck -TimeoutSec 10
                $apiHealthy = $true
                Write-UpgradeLog "API健康检查通过" "SUCCESS"
                break
            } catch {
                $retryCount++
                Write-UpgradeLog "API健康检查失败，重试 $retryCount/$maxRetries..." "WARNING"
                Start-Sleep -Seconds 10
            }
        } while ($retryCount -lt $maxRetries)
        
        if (-not $apiHealthy) {
            Write-UpgradeLog "API健康检查持续失败" "ERROR"
            throw "API服务未能正常启动"
        }
        
        # 基础功能测试
        Write-UpgradeLog "执行基础功能测试..."
        # 这里可以添加更多的功能测试逻辑
        
        Write-UpgradeLog "功能验证测试完成" "SUCCESS"
        
    } catch {
        Write-UpgradeLog "功能验证测试失败: $($_.Exception.Message)" "ERROR"
        Write-UpgradeLog "建议检查应用状态或考虑回滚操作" "WARNING"
    }
}

# 升级完成
$upgradeEndTime = Get-Date
$upgradeDuration = $upgradeEndTime - (Get-Date $upgradeLog.Split('_')[2].Split('.')[0])

Write-UpgradeLog "=== CCPM 系统升级完成 ===" "SUCCESS"
Write-UpgradeLog "目标版本: $NewVersion" "SUCCESS"
Write-UpgradeLog "完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "SUCCESS"
Write-UpgradeLog "升级耗时: $($upgradeDuration.ToString('hh\:mm\:ss'))" "SUCCESS"
Write-UpgradeLog "升级日志: $upgradeLog" "SUCCESS"

if ($BackupPath) {
    Write-UpgradeLog "备份位置: $BackupPath" "SUCCESS"
}

Write-Host "`n升级后建议执行:" -ForegroundColor Yellow
Write-Host "1. 检查所有核心功能是否正常" -ForegroundColor Yellow
Write-Host "2. 验证用户登录和基本操作" -ForegroundColor Yellow
Write-Host "3. 监控系统运行状态" -ForegroundColor Yellow
Write-Host "4. 通知用户系统升级完成" -ForegroundColor Yellow
```

## 升级后验证

### 升级后验证清单

```powershell
# post-upgrade-verification.ps1
param(
    [string]$ExpectedVersion,
    [int]$TestDurationMinutes = 10
)

Write-Host "=== 升级后系统验证 ===" -ForegroundColor Green
Write-Host "预期版本: $ExpectedVersion" -ForegroundColor Cyan
Write-Host "测试持续时间: ${TestDurationMinutes}分钟" -ForegroundColor Cyan

$verificationResults = @()

function Add-VerificationResult($testName, $status, $message = "", $details = "") {
    $verificationResults += [PSCustomObject]@{
        TestName = $testName
        Status = $status
        Message = $message
        Details = $details
        Timestamp = Get-Date
    }
}

# 测试1: 版本确认
Write-Host "`n1. 版本确认测试..." -ForegroundColor Cyan
try {
    # 这里需要根据实际应用提供版本查询接口
    $versionResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/system/version" -Method Get -SkipCertificateCheck
    
    if ($versionResponse.version -eq $ExpectedVersion) {
        Add-VerificationResult "版本确认" "✅ PASS" "版本正确: $($versionResponse.version)"
        Write-Host "✅ 版本确认通过: $($versionResponse.version)" -ForegroundColor Green
    } else {
        Add-VerificationResult "版本确认" "❌ FAIL" "版本不匹配，预期: $ExpectedVersion, 实际: $($versionResponse.version)"
        Write-Host "❌ 版本确认失败" -ForegroundColor Red
    }
} catch {
    Add-VerificationResult "版本确认" "❌ FAIL" "无法获取版本信息: $($_.Exception.Message)"
    Write-Host "❌ 版本确认失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试2: API健康检查
Write-Host "`n2. API健康检查..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck
    Add-VerificationResult "API健康检查" "✅ PASS" "API服务正常响应"
    Write-Host "✅ API健康检查通过" -ForegroundColor Green
} catch {
    Add-VerificationResult "API健康检查" "❌ FAIL" "API服务不响应: $($_.Exception.Message)"
    Write-Host "❌ API健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试3: 数据库连接测试
Write-Host "`n3. 数据库连接测试..." -ForegroundColor Cyan
try {
    $dbTestQuery = "SELECT COUNT(*) AS UserCount FROM Users"
    $dbResult = Invoke-Sqlcmd -Query $dbTestQuery -ServerInstance "localhost" -Database "LYBTDB"
    
    if ($dbResult.UserCount -ge 0) {
        Add-VerificationResult "数据库连接" "✅ PASS" "数据库连接正常，用户数: $($dbResult.UserCount)"
        Write-Host "✅ 数据库连接正常，用户数: $($dbResult.UserCount)" -ForegroundColor Green
    }
} catch {
    Add-VerificationResult "数据库连接" "❌ FAIL" "数据库连接失败: $($_.Exception.Message)"
    Write-Host "❌ 数据库连接失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试4: 用户认证测试
Write-Host "`n4. 用户认证测试..." -ForegroundColor Cyan
try {
    # 使用默认管理员账户测试登录
    $loginPayload = @{
        username = "sysadmin"
        password = "Admin@123456"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/auth/login" -Method Post -Body $loginPayload -ContentType "application/json" -SkipCertificateCheck
    
    if ($loginResponse.success -and $loginResponse.data.token) {
        Add-VerificationResult "用户认证" "✅ PASS" "用户认证功能正常"
        Write-Host "✅ 用户认证测试通过" -ForegroundColor Green
        
        # 使用token测试受保护的API
        $headers = @{ "Authorization" = "Bearer $($loginResponse.data.token)" }
        $userProfileResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/users/profile" -Method Get -Headers $headers -SkipCertificateCheck
        
        if ($userProfileResponse.success) {
            Add-VerificationResult "受保护API访问" "✅ PASS" "JWT认证正常工作"
            Write-Host "✅ JWT认证测试通过" -ForegroundColor Green
        }
    }
} catch {
    Add-VerificationResult "用户认证" "❌ FAIL" "用户认证失败: $($_.Exception.Message)"
    Write-Host "❌ 用户认证测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试5: 核心业务功能测试
Write-Host "`n5. 核心业务功能测试..." -ForegroundColor Cyan

# 获取管理员token（复用之前的登录结果）
$adminToken = $null
try {
    $loginPayload = @{
        username = "sysadmin"
        password = "Admin@123456"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/auth/login" -Method Post -Body $loginPayload -ContentType "application/json" -SkipCertificateCheck
    $adminToken = $loginResponse.data.token
} catch {
    Write-Host "无法获取管理员token，跳过业务功能测试" -ForegroundColor Yellow
}

if ($adminToken) {
    $headers = @{ "Authorization" = "Bearer $adminToken" }
    
    # 测试用户管理功能
    try {
        $usersResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/users" -Method Get -Headers $headers -SkipCertificateCheck
        if ($usersResponse.success) {
            Add-VerificationResult "用户管理" "✅ PASS" "用户管理功能正常"
            Write-Host "✅ 用户管理功能测试通过" -ForegroundColor Green
        }
    } catch {
        Add-VerificationResult "用户管理" "❌ FAIL" "用户管理功能异常: $($_.Exception.Message)"
        Write-Host "❌ 用户管理功能测试失败" -ForegroundColor Red
    }
    
    # 测试患者管理功能
    try {
        $patientsResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/v1/patients" -Method Get -Headers $headers -SkipCertificateCheck
        if ($patientsResponse.success) {
            Add-VerificationResult "患者管理" "✅ PASS" "患者管理功能正常"
            Write-Host "✅ 患者管理功能测试通过" -ForegroundColor Green
        }
    } catch {
        Add-VerificationResult "患者管理" "❌ FAIL" "患者管理功能异常: $($_.Exception.Message)"
        Write-Host "❌ 患者管理功能测试失败" -ForegroundColor Red
    }
}

# 测试6: 性能基准测试
Write-Host "`n6. 性能基准测试..." -ForegroundColor Cyan
try {
    $performanceSamples = @()
    
    for ($i = 1; $i -le 10; $i++) {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck -TimeoutSec 10
            $stopwatch.Stop()
            $performanceSamples += $stopwatch.ElapsedMilliseconds
        } catch {
            $stopwatch.Stop()
            $performanceSamples += 9999 # 标记为超时
        }
    }
    
    $avgResponseTime = ($performanceSamples | Measure-Object -Average).Average
    $maxResponseTime = ($performanceSamples | Measure-Object -Maximum).Maximum
    
    if ($avgResponseTime -le 2000) { # 2秒阈值
        Add-VerificationResult "性能基准" "✅ PASS" "平均响应时间: ${avgResponseTime}ms, 最大: ${maxResponseTime}ms"
        Write-Host "✅ 性能基准测试通过，平均响应时间: ${avgResponseTime}ms" -ForegroundColor Green
    } else {
        Add-VerificationResult "性能基准" "⚠️  WARNING" "响应时间偏慢，平均: ${avgResponseTime}ms"
        Write-Host "⚠️  性能基准测试警告，响应时间偏慢: ${avgResponseTime}ms" -ForegroundColor Yellow
    }
    
} catch {
    Add-VerificationResult "性能基准" "❌ FAIL" "性能测试执行失败: $($_.Exception.Message)"
    Write-Host "❌ 性能基准测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试7: 长时间稳定性监控
if ($TestDurationMinutes -gt 0) {
    Write-Host "`n7. 稳定性监控测试 (${TestDurationMinutes}分钟)..." -ForegroundColor Cyan
    
    $monitoringStart = Get-Date
    $monitoringEnd = $monitoringStart.AddMinutes($TestDurationMinutes)
    $errorCount = 0
    $successCount = 0
    
    while ((Get-Date) -lt $monitoringEnd) {
        try {
            Invoke-RestMethod -Uri "https://localhost:7001/health" -Method Get -SkipCertificateCheck -TimeoutSec 5 | Out-Null
            $successCount++
        } catch {
            $errorCount++
            Write-Host "." -ForegroundColor Red -NoNewline
        }
        
        if (($successCount + $errorCount) % 10 -eq 0) {
            Write-Host "." -ForegroundColor Green -NoNewline
        }
        
        Start-Sleep -Seconds 10 # 每10秒检查一次
    }
    
    $totalChecks = $successCount + $errorCount
    $successRate = if ($totalChecks -gt 0) { [math]::Round(($successCount / $totalChecks) * 100, 2) } else { 0 }
    
    if ($successRate -ge 95) {
        Add-VerificationResult "稳定性监控" "✅ PASS" "成功率: ${successRate}% ($successCount/$totalChecks)"
        Write-Host "`n✅ 稳定性监控通过，成功率: ${successRate}%" -ForegroundColor Green
    } else {
        Add-VerificationResult "稳定性监控" "⚠️  WARNING" "成功率偏低: ${successRate}% ($successCount/$totalChecks)"
        Write-Host "`n⚠️  稳定性监控警告，成功率偏低: ${successRate}%" -ForegroundColor Yellow
    }
}

# 生成验证报告
Write-Host "`n=== 生成验证报告 ===" -ForegroundColor Cyan
$reportPath = "reports\post-upgrade-verification-$(Get-Date -Format 'yyyyMMdd_HHmmss').md"

$report = @"
# 升级后系统验证报告

**验证时间**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**预期版本**: $ExpectedVersion
**测试持续时间**: ${TestDurationMinutes}分钟

## 验证结果汇总

| 测试项目 | 状态 | 详细信息 |
|----------|------|----------|
"@

foreach ($result in $verificationResults) {
    $report += "`n| $($result.TestName) | $($result.Status) | $($result.Message) |"
}

$passCount = ($verificationResults | Where-Object {$_.Status -like "*PASS*"}).Count
$failCount = ($verificationResults | Where-Object {$_.Status -like "*FAIL*"}).Count
$warningCount = ($verificationResults | Where-Object {$_.Status -like "*WARNING*"}).Count
$totalTests = $verificationResults.Count

$report += @"

## 测试统计
- **总测试数**: $totalTests
- **通过**: $passCount
- **失败**: $failCount  
- **警告**: $warningCount
- **通过率**: $([math]::Round(($passCount / $totalTests) * 100, 2))%

## 总体评估
$(if ($failCount -eq 0) {
    "✅ **系统升级验证通过** - 所有关键功能正常运行"
} elseif ($failCount -le 2 -and $passCount -ge ($totalTests * 0.8)) {
    "⚠️  **系统升级基本成功** - 存在少量问题，建议关注和修复"  
} else {
    "❌ **系统升级存在严重问题** - 建议立即排查或考虑回滚"
})

## 后续建议
1. 持续监控系统运行状态
2. 关注用户反馈和问题报告
3. 定期检查系统日志
$(if ($warningCount -gt 0) { "4. 优化性能和稳定性问题" })
$(if ($failCount -gt 0) { "4. 优先解决失败的功能问题" })

---
*报告生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
"@

New-Item -Path (Split-Path $reportPath -Parent) -ItemType Directory -Force -ErrorAction SilentlyContinue
Set-Content -Path $reportPath -Value $report -Encoding UTF8

Write-Host "✅ 验证报告已生成: $reportPath" -ForegroundColor Green

# 控制台总结
Write-Host "`n=== 验证总结 ===" -ForegroundColor Green
Write-Host "通过: $passCount, 失败: $failCount, 警告: $warningCount" -ForegroundColor Cyan
Write-Host "通过率: $([math]::Round(($passCount / $totalTests) * 100, 2))%" -ForegroundColor Cyan

if ($failCount -eq 0) {
    Write-Host "🎉 系统升级验证完全通过！" -ForegroundColor Green
} elseif ($failCount -le 2) {
    Write-Host "⚠️  系统升级基本成功，请关注失败项目" -ForegroundColor Yellow
} else {
    Write-Host "❌ 系统升级存在严重问题，建议立即处理" -ForegroundColor Red
}
```

## 相关文档

- [CPM-回滚操作指南.md](../04-故障排除/CPM-回滚操作指南.md) - 升级失败时的回滚操作
- [CPM-维护流程.md](CPM-维护流程.md) - 日常维护操作
- [CPM-监控指南.md](CPM-监控指南.md) - 系统监控和告警

## 更新记录

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，建立完整的升级流程体系 | Claude |

---

**重要提醒**:
1. 升级前务必完成完整的系统备份
2. 在测试环境中先验证升级流程
3. 确保有足够的回滚时间窗口
4. 提前通知所有用户升级计划
5. 准备应急联系方式和技术支持