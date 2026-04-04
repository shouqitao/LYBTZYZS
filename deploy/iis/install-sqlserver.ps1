#requires -Version 5.1
<#!
.SYNOPSIS
  SQL Server Standard 本地安装与配置脚本（仅本地访问）。

.DESCRIPTION
  功能：
  1) 安装 SQL Server 2022 Standard（支持 ISO 挂载或本地安装程序）
  2) 配置为仅本地访问（禁用 TCP/IP，启用 Shared Memory/Named Pipes）
  3) 不创建外部防火墙规则（SQL Server 仅供本地 WebAPI 使用）
  4) 创建 LYBT 数据库
  5) 配置 Windows 身份验证访问权限
  
  安全说明：
  - SQL Server 不对外提供 TCP 服务，仅供本地 WebAPI 进程访问
  - 使用 Shared Memory 和 Named Pipes 进行本地进程间通信
  - 不创建 SQL Server 的防火墙规则
  - 注意：SQL Server Standard 需要用户提供安装介质（ISO 或 setup.exe）
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SaPassword = "LYBT@Sa2024!Secure",

    [Parameter()]
    [string]$SqlInstanceName = "MSSQLSERVER",

    [Parameter()]
    [string]$InstallerPath = "",

    [Parameter()]
    [ValidateSet("Standard", "Express")]
    [string]$Edition = "Standard",

    [Parameter()]
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Log {
    param(
        [Parameter(Mandatory)] [string]$Message,
        [ValidateSet('INFO', 'WARN', 'ERROR', 'OK')] [string]$Level = 'INFO'
    )
    $prefix = "[{0}] [{1}]" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level
    switch ($Level) {
        'OK'    { Write-Host "$prefix $Message" -ForegroundColor Green }
        'WARN'  { Write-Host "$prefix $Message" -ForegroundColor Yellow }
        'ERROR' { Write-Host "$prefix $Message" -ForegroundColor Red }
        default { Write-Host "$prefix $Message" -ForegroundColor Cyan }
    }
}

function Test-SqlServerInstalled {
    $services = Get-Service | Where-Object { $_.Name -like "*SQL*" }
    return $services.Count -gt 0
}

