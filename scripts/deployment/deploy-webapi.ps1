<#
.SYNOPSIS
    LYBT WebAPI 自动化部署脚本

.DESCRIPTION
    自动化部署LYBT WebAPI到Windows Server生产环境，包括：
    - 环境检查和配置验证
    - 项目编译和发布
    - 数据库迁移
    - Windows Service安装/更新
    - 服务启动和验证

.PARAMETER TargetPath
    部署目标路径（默认：D:\deploy\LYBT\WebAPI）

.PARAMETER SkipDatabaseMigration
    跳过数据库迁移步骤

.PARAMETER SkipConfigValidation
    跳过配置验证步骤（不推荐）

.PARAMETER ServiceName
    Windows Service名称（默认：LYBTWebAPI）

.PARAMETER BackupBeforeDeploy
    部署前备份现有文件

.EXAMPLE
    .\deploy-webapi.ps1
    使用默认设置部署

.EXAMPLE
    .\deploy-webapi.ps1 -TargetPath "D:\apps\lybt" -BackupBeforeDeploy
    自定义部署路径并启用备份

.NOTES
    文件名: deploy-webapi.ps1
    作者: LYBT开发团队
    版本: 1.0.0
    创建日期: 2025-10-30
    参考文档: docs/how-to-guides/server/webapi-deployment.md
#>

param(
    [string]$TargetPath = "D:\deploy\LYBT\WebAPI",
    [switch]$SkipDatabaseMigration,
    [switch]$SkipConfigValidation,
    [string]$ServiceName = "LYBTWebAPI",
    [switch]$BackupBeforeDeploy
)

# ============================================
# 配置变量
# ============================================
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$WebAPIProject = Join-Path $ProjectRoot "src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj"
$LogFile = Join-Path $PSScriptRoot "deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

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
    Write-Host "`n=============================================" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "=============================================" -ForegroundColor Cyan
}

# ============================================
# 环境检查
# ============================================
function Test-Prerequisites {
    Write-Section "Step 1: 环境检查"

    # 检查管理员权限
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Log "需要管理员权限来安装Windows Service" -Level Error
        exit 1
    }
    Write-Log "管理员权限检查通过" -Level Success

    # 检查.NET 8 Runtime
    $dotnetVersion = & dotnet --version 2>$null
    if (-not $dotnetVersion) {
        Write-Log ".NET SDK未安装" -Level Error
        exit 1
    }
    Write-Log ".NET SDK版本: $dotnetVersion" -Level Success

    # 检查项目文件
    if (-not (Test-Path $WebAPIProject)) {
        Write-Log "项目文件不存在: $WebAPIProject" -Level Error
        exit 1
    }
    Write-Log "项目文件检查通过" -Level Success

    # 检查目标路径父目录
    $parentPath = Split-Path -Parent $TargetPath
    if (-not (Test-Path $parentPath)) {
        Write-Log "创建父目录: $parentPath" -Level Info
        New-Item -ItemType Directory -Path $parentPath -Force | Out-Null
    }
}

# ============================================
# 配置验证
# ============================================
function Test-ProductionConfig {
    Write-Section "Step 2: 配置验证"

    if ($SkipConfigValidation) {
        Write-Log "跳过配置验证（-SkipConfigValidation）" -Level Warning
        return
    }

    $validateScript = Join-Path $PSScriptRoot "validate-production-config.ps1"
    if (Test-Path $validateScript) {
        Write-Log "运行配置验证脚本..." -Level Info
        & $validateScript -ExitOnError
        if ($LASTEXITCODE -ne 0) {
            Write-Log "配置验证失败，请修复后重试" -Level Error
            exit 1
        }
        Write-Log "配置验证通过" -Level Success
    } else {
        Write-Log "配置验证脚本不存在，跳过验证" -Level Warning
    }
}

# ============================================
# 备份现有部署
# ============================================
function Backup-ExistingDeployment {
    Write-Section "Step 3: 备份现有部署"

    if (-not $BackupBeforeDeploy) {
        Write-Log "跳过备份（未启用-BackupBeforeDeploy）" -Level Info
        return
    }

    if (Test-Path $TargetPath) {
        $backupPath = "$TargetPath-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Write-Log "备份到: $backupPath" -Level Info
        Copy-Item -Path $TargetPath -Destination $backupPath -Recurse -Force
        Write-Log "备份完成" -Level Success
    } else {
        Write-Log "目标路径不存在，无需备份" -Level Info
    }
}

# ============================================
# 发布应用
# ============================================
function Publish-Application {
    Write-Section "Step 4: 编译和发布"

    Write-Log "开始编译项目..." -Level Info
    $publishArgs = @(
        "publish"
        $WebAPIProject
        "-c", "Release"
        "-o", $TargetPath
        "--self-contained", "true"
        "-r", "win-x64"
        "/p:PublishSingleFile=false"
    )

    & dotnet @publishArgs 2>&1 | Tee-Object -FilePath $LogFile -Append

    if ($LASTEXITCODE -ne 0) {
        Write-Log "编译失败" -Level Error
        exit 1
    }

    Write-Log "发布完成: $TargetPath" -Level Success
}

