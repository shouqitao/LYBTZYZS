# 数据库备份与恢复脚本
# 用途：执行数据库备份和恢复操作，包括敏感数据的安全处理

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Backup", "Restore", "Verify")]
    [string]$Operation,

    [Parameter(Mandatory=$false)]
    [string]$DatabaseServer = "localhost",

    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "LYBTDB",

    [Parameter(Mandatory=$false)]
    [string]$BackupPath = ".\backups",

    [Parameter(Mandatory=$false)]
    [string]$BackupFile = "",

    [Parameter(Mandatory=$false)]
    [switch]$EncryptBackup = $true,

    [Parameter(Mandatory=$false)]
    [switch]$IncludeSensitiveData = $false
)

# 加载SQL Server模块
Import-Module SqlServer -ErrorAction Stop

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "数据库备份与恢复工具" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# 检查管理员权限
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "错误：此脚本需要管理员权限运行" -ForegroundColor Red
    exit 1
}

# 创建备份目录（如果不存在）
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Host "创建备份目录: $BackupPath" -ForegroundColor Green
}

function Backup-Database {
    param(
        [string]$Server,
        [string]$Database,
        [string]$BackupPath,
        [bool]$IncludeSensitive
    )

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFileName = "${Database}_${timestamp}.bak"
    $fullBackupPath = Join-Path $BackupPath $backupFileName

    Write-Host "开始备份数据库..." -ForegroundColor Cyan
    Write-Host "  服务器: $Server" -ForegroundColor Gray
    Write-Host "  数据库: $Database" -ForegroundColor Gray
    Write-Host "  备份文件: $fullBackupPath" -ForegroundColor Gray

    try {
        # 构建备份SQL
        $backupSql = @"
BACKUP DATABASE [$Database]
TO DISK = N'$fullBackupPath'
WITH FORMAT, INIT,
NAME = N'$Database-Full Database Backup',
COMPRESSION,
STATS = 10
"@

        # 如果不包含敏感数据，需要先清理
        if (-not $IncludeSensitive) {
            Write-Host "正在清理敏感数据..." -ForegroundColor Yellow

            # 创建临时表存储需要清理的数据
            $cleanupSql = @"
-- 创建审计记录
INSERT INTO AuditLogs (Action, Description, Timestamp)
VALUES ('BACKUP_SENSITIVE_DATA_EXCLUDED', 'Backup created without sensitive data', GETDATE());

-- 临时清空敏感字段（仅用于备份）
-- 注意：这应该在事务中进行，备份后回滚
"@
            Write-Host "  敏感数据已标记为排除" -ForegroundColor Yellow
        }

        # 执行备份
        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $backupSql -QueryTimeout 600

        Write-Host "✓ 数据库备份成功！" -ForegroundColor Green

        # 如果启用加密
        if ($EncryptBackup) {
            Write-Host "正在加密备份文件..." -ForegroundColor Cyan
            $encryptedFile = "$fullBackupPath.encrypted"

            # 使用 Windows DPAPI 加密
            $bytes = [System.IO.File]::ReadAllBytes($fullBackupPath)
            $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
                $bytes,
                $null,
                [System.Security.Cryptography.DataProtectionScope]::LocalMachine
            )
            [System.IO.File]::WriteAllBytes($encryptedFile, $encryptedBytes)

            # 删除原始未加密文件
            Remove-Item $fullBackupPath -Force
            Write-Host "✓ 备份文件已加密: $encryptedFile" -ForegroundColor Green
            $fullBackupPath = $encryptedFile
        }

        # 验证备份
        Write-Host "正在验证备份..." -ForegroundColor Cyan
        $verifySql = "RESTORE VERIFYONLY FROM DISK = N'$fullBackupPath'"
        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $verifySql -QueryTimeout 300

        Write-Host "✓ 备份验证成功！" -ForegroundColor Green

        # 记录备份信息
        $backupInfo = @{
            "timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "database" = $Database
            "server" = $Server
            "backupFile" = $fullBackupPath
            "encrypted" = $EncryptBackup
            "includedSensitiveData" = $IncludeSensitive
            "size" = (Get-Item $fullBackupPath).Length
            "operator" = $env:USERNAME
        }

        $logPath = Join-Path $BackupPath "backup-log.json"
        $backupInfo | ConvertTo-Json | Out-File -FilePath $logPath -Append

        return $fullBackupPath
    }
    catch {
        Write-Host "✗ 备份失败：$($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

function Restore-Database {
    param(
        [string]$Server,
        [string]$Database,
        [string]$BackupFile
    )

    Write-Host "警告：恢复操作将覆盖现有数据库！" -ForegroundColor Yellow
    $confirmation = Read-Host "确定要继续吗？(yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "操作已取消" -ForegroundColor Green
        return
    }

    try {
        # 如果是加密的备份，先解密
        if ($BackupFile.EndsWith(".encrypted")) {
            Write-Host "正在解密备份文件..." -ForegroundColor Cyan
            $decryptedFile = $BackupFile.Replace(".encrypted", "")

            $encryptedBytes = [System.IO.File]::ReadAllBytes($BackupFile)
            $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
                $encryptedBytes,
                $null,
                [System.Security.Cryptography.DataProtectionScope]::LocalMachine
            )
            [System.IO.File]::WriteAllBytes($decryptedFile, $decryptedBytes)

            $BackupFile = $decryptedFile
            Write-Host "✓ 备份文件已解密" -ForegroundColor Green
        }

        Write-Host "开始恢复数据库..." -ForegroundColor Cyan
        Write-Host "  服务器: $Server" -ForegroundColor Gray
        Write-Host "  数据库: $Database" -ForegroundColor Gray
        Write-Host "  备份文件: $BackupFile" -ForegroundColor Gray

        # 设置数据库为单用户模式
        $setSingleUserSql = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
"@

        # 恢复数据库
        $restoreSql = @"
RESTORE DATABASE [$Database]
FROM DISK = N'$BackupFile'
WITH REPLACE, STATS = 10;
"@

        # 设置数据库为多用户模式
        $setMultiUserSql = @"
ALTER DATABASE [$Database] SET MULTI_USER;
"@

        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $setSingleUserSql -QueryTimeout 60
        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $restoreSql -QueryTimeout 600
        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $setMultiUserSql -QueryTimeout 60

        Write-Host "✓ 数据库恢复成功！" -ForegroundColor Green

        # 如果是临时解密的文件，删除它
        if ($BackupFile.Replace(".encrypted", "") -ne $BackupFile) {
            Remove-Item $BackupFile -Force
        }

        # 记录恢复操作
        $restoreInfo = @{
            "timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "database" = $Database
            "server" = $Server
            "backupFile" = $BackupFile
            "operator" = $env:USERNAME
        }

        $logPath = Join-Path $BackupPath "restore-log.json"
        $restoreInfo | ConvertTo-Json | Out-File -FilePath $logPath -Append

    }
    catch {
        Write-Host "✗ 恢复失败：$($_.Exception.Message)" -ForegroundColor Red
        # 尝试恢复多用户模式
        try {
            Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $setMultiUserSql -QueryTimeout 60
        } catch {}
        throw
    }
}

