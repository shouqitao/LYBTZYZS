<#
.SYNOPSIS
    LYBT 部署回滚脚本

.DESCRIPTION
    自动化部署回滚脚本，支持：
    - Windows Service回滚
    - 应用文件回滚
    - 数据库回滚（可选）
    - 回滚验证
    - 详细日志记录

.PARAMETER TargetPath
    当前部署路径（默认：D:\deploy\LYBT\WebAPI）

.PARAMETER BackupTimestamp
    备份时间戳（格式：yyyyMMdd-HHmmss），如：20251030-143000

.PARAMETER ServiceName
    Windows Service名称（默认：LYBTWebAPI）

.PARAMETER RestoreDatabase
    是否恢复数据库（默认：False）

.PARAMETER DatabaseBackupFile
    数据库备份文件路径（如果RestoreDatabase为True，此参数必需）

.PARAMETER SkipServiceRestart
    跳过服务重启步骤

.EXAMPLE
    .\rollback-deployment.ps1 -BackupTimestamp "20251030-143000"
    回滚到指定时间戳的备份版本

.EXAMPLE
    .\rollback-deployment.ps1 -BackupTimestamp "20251030-143000" -RestoreDatabase -DatabaseBackupFile "D:\backups\LYBT\Database\LYBTDB_Full_20251030-143000.bak"
    回滚应用和数据库

.NOTES
    文件名: rollback-deployment.ps1
    作者: LYBT开发团队
    版本: 1.0.0
    创建日期: 2025-10-30
    参考文档: docs/how-to-guides/server/webapi-deployment.md
#>

param(
    [string]$TargetPath = "D:\deploy\LYBT\WebAPI",

    [Parameter(Mandatory=$true)]
    [ValidatePattern("^\d{8}-\d{6}$")]
    [string]$BackupTimestamp,

    [string]$ServiceName = "LYBTWebAPI",

    [switch]$RestoreDatabase,

    [string]$DatabaseBackupFile = "",

    [switch]$SkipServiceRestart
)

# ============================================
# 配置变量
# ============================================
$ErrorActionPreference = "Stop"
$LogFile = Join-Path $PSScriptRoot "rollback-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

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
# 环境检查
# ============================================
function Test-Prerequisites {
    Write-Section "Step 1: 环境检查"

    # 检查管理员权限
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Log "需要管理员权限来操作Windows Service" -Level Error
        exit 1
    }
    Write-Log "管理员权限检查通过" -Level Success

    # 检查目标路径
    if (-not (Test-Path $TargetPath)) {
        Write-Log "目标路径不存在: $TargetPath" -Level Error
        exit 1
    }
    Write-Log "目标路径检查通过" -Level Success
}

# ============================================
# 查找备份
# ============================================
function Get-BackupPath {
    Write-Section "Step 2: 查找备份"

    $backupPath = "$TargetPath-backup-$BackupTimestamp"

    if (-not (Test-Path $backupPath)) {
        Write-Log "备份路径不存在: $backupPath" -Level Error
        Write-Log "请确认时间戳格式正确，或查看可用备份:" -Level Error
        Write-Log "  Get-ChildItem '$TargetPath-backup-*' | Select-Object Name" -Level Error
        exit 1
    }

    Write-Log "找到备份: $backupPath" -Level Success

    # 获取备份信息
    $backupInfo = Get-Item $backupPath
    Write-Log "备份创建时间: $($backupInfo.CreationTime)" -Level Info

    return $backupPath
}

# ============================================
# 停止服务
# ============================================
function Stop-WebAPIService {
    Write-Section "Step 3: 停止服务"

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if (-not $service) {
        Write-Log "服务不存在: $ServiceName" -Level Warning
        return
    }

    if ($service.Status -eq "Stopped") {
        Write-Log "服务已经停止" -Level Info
        return
    }

    Write-Log "停止服务: $ServiceName" -Level Info
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 3

    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Stopped") {
        Write-Log "服务停止成功" -Level Success
    } else {
        Write-Log "服务停止失败，状态: $($service.Status)" -Level Error
        exit 1
    }
}