function Install-SqlServer {
    param(
        [string]$SaPassword,
        [string]$SetupPath
    )

    # 如果是 ISO 文件，挂载它
    $isoDrive = $null
    if ($SetupPath -match "\.iso$") {
        Write-Log "挂载 ISO 镜像: $SetupPath" "INFO"
        $isoDrive = (Mount-DiskImage -ImagePath $SetupPath -PassThru | Get-Volume).DriveLetter
        if (-not $isoDrive) {
            throw "无法挂载 ISO 镜像"
        }
        $SetupPath = "$isoDrive`:\setup.exe"
        Write-Log "ISO 已挂载到 ${isoDrive}: 驱动器" "OK"
    }

    if (-not (Test-Path $SetupPath)) {
        throw "找不到 SQL Server 安装程序: $SetupPath"
    }

    Write-Log "安装 SQL Server $Edition (这可能需要 15-30 分钟)..." "INFO"
    
    # 实例名称参数
    $instanceParam = if ($SqlInstanceName -eq "MSSQLSERVER") { "" } else { "/INSTANCENAME=$SqlInstanceName" }
    
    # 静默安装配置 - 禁用 TCP/IP，启用 Named Pipes
    $arguments = @(
        "/QUIET"
        "/IACCEPTSQLSERVERLICENSETERMS"
        "/ACTION=Install"
        "/FEATURES=SQLENGINE"
        "/SQLSVCACCOUNT=`"NT AUTHORITY\SYSTEM`""
        "/SQLSYSADMINACCOUNTS=`"BUILTIN\ADMINISTRATORS`""
        "/SECURITYMODE=SQL"
        "/SAPWD=`"$SaPassword`""
        "/TCPENABLED=0"
        "/NPENABLED=1"
    )
    
    if ($instanceParam) {
        $arguments += $instanceParam
    }

    if ($Edition -eq "Express") {
        Write-Log "使用 Express 版本参数..." "INFO"
    }

    $process = Start-Process -FilePath $SetupPath -ArgumentList $arguments -PassThru -Wait
    
    # 卸载 ISO
    if ($isoDrive) {
        Write-Log "卸载 ISO 镜像..." "INFO"
        Dismount-DiskImage -ImagePath $isoDrive -ErrorAction SilentlyContinue
    }

    if ($process.ExitCode -ne 0) {
        throw "SQL Server 安装失败，退出码: $($process.ExitCode)"
    }

    Write-Log "SQL Server $Edition 安装完成" "OK"
}

function Install-SqlServerExpress {
    param([string]$SaPassword)

    $tempDir = Join-Path $env:TEMP "lybt-sql-install"
    if (-not (Test-Path $tempDir)) {
        New-Item -Path $tempDir -ItemType Directory -Force | Out-Null
    }

    # SQL Server 2022 Express 下载链接
    $installerUrl = "https://go.microsoft.com/fwlink/?linkid=2215158"
    $installerPath = Join-Path $tempDir "SQLEXPR_x64_ENU.exe"

    Write-Log "下载 SQL Server 2022 Express..." "INFO"
    try {
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing -TimeoutSec 300
    }
    catch {
        # 备用下载链接
        $installerUrl = "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe"
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing -TimeoutSec 300
    }

    Write-Log "安装 SQL Server Express (这可能需要 10-20 分钟)..." "INFO"
    
    # 静默安装配置 - 禁用 TCP/IP (TCPENABLED=0)，启用 Named Pipes (NPENABLED=1)
    $arguments = "/QUIET /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQLENGINE /INSTANCENAME=$SqlInstanceName /SQLSVCACCOUNT=`"NT AUTHORITY\SYSTEM`" /SQLSYSADMINACCOUNTS=`"BUILTIN\ADMINISTRATORS`" /SECURITYMODE=SQL /SAPWD=`"$SaPassword`" /TCPENABLED=0 /NPENABLED=1"
    
    $process = Start-Process -FilePath $installerPath -ArgumentList $arguments -PassThru -Wait
    
    if ($process.ExitCode -ne 0) {
        throw "SQL Server 安装失败，退出码: $($process.ExitCode)"
    }

    Write-Log "SQL Server Express 安装完成" "OK"
}

function Disable-SqlTcpProtocol {
    Write-Log "禁用 SQL Server TCP/IP 协议（仅本地访问）..." "INFO"

    # 使用 WMI 禁用 TCP
    $wmi = Get-WmiObject -Namespace "root\Microsoft\SqlServer\ComputerManagement16" -Class ServerNetworkProtocol -Filter "ProtocolName='Tcp'" -ErrorAction SilentlyContinue
    if ($wmi) {
        $wmi.SetDisable() | Out-Null
        Write-Log "已禁用 TCP/IP 协议 (WMI)" "OK"
    }

    # 使用注册表禁用 TCP
    $regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.$SqlInstanceName\MSSQLServer\SuperSocketNetLib\Tcp"
    if (Test-Path $regPath) {
        Set-ItemProperty -Path $regPath -Name "Enabled" -Value 0
        Write-Log "已禁用 TCP/IP 协议 (注册表)" "OK"
    }

    # 注册表路径根据实例名确定
    $instanceKey = if ($SqlInstanceName -eq "MSSQLSERVER") { "MSSQL16.MSSQLSERVER" } else { "MSSQL16.$SqlInstanceName" }
    
    # 启用 Shared Memory
    $smRegPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceKey\MSSQLServer\SuperSocketNetLib\Sm"
    if (Test-Path $smRegPath) {
        Set-ItemProperty -Path $smRegPath -Name "Enabled" -Value 1
        Write-Log "已启用 Shared Memory 协议" "OK"
    }

    # 启用 Named Pipes
    $npRegPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceKey\MSSQLServer\SuperSocketNetLib\Np"
    if (Test-Path $npRegPath) {
        Set-ItemProperty -Path $npRegPath -Name "Enabled" -Value 1
        Write-Log "已启用 Named Pipes 协议" "OK"
    }
}

function Initialize-LybtDatabase {
    param([string]$SaPassword)

    Write-Log "创建 LYBT 数据库和配置权限..." "INFO"

    # 等待 SQL Server 服务启动
    $sqlService = Get-Service -Name "MSSQL`$$SqlInstanceName" -ErrorAction SilentlyContinue
    if ($sqlService) {
        Write-Log "等待 SQL Server 服务启动..." "INFO"
        $sqlService | Start-Service -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 10
        
        $maxWait = 30
        $waited = 0
        while ($sqlService.Status -ne 'Running' -and $waited -lt $maxWait) {
            Start-Sleep -Seconds 2
            $sqlService.Refresh()
            $waited += 2
        }
    }

    $serverName = "$env:COMPUTERNAME\$SqlInstanceName"
    
    # 创建数据库
    $createDbSql = @"
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LYBTDB')
BEGIN
    CREATE DATABASE LYBTDB;
    ALTER DATABASE LYBTDB SET RECOVERY SIMPLE;
END
"@

    try {
        $createDbSql | sqlcmd -S $serverName -U sa -P $SaPassword -b -e
        Write-Log "数据库 LYBTDB 已创建或已存在" "OK"
    }
    catch {
        Write-Log "创建数据库失败: $($_.Exception.Message)" "WARN"
    }

    # 授予 Windows Network Service 账户访问权限
    $grantAccessSql = @"
USE LYBTDB;

-- 创建 Windows 登录 (Network Service)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'NT AUTHORITY\NETWORK SERVICE')
BEGIN
    CREATE LOGIN [NT AUTHORITY\NETWORK SERVICE] FROM WINDOWS;
END

-- 创建数据库用户
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'NT AUTHORITY\NETWORK SERVICE')
BEGIN
    CREATE USER [NT AUTHORITY\NETWORK SERVICE] FOR LOGIN [NT AUTHORITY\NETWORK SERVICE];
END

-- 添加到 db_owner 角色
ALTER ROLE db_owner ADD MEMBER [NT AUTHORITY\NETWORK SERVICE];

-- 也添加当前管理员账户
DECLARE @CurrentUser NVARCHAR(128) = SUSER_SNAME();
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = @CurrentUser)
BEGIN
    EXEC('CREATE LOGIN [' + @CurrentUser + '] FROM WINDOWS');
END

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @CurrentUser)
BEGIN
    EXEC('CREATE USER [' + @CurrentUser + '] FOR LOGIN [' + @CurrentUser + ']');
END

EXEC('ALTER ROLE db_owner ADD MEMBER [' + @CurrentUser + ']');
"@

    try {
        $grantAccessSql | sqlcmd -S $serverName -U sa -P $SaPassword -b -e
        Write-Log "已配置 Windows 身份验证访问权限 (Network Service)" "OK"
    }
    catch {
        Write-Log "配置权限失败: $($_.Exception.Message)" "WARN"
    }

    # 启用 Windows 身份验证模式（混合模式）
    $enableWinAuthSql = @"
EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'LoginMode', REG_DWORD, 2;
"@

    try {
        $enableWinAuthSql | sqlcmd -S $serverName -U sa -P $SaPassword -b -e
        Write-Log "已启用混合身份验证模式 (Windows + SQL)" "OK"
    }
    catch {
        Write-Log "启用 Windows 身份验证失败: $($_.Exception.Message)" "WARN"
    }
}

function Restart-SqlServices {
    Write-Log "重启 SQL Server 服务..." "INFO"

    $services = @(
        "MSSQL`$$SqlInstanceName",
        "SQLTELEMETRY`$$SqlInstanceName",
        "SQLWriter",
        "SQLBrowser"
    )

    foreach ($svcName in $services) {
        $svc = Get-Service -Name $svcName -ErrorAction SilentlyContinue
        if ($svc) {
            Write-Log "重启服务: $($svc.Name)" "INFO"
            $svc | Restart-Service -Force -ErrorAction SilentlyContinue
        }
    }

    Start-Sleep -Seconds 5
    Write-Log "SQL Server 服务已重启" "OK"
}

# 主执行流程
try {
    Write-Log "=== SQL Server $Edition 安装与配置（仅本地访问）===" "INFO"

    if (-not $SkipInstall) {
        if (Test-SqlServerInstalled) {
            Write-Log "检测到 SQL Server 已安装，跳过安装步骤" "WARN"
        }
        else {
            if ($Edition -eq "Standard") {
                if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
                    throw "使用 Standard 版本时，必须通过 -InstallerPath 指定安装程序路径（ISO 或 setup.exe）"
                }
                Install-SqlServer -SaPassword $SaPassword -SetupPath $InstallerPath
            }
            else {
                Install-SqlServerExpress -SaPassword $SaPassword
            }
        }
    }

    Disable-SqlTcpProtocol
    Initialize-LybtDatabase -SaPassword $SaPassword
    Restart-SqlServices

    Write-Log "=== SQL Server 配置完成 ===" "OK"
    Write-Host ""
    Write-Host "数据库连接字符串 (Windows 身份验证):" -ForegroundColor Cyan
    Write-Host "Server=$env:COMPUTERNAME\$SqlInstanceName;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;" -ForegroundColor Green
    Write-Host ""
    Write-Host "安全说明:" -ForegroundColor Cyan
    Write-Host "- SQL Server TCP/IP 已禁用，仅本地访问" -ForegroundColor Green
    Write-Host "- 使用 Shared Memory/Named Pipes 进行本地通信" -ForegroundColor Green
    Write-Host "- 未创建 SQL Server 防火墙规则" -ForegroundColor Green
    Write-Host "- WebAPI 服务账户 (NETWORK SERVICE) 已授权" -ForegroundColor Green
}
catch {
    Write-Log "错误: $($_.Exception.Message)" "ERROR"
    exit 1
}