# ============================================
# 数据库迁移
# ============================================
function Update-Database {
    Write-Section "Step 5: 数据库迁移"

    if ($SkipDatabaseMigration) {
        Write-Log "跳过数据库迁移（-SkipDatabaseMigration）" -Level Warning
        return
    }

    # 检查连接字符串
    $connectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Log "数据库连接字符串未配置，跳过迁移" -Level Warning
        return
    }

    Write-Log "执行数据库迁移..." -Level Info
    Push-Location (Split-Path $WebAPIProject)
    try {
        & dotnet ef database update --no-build 2>&1 | Tee-Object -FilePath $LogFile -Append

        if ($LASTEXITCODE -ne 0) {
            Write-Log "数据库迁移失败" -Level Error
            exit 1
        }

        Write-Log "数据库迁移完成" -Level Success
    } finally {
        Pop-Location
    }
}

# ============================================
# 安装/更新Windows Service
# ============================================
function Install-WindowsService {
    Write-Section "Step 6: Windows Service 配置"

    $exePath = Join-Path $TargetPath "LYBT.WebAPI.exe"
    if (-not (Test-Path $exePath)) {
        Write-Log "可执行文件不存在: $exePath" -Level Error
        exit 1
    }

    # 检查服务是否已存在
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if ($existingService) {
        Write-Log "服务已存在，停止服务..." -Level Info
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2

        Write-Log "删除现有服务..." -Level Info
        & sc.exe delete $ServiceName
        Start-Sleep -Seconds 2
    }

    Write-Log "创建Windows Service..." -Level Info
    $scArgs = @(
        "create"
        $ServiceName
        "binPath= `"$exePath`""
        "start= auto"
        "DisplayName= `"凌隐宝堂WebAPI服务`""
        "obj= `"LocalSystem`""
    )

    & sc.exe @scArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Log "创建服务失败" -Level Error
        exit 1
    }

    # 配置服务失败恢复策略
    Write-Log "配置服务恢复策略..." -Level Info
    & sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

    Write-Log "服务配置完成" -Level Success
}

# ============================================
# 启动服务
# ============================================
function Start-WebAPIService {
    Write-Section "Step 7: 启动服务"

    Write-Log "启动 $ServiceName 服务..." -Level Info
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
# 验证部署
# ============================================
function Test-Deployment {
    Write-Section "Step 8: 部署验证"

    Write-Log "等待服务完全启动..." -Level Info
    Start-Sleep -Seconds 10

    # 检查服务状态
    $service = Get-Service -Name $ServiceName
    Write-Log "服务状态: $($service.Status)" -Level Info

    # 检查端口监听（假设使用5001端口）
    $listeningPort = Get-NetTCPConnection -LocalPort 5001 -State Listen -ErrorAction SilentlyContinue
    if ($listeningPort) {
        Write-Log "HTTPS端口5001监听正常" -Level Success
    } else {
        Write-Log "警告: HTTPS端口5001未监听，请检查配置" -Level Warning
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
}

# ============================================
# 主流程
# ============================================
function Main {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║          LYBT WebAPI 自动化部署脚本 v1.0.0               ║" -ForegroundColor White
    Write-Host "║            凌隐宝堂中医诊所诊疗系统 WebAPI                ║" -ForegroundColor White
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    Write-Log "部署开始时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Level Info
    Write-Log "目标路径: $TargetPath" -Level Info
    Write-Log "服务名称: $ServiceName" -Level Info
    Write-Log "日志文件: $LogFile" -Level Info

    try {
        Test-Prerequisites
        Test-ProductionConfig
        Backup-ExistingDeployment
        Publish-Application
        Update-Database
        Install-WindowsService
        Start-WebAPIService
        Test-Deployment

        Write-Section "部署完成"
        Write-Log "部署成功完成！" -Level Success
        Write-Log "服务名称: $ServiceName" -Level Info
        Write-Log "部署路径: $TargetPath" -Level Info
        Write-Log "日志文件: $LogFile" -Level Info

        Write-Host "`n下一步:" -ForegroundColor Cyan
        Write-Host "  1. 访问 Swagger UI: https://localhost:5001/swagger" -ForegroundColor White
        Write-Host "  2. 检查服务状态: Get-Service -Name $ServiceName" -ForegroundColor White
        Write-Host "  3. 查看服务日志: $TargetPath\logs" -ForegroundColor White
        Write-Host ""

    } catch {
        Write-Log "部署失败: $($_.Exception.Message)" -Level Error
        Write-Log "堆栈跟踪: $($_.ScriptStackTrace)" -Level Error
        exit 1
    }
}

# 执行主流程
Main