# ============================================
# 备份当前部署（回滚前）
# ============================================
function Backup-CurrentDeployment {
    Write-Section "Step 4: 备份当前部署"

    $currentBackupPath = "$TargetPath-before-rollback-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

    Write-Log "备份当前部署到: $currentBackupPath" -Level Info
    Write-Log "（如果回滚失败，可以用此备份恢复）" -Level Warning

    try {
        Copy-Item -Path $TargetPath -Destination $currentBackupPath -Recurse -Force
        Write-Log "当前部署备份完成" -Level Success
    }
    catch {
        Write-Log "当前部署备份失败: $($_.Exception.Message)" -Level Error
        Write-Log "继续执行回滚..." -Level Warning
    }
}

# ============================================
# 恢复应用文件
# ============================================
function Restore-ApplicationFiles {
    param([string]$BackupPath)

    Write-Section "Step 5: 恢复应用文件"

    Write-Log "清理当前部署目录..." -Level Info

    try {
        # 删除当前部署目录内容（保留目录本身）
        Get-ChildItem -Path $TargetPath -Recurse | Remove-Item -Force -Recurse -ErrorAction Stop
        Write-Log "当前部署目录已清理" -Level Success
    }
    catch {
        Write-Log "清理部署目录失败: $($_.Exception.Message)" -Level Error
        exit 1
    }

    Write-Log "从备份恢复文件..." -Level Info

    try {
        # 从备份恢复文件
        Copy-Item -Path "$BackupPath\*" -Destination $TargetPath -Recurse -Force
        Write-Log "应用文件恢复完成" -Level Success

        # 验证关键文件
        $exePath = Join-Path $TargetPath "LYBT.WebAPI.exe"
        if (-not (Test-Path $exePath)) {
            Write-Log "关键文件缺失: $exePath" -Level Error
            exit 1
        }
        Write-Log "关键文件验证通过" -Level Success
    }
    catch {
        Write-Log "应用文件恢复失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 恢复数据库
# ============================================
function Restore-Database {
    Write-Section "Step 6: 恢复数据库"

    if (-not $RestoreDatabase) {
        Write-Log "跳过数据库恢复（-RestoreDatabase 未指定）" -Level Info
        return
    }

    if ([string]::IsNullOrWhiteSpace($DatabaseBackupFile)) {
        Write-Log "数据库备份文件路径未指定（-DatabaseBackupFile 必需）" -Level Error
        exit 1
    }

    if (-not (Test-Path $DatabaseBackupFile)) {
        Write-Log "数据库备份文件不存在: $DatabaseBackupFile" -Level Error
        exit 1
    }

    Write-Log "数据库备份文件: $DatabaseBackupFile" -Level Info

    # 获取连接字符串
    $connectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Log "数据库连接字符串未配置" -Level Error
        exit 1
    }

    # 解析服务器名称
    if ($connectionString -match "Server=([^;]+)") {
        $server = $Matches[1]
    } else {
        $server = "localhost"
    }

    # 解析数据库名称
    if ($connectionString -match "Database=([^;]+)") {
        $database = $Matches[1]
    } else {
        $database = "LYBTDB"
    }

    Write-Log "目标数据库: $server\$database" -Level Info
    Write-Log "⚠️ 警告：数据库恢复将覆盖当前数据！" -Level Warning

    # 确认恢复
    $confirmation = Read-Host "确认恢复数据库？输入 YES 继续"
    if ($confirmation -ne "YES") {
        Write-Log "用户取消数据库恢复" -Level Warning
        return
    }

    Write-Log "执行数据库恢复..." -Level Info

    try {
        # 构建恢复命令（使用REPLACE强制覆盖）
        $restoreCommand = @"
USE master;
ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$database]
FROM DISK = N'$DatabaseBackupFile'
WITH REPLACE, RECOVERY;
ALTER DATABASE [$database] SET MULTI_USER;
"@

        $result = & sqlcmd -S $server -Q $restoreCommand -W -b 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Log "数据库恢复失败" -Level Error
            Write-Log "错误信息: $result" -Level Error
            exit 1
        }

        Write-Log "数据库恢复成功" -Level Success
    }
    catch {
        Write-Log "数据库恢复失败: $($_.Exception.Message)" -Level Error
        exit 1
    }
}