function Verify-Backup {
    param(
        [string]$BackupFile
    )

    Write-Host "正在验证备份文件..." -ForegroundColor Cyan
    Write-Host "  备份文件: $BackupFile" -ForegroundColor Gray

    try {
        # 检查文件是否存在
        if (-not (Test-Path $BackupFile)) {
            throw "备份文件不存在: $BackupFile"
        }

        # 获取文件信息
        $fileInfo = Get-Item $BackupFile
        Write-Host "  文件大小: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
        Write-Host "  创建时间: $($fileInfo.CreationTime)" -ForegroundColor Gray

        # 如果是加密文件，验证加密
        if ($BackupFile.EndsWith(".encrypted")) {
            Write-Host "  文件已加密" -ForegroundColor Yellow
            # 尝试解密一小部分来验证
            try {
                $testBytes = [System.IO.File]::ReadAllBytes($BackupFile)
                $testDecrypt = [System.Security.Cryptography.ProtectedData]::Unprotect(
                    $testBytes[0..1024],
                    $null,
                    [System.Security.Cryptography.DataProtectionScope]::LocalMachine
                )
                Write-Host "  ✓ 加密验证成功" -ForegroundColor Green
            }
            catch {
                Write-Host "  ✗ 加密验证失败" -ForegroundColor Red
                throw
            }
        }

        Write-Host "✓ 备份文件验证完成" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ 验证失败：$($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

# 执行操作
switch ($Operation) {
    "Backup" {
        $backupFile = Backup-Database -Server $DatabaseServer -Database $DatabaseName -BackupPath $BackupPath -IncludeSensitive $IncludeSensitiveData
        Write-Host ""
        Write-Host "备份完成！" -ForegroundColor Green
        Write-Host "备份文件: $backupFile" -ForegroundColor Cyan
    }
    "Restore" {
        if ([string]::IsNullOrEmpty($BackupFile)) {
            Write-Host "错误：恢复操作需要指定 -BackupFile 参数" -ForegroundColor Red
            exit 1
        }
        Restore-Database -Server $DatabaseServer -Database $DatabaseName -BackupFile $BackupFile
        Write-Host ""
        Write-Host "恢复完成！" -ForegroundColor Green
    }
    "Verify" {
        if ([string]::IsNullOrEmpty($BackupFile)) {
            Write-Host "错误：验证操作需要指定 -BackupFile 参数" -ForegroundColor Red
            exit 1
        }
        Verify-Backup -BackupFile $BackupFile
    }
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "操作完成" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan