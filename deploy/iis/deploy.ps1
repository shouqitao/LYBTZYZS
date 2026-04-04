#requires -Version 5.1
<#!
.SYNOPSIS
  LYBTZYZS WebAPI Windows Server 2012 IIS 一键部署脚本。

.DESCRIPTION
  功能：
  1) 自动检查管理员权限
  2) 自动安装 .NET 8 Hosting Bundle（含 ASP.NET Core Runtime）
  3) 发布 WebAPI 到目标目录
  4) 配置必需环境变量（JWT_SECRET / SYSADMIN_PASSWORD / NEWUSER_PASSWORD）
  5) 创建并启动 Kestrel Windows Service（默认监听 5000）
  6) 调用 setup-iis.ps1 配置 IIS 反向代理
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ProjectFile = "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj",

    [Parameter()]
    [string]$PublishRoot = "C:\Services\LYBT-WebAPI",

    [Parameter()]
    [string]$ServiceName = "LYBT-WebAPI-Kestrel",

    [Parameter()]
    [int]$KestrelHttpPort = 5000,

    [Parameter()]
    [int]$KestrelHttpsPort = 5001,

    [Parameter()]
    [string]$SiteName = "LYBT-WebAPI",

    [Parameter()]
    [int]$IISHttpPort = 80,

    [Parameter()]
    [int]$IISHttpsPort = 443,

    [Parameter()]
    [string]$HostHeader = "",

    [Parameter()]
    [string]$ProxyRoot = "C:\inetpub\lybt-webapi-proxy",

    [Parameter()]
    [string]$JwtSecret,

    [Parameter()]
    [string]$SysAdminPassword,

    [Parameter()]
    [string]$NewUserPassword,

    [Parameter()]
    [switch]$SkipDotNetInstall,

    [Parameter()]
    [switch]$SkipIisSetup,

    [Parameter()]
    [string]$SqlInstanceName = "MSSQLSERVER",

    [Parameter()]
    [string]$SqlInstallerPath = "",

    [Parameter()]
    [ValidateSet("Standard", "Express")]
    [string]$SqlEdition = "Standard"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function New-LogContext {
    param([string]$BaseDir)

    if (-not (Test-Path $BaseDir)) {
        New-Item -Path $BaseDir -ItemType Directory -Force | Out-Null
    }

    $logDir = Join-Path $BaseDir "logs"
    if (-not (Test-Path $logDir)) {
        New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    }

    return Join-Path $logDir ("deploy-{0}.log" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}

function Write-Log {
    param(
        [Parameter(Mandatory)] [string]$Message,
        [ValidateSet('INFO', 'WARN', 'ERROR', 'OK')] [string]$Level = 'INFO'
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[{0}] [{1}] {2}" -f $timestamp, $Level, $Message
    Add-Content -Path $script:LogFile -Value $line

    switch ($Level) {
        'OK'    { Write-Host $line -ForegroundColor Green }
        'WARN'  { Write-Host $line -ForegroundColor Yellow }
        'ERROR' { Write-Host $line -ForegroundColor Red }
        default { Write-Host $line -ForegroundColor Cyan }
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [scriptblock]$Action
    )

    Write-Log "开始: $Name" "INFO"
    try {
        & $Action
        Write-Log "完成: $Name" "OK"
    }
    catch {
        Write-Log "失败: $Name -> $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Install-DotNetHostingBundleIfNeeded {
    if ($SkipDotNetInstall) {
        Write-Log "已跳过 .NET 8 安装检查（-SkipDotNetInstall）" "WARN"
        return
    }

    $hasRuntime = $false
    try {
        $runtimeList = & dotnet --list-runtimes 2>$null
        if ($runtimeList -match 'Microsoft\.AspNetCore\.App\s+8\.' -or $runtimeList -match 'Microsoft\.NETCore\.App\s+8\.') {
            $hasRuntime = $true
        }
    }
    catch {
        $hasRuntime = $false
    }

    if ($hasRuntime) {
        Write-Log ".NET 8 Runtime 已存在，跳过安装" "OK"
        return
    }

    $tempDir = Join-Path $env:TEMP "lybt-deploy"
    if (-not (Test-Path $tempDir)) {
        New-Item -Path $tempDir -ItemType Directory -Force | Out-Null
    }

    $installer = Join-Path $tempDir "dotnet-hosting-8.exe"
    $url = "https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe"

    Write-Log "下载 .NET 8 Hosting Bundle: $url" "INFO"
    Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing

    Write-Log "静默安装 .NET 8 Hosting Bundle" "INFO"
    $p = Start-Process -FilePath $installer -ArgumentList '/install', '/quiet', '/norestart' -PassThru -Wait
    if ($p.ExitCode -ne 0) {
        throw ".NET Hosting Bundle 安装失败，退出码: $($p.ExitCode)"
    }

    Write-Log ".NET 8 Hosting Bundle 安装完成" "OK"
}

function Set-MachineEnvIfProvided {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter()] [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $existing = [Environment]::GetEnvironmentVariable($Name, 'Machine')
        if ([string]::IsNullOrWhiteSpace($existing)) {
            Write-Log "环境变量 $Name 未提供且机器级未设置，请手工设置" "WARN"
        }
        else {
            Write-Log "环境变量 $Name 已存在（沿用机器级值）" "OK"
        }
        return
    }

    [Environment]::SetEnvironmentVariable($Name, $Value, 'Machine')
    Write-Log "已设置机器级环境变量: $Name" "OK"
}

function Ensure-WindowsService {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$DotnetPath,
        [Parameter(Mandatory)] [string]$DllPath,
        [Parameter(Mandatory)] [int]$Port
    )

    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($svc) {
        Write-Log "检测到已有服务 $Name，执行重建" "WARN"
        if ($svc.Status -ne 'Stopped') {
            Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
        & sc.exe delete $Name | Out-Null
        Start-Sleep -Seconds 2
    }

    $binPath = '"{0}" "{1}" --urls "http://0.0.0.0:{2}" --environment Production' -f $DotnetPath, $DllPath, $Port
    & sc.exe create $Name binPath= $binPath start= auto DisplayName= "LYBT WebAPI Kestrel Service" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "创建服务失败: $Name"
    }

    & sc.exe description $Name "LYBTZYZS WebAPI Kestrel 后端服务" | Out-Null
    & sc.exe failure $Name reset= 60 actions= restart/5000/restart/5000/restart/5000 | Out-Null

    & sc.exe config $Name obj= "NT AUTHORITY\NETWORKSERVICE" | Out-Null

    Start-Service -Name $Name
    Start-Sleep -Seconds 3

    $status = (Get-Service -Name $Name).Status
    if ($status -ne 'Running') {
        throw "服务未运行，当前状态: $status"
    }
    Write-Log "服务已启动: $Name (监听 0.0.0.0:$Port，允许外部访问)" "OK"
}

function New-WebApiFirewallRules {
    param(
        [int]$HttpPort = 5000,
        [int]$HttpsPort = 5001
    )

    Write-Log "配置 WebAPI Windows 防火墙规则..." "INFO"

    $httpRule = "LYBT-WebAPI-HTTP-$HttpPort"
    if (-not (Get-NetFirewallRule -DisplayName $httpRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $httpRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort $HttpPort -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙规则已创建: $httpRule (允许外部访问)" "OK"
    }

    $httpsRule = "LYBT-WebAPI-HTTPS-$HttpsPort"
    if (-not (Get-NetFirewallRule -DisplayName $httpsRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $httpsRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort $HttpsPort -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙规则已创建: $httpsRule (允许外部访问)" "OK"
    }

    $iisHttpRule = "LYBT-IIS-HTTP-80"
    if (-not (Get-NetFirewallRule -DisplayName $iisHttpRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $iisHttpRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80 -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙规则已创建: $iisHttpRule" "OK"
    }
}

try {
    if (-not (Test-IsAdministrator)) {
        throw "请使用管理员权限运行本脚本（右键 PowerShell -> 以管理员身份运行）"
    }

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $script:LogFile = New-LogContext -BaseDir $PSScriptRoot

    Write-Log "LYBTZYZS IIS 一键部署开始" "INFO"
    Write-Log "日志文件: $script:LogFile" "INFO"

    $projectFullPath = Join-Path $repoRoot $ProjectFile
    if (-not (Test-Path $projectFullPath)) {
        throw "找不到项目文件: $projectFullPath"
    }

    $publishDir = Join-Path $PublishRoot "app"
    if (-not (Test-Path $publishDir)) {
        New-Item -Path $publishDir -ItemType Directory -Force | Out-Null
    }

    Invoke-Step -Name "安装 SQL Server Express" -Action {
        $sqlScript = Join-Path $PSScriptRoot "install-sqlserver.ps1"
        if (Test-Path $sqlScript) {
            & $sqlScript -SaPassword "LYBT@Sa2024!Secure" -LybtDbPassword "LYBT@Db2024!App"
            if ($LASTEXITCODE -ne 0) {
                throw "SQL Server 安装失败"
            }
        }
        else {
            Write-Log "SQL Server 安装脚本不存在，跳过" "WARN"
        }
    }
    Invoke-Step -Name "安装 SQL Server $SqlEdition" -Action {
        $sqlScript = Join-Path $PSScriptRoot "install-sqlserver.ps1"
        if (Test-Path $sqlScript) {
            $sqlParams = @{
                SaPassword = "LYBT@Sa2024!Secure"
                SqlInstanceName = $SqlInstanceName
                Edition = $SqlEdition
            }
            
            if ($SqlEdition -eq "Standard" -and $SqlInstallerPath) {
                $sqlParams['InstallerPath'] = $SqlInstallerPath
            }
            
            & $sqlScript @sqlParams
            if ($LASTEXITCODE -ne 0) {
                throw "SQL Server 安装失败"
            }
        }
        else {
            Write-Log "SQL Server 安装脚本不存在，跳过" "WARN"
        }
    }

    Invoke-Step -Name "安装 .NET 8 Runtime/Hosting Bundle" -Action {
        Install-DotNetHostingBundleIfNeeded
    }

    $computerName = $env:COMPUTERNAME
    $serverNameForConn = if ($SqlInstanceName -eq "MSSQLSERVER") { $computerName } else { "$computerName\$SqlInstanceName" }
    $trustedConnectionString = "Server=$serverNameForConn;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"
    $env:ConnectionStrings__DefaultConnection = $trustedConnectionString

    Invoke-Step -Name "发布 WebAPI" -Action {
        & dotnet publish "$projectFullPath" -c Release -o "$publishDir" --self-contained false
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish 失败，退出码: $LASTEXITCODE"
        }
    }

    Invoke-Step -Name "复制部署模板文件" -Action {
        Copy-Item -Path (Join-Path $PSScriptRoot "web.config") -Destination (Join-Path $ProxyRoot "web.config") -Force -ErrorAction SilentlyContinue
        Copy-Item -Path (Join-Path $PSScriptRoot "appsettings.Override.json") -Destination (Join-Path $publishDir "appsettings.Override.json") -Force
        Copy-Item -Path (Join-Path $PSScriptRoot ".env.example") -Destination (Join-Path $PublishRoot ".env.example") -Force
        
        $computerName = $env:COMPUTERNAME
        $appSettings = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=$computerName\SQLEXPRESS;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
"@
        $appSettings | Out-File -FilePath (Join-Path $publishDir "appsettings.Production.json") -Encoding UTF8 -Force
    }

    Invoke-Step -Name "配置关键环境变量" -Action {
        Set-MachineEnvIfProvided -Name "JWT_SECRET" -Value $JwtSecret
        Set-MachineEnvIfProvided -Name "SYSADMIN_PASSWORD" -Value $SysAdminPassword
        Set-MachineEnvIfProvided -Name "NEWUSER_PASSWORD" -Value $NewUserPassword
        $computerName = $env:COMPUTERNAME
        $serverNameForConn = if ($SqlInstanceName -eq "MSSQLSERVER") { $computerName } else { "$computerName\$SqlInstanceName" }
        $trustedConn = "Server=$serverNameForConn;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"
        [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $trustedConn, "Machine")
        [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
    }

    Invoke-Step -Name "配置 Windows 防火墙规则" -Action {
        New-WebApiFirewallRules -HttpPort $KestrelHttpPort -HttpsPort $KestrelHttpsPort
    }

    Invoke-Step -Name "创建并启动 Kestrel Windows Service" -Action {
        $dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source
        $dllPath = Join-Path $publishDir "LYBT.WebAPI.dll"
        if (-not (Test-Path $dllPath)) {
            throw "发布产物不存在: $dllPath"
        }
        Ensure-WindowsService -Name $ServiceName -DotnetPath $dotnetPath -DllPath $dllPath -Port $KestrelHttpPort
    }

    if (-not $SkipIisSetup) {
        Invoke-Step -Name "配置 IIS 反向代理站点" -Action {
            $setupScript = Join-Path $PSScriptRoot "setup-iis.ps1"
            if (-not (Test-Path $setupScript)) {
                throw "找不到 setup-iis.ps1: $setupScript"
            }

            & $setupScript `
                -SiteName $SiteName `
                -SiteRoot $ProxyRoot `
                -KestrelHttpPort $KestrelHttpPort `
                -IISHttpPort $IISHttpPort `
                -IISHttpsPort $IISHttpsPort `
                -HostHeader $HostHeader

            if ($LASTEXITCODE -ne 0) {
                throw "setup-iis.ps1 执行失败，退出码: $LASTEXITCODE"
            }
        }
    }
    else {
        Write-Log "已跳过 IIS 配置（-SkipIisSetup）" "WARN"
    }

    Write-Log "部署完成" "OK"
    Write-Host ""
    Write-Host "==================== 部署摘要 ====================" -ForegroundColor Green
    Write-Host "发布目录       : $publishDir"
    Write-Host "服务名称       : $ServiceName"
    Write-Host "Kestrel HTTP   : http://0.0.0.0:$KestrelHttpPort (允许外部访问)" -ForegroundColor Yellow
    Write-Host "Kestrel HTTPS  : https://0.0.0.0:$KestrelHttpsPort (需证书额外配置)" -ForegroundColor Yellow
    Write-Host "IIS 入口       : http://<server>:$IISHttpPort/"
    Write-Host "SQL Server     : $env:COMPUTERNAME\SQLEXPRESS"
    $displaySqlInstance = if ($SqlInstanceName -eq "MSSQLSERVER") { "(默认实例)" } else { $SqlInstanceName }
    Write-Host "SQL Server     : $env:COMPUTERNAME\$displaySqlInstance"
    Write-Host "数据库         : LYBTDB"
    Write-Host "外部访问地址   : http://$env:COMPUTERNAME`:$KestrelHttpPort/health" -ForegroundColor Cyan
    Write-Host "日志文件       : $script:LogFile"
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "防火墙规则已创建，允许外部设备连接。" -ForegroundColor Green
}
catch {
    if ($script:LogFile) {
        Write-Log "部署异常终止: $($_.Exception.Message)" "ERROR"
    }
    else {
        Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    }
    exit 1
}
