<#
.SYNOPSIS
    LYBT 数据库备份脚本

.DESCRIPTION
    自动化数据库备份脚本，支持：
    - 完整备份（Full Backup）
    - 差异备份（Differential Backup）
    - 事务日志备份（Transaction Log Backup）
    - 备份压缩和验证
    - 自动清理旧备份
    - 详细日志记录

.PARAMETER BackupType
    备份类型：Full（默认）、Differential、Log

.PARAMETER BackupPath
    备份文件存储路径（默认：D:\backups\LYBT\Database）

.PARAMETER DatabaseName
    数据库名称（默认：LYBTDB）

.PARAMETER RetentionDays
    备份保留天数（默认：7天）

.PARAMETER Compress
    是否启用备份压缩（默认：True）

.PARAMETER Verify
    是否验证备份完整性（默认：True）

.EXAMPLE
    .\backup-database.ps1
    执行完整备份，使用默认设置

.EXAMPLE
    .\backup-database.ps1 -BackupType Differential -RetentionDays 14
    执行差异备份，保留14天

.EXAMPLE
    .\backup-database.ps1 -BackupType Log -Verify:$false
    执行事务日志备份，跳过验证

.NOTES
    文件名: backup-database.ps1
    作者: LYBT开发团队
    版本: 1.0.0
    创建日期: 2025-10-30
    参考文档: docs/how-to-guides/server/webapi-deployment.md
#>

param(
    [ValidateSet("Full", "Differential", "Log")]
    [string]$BackupType = "Full",

    [string]$BackupPath = "D:\backups\LYBT\Database",

    [string]$DatabaseName = "LYBTDB",

    [int]$RetentionDays = 7,

    [bool]$Compress = $true,

    [bool]$Verify = $true
)

# ============================================
# 配置变量
# ============================================
$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $PSScriptRoot
$LogFile = Join-Path $PSScriptRoot "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

# ============================================
# 日志函数
# ============================================
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"

    # 控制台输出
    switch ($Level) {
        "Info"    { Write-Host $Message -ForegroundColor White }
        "Success" { Write-Host "✅ $Message" -ForegroundColor Green }
        "Warning" { Write-Host "⚠️ $Message" -ForegroundColor Yellow }
        "Error"   { Write-Host "❌ $Message" -ForegroundColor Red }
    }

    # 文件输出
    Add-Content -Path $LogFile -Value $logMessage
}

function Write-Section {
    param([string]$Title)
    Write-Host "`n==============================================" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "==============================================" -ForegroundColor Cyan
}

# ============================================
# 获取连接字符串
# ============================================
function Get-ConnectionString {
    Write-Section "获取数据库连接信息"

    $connectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Log "数据库连接字符串未配置（环境变量：ConnectionStrings__DefaultConnection）" -Level Error
        Write-Log "请运行配置验证脚本：.\validate-production-config.ps1" -Level Error
        exit 1
    }

    Write-Log "连接字符串获取成功" -Level Success
    return $connectionString
}

# ============================================
# 解析服务器和数据库信息
# ============================================
function Parse-ConnectionString {
    param([string]$ConnectionString)

    try {
        # 解析Server
        if ($ConnectionString -match "Server=([^;]+)") {
            $server = $Matches[1]
        } else {
            $server = "localhost"
        }

        # 解析Database（如果指定了DatabaseName参数，优先使用参数）
        if ($ConnectionString -match "Database=([^;]+)") {
            $database = $Matches[1]
        } else {
            $database = $DatabaseName
        }

        Write-Log "数据库服务器: $server" -Level Info
        Write-Log "数据库名称: $database" -Level Info

        return @{
            Server = $server
            Database = $database
        }
    }
    catch {
        Write-Log "连接字符串解析失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 测试SQL Server连接
# ============================================
function Test-SqlConnection {
    param(
        [string]$Server,
        [string]$ConnectionString
    )

    Write-Section "测试数据库连接"

    try {
        # 使用sqlcmd测试连接
        $testQuery = "SELECT @@VERSION"
        $result = & sqlcmd -S $Server -Q $testQuery -W -b 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Log "数据库连接失败" -Level Error
            Write-Log "错误信息: $result" -Level Error
            exit 1
        }

        Write-Log "数据库连接测试成功" -Level Success
    }
    catch {
        Write-Log "数据库连接测试失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 准备备份目录
# ============================================
function Initialize-BackupDirectory {
    Write-Section "准备备份目录"

    if (-not (Test-Path $BackupPath)) {
        Write-Log "创建备份目录: $BackupPath" -Level Info
        New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    }

    # 测试写权限
    $testFile = Join-Path $BackupPath "test-$(Get-Date -Format 'yyyyMMddHHmmss').tmp"
    try {
        New-Item -ItemType File -Path $testFile -Force | Out-Null
        Remove-Item -Path $testFile -Force
        Write-Log "备份目录权限检查通过" -Level Success
    }
    catch {
        Write-Log "备份目录没有写权限: $BackupPath" -Level Error
        exit 1
    }

    Write-Log "备份目录: $BackupPath" -Level Info
}

# ============================================
# 生成备份文件名
# ============================================
function Get-BackupFileName {
    param(
        [string]$Database,
        [string]$BackupType
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

    switch ($BackupType) {
        "Full"         { $suffix = "Full" }
        "Differential" { $suffix = "Diff" }
        "Log"          { $suffix = "Log" }
    }

    return "${Database}_${suffix}_${timestamp}.bak"
}

# ============================================
# 执行数据库备份
# ============================================
function Invoke-DatabaseBackup {
    param(
        [string]$Server,
        [string]$Database,
        [string]$BackupType,
        [string]$BackupFilePath,
        [bool]$Compress
    )

    Write-Section "执行数据库备份"

    Write-Log "备份类型: $BackupType" -Level Info
    Write-Log "目标文件: $BackupFilePath" -Level Info
    Write-Log "压缩: $Compress" -Level Info

    # 构建SQL备份命令
    $backupCommand = switch ($BackupType) {
        "Full" {
            "BACKUP DATABASE [$Database] TO DISK = N'$BackupFilePath'"
        }
        "Differential" {
            "BACKUP DATABASE [$Database] TO DISK = N'$BackupFilePath' WITH DIFFERENTIAL"
        }
        "Log" {
            "BACKUP LOG [$Database] TO DISK = N'$BackupFilePath'"
        }
    }

    # 添加压缩选项
    if ($Compress) {
        $backupCommand += ", COMPRESSION"
    } else {
        $backupCommand += ", NO_COMPRESSION"
    }

    # 添加统计信息
    $backupCommand += ", STATS = 10"

    Write-Log "执行备份..." -Level Info

    try {
        # 执行备份
        $result = & sqlcmd -S $Server -Q $backupCommand -W -b 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Log "备份失败" -Level Error
            Write-Log "错误信息: $result" -Level Error
            exit 1
        }

        # 检查备份文件是否存在
        if (-not (Test-Path $BackupFilePath)) {
            Write-Log "备份文件未生成: $BackupFilePath" -Level Error
            exit 1
        }

        # 获取备份文件大小
        $fileInfo = Get-Item $BackupFilePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)

        Write-Log "备份成功完成" -Level Success
        Write-Log "备份文件大小: ${fileSizeMB} MB" -Level Info

        return $BackupFilePath
    }
    catch {
        Write-Log "备份执行失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 验证备份文件
# ============================================
function Test-BackupFile {
    param(
        [string]$Server,
        [string]$BackupFilePath
    )

    Write-Section "验证备份文件"

    Write-Log "验证备份完整性..." -Level Info

    $verifyCommand = "RESTORE VERIFYONLY FROM DISK = N'$BackupFilePath'"

    try {
        $result = & sqlcmd -S $Server -Q $verifyCommand -W -b 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Log "备份文件验证失败" -Level Error
            Write-Log "错误信息: $result" -Level Error
            exit 1
        }

        Write-Log "备份文件验证成功，备份文件完整有效" -Level Success
    }
    catch {
        Write-Log "备份验证失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 清理旧备份
# ============================================
function Remove-OldBackups {
    param(
        [string]$BackupPath,
        [int]$RetentionDays
    )

    Write-Section "清理旧备份"

    if ($RetentionDays -le 0) {
        Write-Log "备份保留天数设置为0，跳过清理" -Level Warning
        return
    }

    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    Write-Log "保留策略: 删除 $cutoffDate 之前的备份" -Level Info

    $oldBackups = Get-ChildItem -Path $BackupPath -Filter "*.bak" |
                  Where-Object { $_.LastWriteTime -lt $cutoffDate }

    if ($oldBackups.Count -eq 0) {
        Write-Log "没有需要清理的旧备份" -Level Info
        return
    }

    Write-Log "发现 $($oldBackups.Count) 个旧备份需要清理" -Level Info

    foreach ($backup in $oldBackups) {
        try {
            Write-Log "删除: $($backup.Name) ($(Get-Date $backup.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss'))" -Level Info
            Remove-Item -Path $backup.FullName -Force
        }
        catch {
            Write-Log "删除备份失败: $($backup.Name)" -Level Warning
        }
    }

    Write-Log "旧备份清理完成" -Level Success
}

# ============================================
# 主流程
# ============================================
function Main {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║        LYBT 数据库备份脚本 v1.0.0                        ║" -ForegroundColor White
    Write-Host "║          凌隐宝堂中医诊所诊疗系统                         ║" -ForegroundColor White
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    Write-Log "备份开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Level Info
    Write-Log "备份类型: $BackupType" -Level Info
    Write-Log "备份路径: $BackupPath" -Level Info
    Write-Log "保留天数: $RetentionDays" -Level Info
    Write-Log "日志文件: $LogFile" -Level Info

    try {
        # Step 1: 获取连接字符串
        $connectionString = Get-ConnectionString

        # Step 2: 解析连接信息
        $dbInfo = Parse-ConnectionString -ConnectionString $connectionString

        # Step 3: 测试数据库连接
        Test-SqlConnection -Server $dbInfo.Server -ConnectionString $connectionString

        # Step 4: 准备备份目录
        Initialize-BackupDirectory

        # Step 5: 生成备份文件名
        $backupFileName = Get-BackupFileName -Database $dbInfo.Database -BackupType $BackupType
        $backupFilePath = Join-Path $BackupPath $backupFileName

        # Step 6: 执行备份
        $backupFile = Invoke-DatabaseBackup `
            -Server $dbInfo.Server `
            -Database $dbInfo.Database `
            -BackupType $BackupType `
            -BackupFilePath $backupFilePath `
            -Compress $Compress

        # Step 7: 验证备份（可选）
        if ($Verify) {
            Test-BackupFile -Server $dbInfo.Server -BackupFilePath $backupFile
        } else {
            Write-Log "跳过备份验证（-Verify = `$false）" -Level Warning
        }

        # Step 8: 清理旧备份
        Remove-OldBackups -BackupPath $BackupPath -RetentionDays $RetentionDays

        # 完成
        Write-Section "备份完成"
        Write-Log "备份成功完成！" -Level Success
        Write-Log "备份文件: $backupFile" -Level Info
        Write-Log "日志文件: $LogFile" -Level Info

        Write-Host "`n下一步:" -ForegroundColor Cyan
        Write-Host "  1. 验证备份文件: Test-Path `"$backupFile`"" -ForegroundColor White
        Write-Host "  2. 查看备份历史: Get-ChildItem `"$BackupPath`" | Sort-Object LastWriteTime -Descending" -ForegroundColor White
        Write-Host "  3. 手动验证备份: sqlcmd -S $($dbInfo.Server) -Q `"RESTORE VERIFYONLY FROM DISK = N'$backupFile'`"" -ForegroundColor White
        Write-Host ""

    } catch {
        Write-Log "备份失败: $($_.Exception.Message)" -Level Error
        Write-Log "堆栈跟踪: $($_.ScriptStackTrace)" -Level Error
        exit 1
    }
}

# 执行主流程
Main