# ============================================
# 启动服务
# ============================================
function Start-WebAPIService {
    Write-Section "Step 7: 启动服务"

    if ($SkipServiceRestart) {
        Write-Log "跳过服务启动（-SkipServiceRestart）" -Level Warning
        return
    }

    Write-Log "启动服务: $ServiceName" -Level Info
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 5

    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-Log "服务启动成功" -Level Success
    } else {
        Write-Log "服务启动失败，状态: $($service.Status)" -Level Error

        # 尝试从事件日志获取错误信息
        Write-Log "查看事件日志以获取详细信息:" -Level Info
        Get-EventLog -LogName Application -Source $ServiceName -Newest 5 -ErrorAction SilentlyContinue |
            Format-List TimeGenerated, EntryType, Message |
            Out-String |
            Write-Host

        exit 1
    }
}

# ============================================
# 验证回滚
# ============================================
function Test-Rollback {
    Write-Section "Step 8: 验证回滚"

    if ($SkipServiceRestart) {
        Write-Log "跳过验证（服务未启动）" -Level Warning
        return
    }

    Write-Log "等待服务完全启动..." -Level Info
    Start-Sleep -Seconds 10

    # 检查服务状态
    $service = Get-Service -Name $ServiceName
    Write-Log "服务状态: $($service.Status)" -Level Info

    if ($service.Status -ne "Running") {
        Write-Log "服务未运行" -Level Error
        exit 1
    }

    # 检查端口监听
    $listeningPort = Get-NetTCPConnection -LocalPort 5001 -State Listen -ErrorAction SilentlyContinue
    if ($listeningPort) {
        Write-Log "HTTPS端口5001监听正常" -Level Success
    } else {
        Write-Log "警告: HTTPS端口5001未监听" -Level Warning
    }

    # 检查日志文件
    $logPath = Join-Path $TargetPath "logs"
    if (Test-Path $logPath) {
        $latestLog = Get-ChildItem $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestLog) {
            Write-Log "最新日志文件: $($latestLog.FullName)" -Level Info
            Write-Log "日志最后10行:" -Level Info
            Get-Content $latestLog.FullName -Tail 10 | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Gray
            }
        }
    }

    Write-Log "回滚验证完成" -Level Success
}

# ============================================
# 主流程
# ============================================
function Main {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║        LYBT 部署回滚脚本 v1.0.0                          ║" -ForegroundColor White
    Write-Host "║          凌隐宝堂中医诊所诊疗系统                         ║" -ForegroundColor White
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    Write-Log "回滚开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Level Info
    Write-Log "目标路径: $TargetPath" -Level Info
    Write-Log "备份时间戳: $BackupTimestamp" -Level Info
    Write-Log "服务名称: $ServiceName" -Level Info
    Write-Log "恢复数据库: $RestoreDatabase" -Level Info
    Write-Log "日志文件: $LogFile" -Level Info

    Write-Host "`n⚠️ 警告：回滚操作将覆盖当前部署！" -ForegroundColor Yellow
    $confirmation = Read-Host "确认继续？输入 YES 继续"

    if ($confirmation -ne "YES") {
        Write-Log "用户取消回滚操作" -Level Warning
        exit 0
    }

    try {
        Test-Prerequisites
        $backupPath = Get-BackupPath
        Stop-WebAPIService
        Backup-CurrentDeployment
        Restore-ApplicationFiles -BackupPath $backupPath
        Restore-Database
        Start-WebAPIService
        Test-Rollback

        Write-Section "回滚完成"
        Write-Log "回滚成功完成！" -Level Success
        Write-Log "服务名称: $ServiceName" -Level Info
        Write-Log "部署路径: $TargetPath" -Level Info
        Write-Log "备份版本: $BackupTimestamp" -Level Info
        Write-Log "日志文件: $LogFile" -Level Info

        Write-Host "`n下一步:" -ForegroundColor Cyan
        Write-Host "  1. 访问 Swagger UI: https://localhost:5001/swagger" -ForegroundColor White
        Write-Host "  2. 检查服务状态: Get-Service -Name $ServiceName" -ForegroundColor White
        Write-Host "  3. 查看服务日志: $TargetPath\logs" -ForegroundColor White
        Write-Host "  4. 验证应用功能是否正常" -ForegroundColor White
        Write-Host ""

    } catch {
        Write-Log "回滚失败: $($_.Exception.Message)" -Level Error
        Write-Log "堆栈跟踪: $($_.ScriptStackTrace)" -Level Error
        exit 1
    }
}

# 执行主流程
Main
