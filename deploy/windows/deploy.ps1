#requires -RunAsAdministrator
# LYBT WebAPI Windows Server 部署脚本
# 使用方法: .\deploy.ps1 -DeployPath "C:\Services\LYBT-API" -Port 5000

param(
    [Parameter()]
    [string]$DeployPath = "C:\Services\LYBT-API",
    
    [Parameter()]
    [int]$HttpPort = 5000,
    
    [Parameter()]
    [int]$HttpsPort = 5001,
    
    [Parameter()]
    [string]$ServiceName = "LYBT-API",
    
    [Parameter()]
    [string]$SourcePath = ".",
    
    [Parameter()]
    [switch]$CreateIIS,
    
    [Parameter()]
    [string]$IISAppPool = "LYBT-API",
    
    [Parameter()]
    [string]$IISSiteName = "LYBT-API"
)

$ErrorActionPreference = "Stop"

Write-Host "=== LYBT WebAPI Windows Server 部署脚本 ===" -ForegroundColor Cyan
Write-Host ""

# 检查 .NET SDK
Write-Host "[1/8] 检查 .NET 运行时..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error ".NET SDK 未安装。请先安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
}
Write-Host "✓ 检测到 .NET 版本: $dotnetVersion" -ForegroundColor Green

# 检查 SQL Server
Write-Host ""
Write-Host "[2/8] 检查 SQL Server 连接..." -ForegroundColor Yellow
try {
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=true;Connection Timeout=5"
    $sqlConnection.Open()
    $sqlConnection.Close()
    Write-Host "✓ SQL Server 连接正常" -ForegroundColor Green
} catch {
    Write-Warning "⚠ 无法连接到 SQL Server。请确保 SQL Server 已安装并运行。"
    Write-Warning "  如果 SQL Server 使用命名实例，请修改连接字符串。"
}

# 创建部署目录
Write-Host ""
Write-Host "[3/8] 创建部署目录..." -ForegroundColor Yellow
if (-not (Test-Path $DeployPath)) {
    New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null
    Write-Host "✓ 创建目录: $DeployPath" -ForegroundColor Green
} else {
    Write-Host "✓ 目录已存在: $DeployPath" -ForegroundColor Green
}

# 创建日志目录
$logPath = Join-Path $DeployPath "logs"
if (-not (Test-Path $logPath)) {
    New-Item -ItemType Directory -Path $logPath -Force | Out-Null
    Write-Host "✓ 创建日志目录: $logPath" -ForegroundColor Green
}

# 发布应用
Write-Host ""
Write-Host "[4/8] 发布应用程序..." -ForegroundColor Yellow
$projectPath = Join-Path $SourcePath "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Error "找不到项目文件: $projectPath"
    exit 1
}

dotnet publish $projectPath -c Release -o $DeployPath --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Error "发布失败"
    exit 1
}
Write-Host "✓ 应用发布成功" -ForegroundColor Green

# 配置环境变量
Write-Host ""
Write-Host "[5/8] 检查环境变量..." -ForegroundColor Yellow
$jwtSecret = [Environment]::GetEnvironmentVariable("JWT_SECRET", "Machine")
if (-not $jwtSecret) {
    Write-Warning "⚠ JWT_SECRET 环境变量未设置"
    Write-Host "  请运行以下命令设置 JWT 密钥:" -ForegroundColor Yellow
    Write-Host "  [Environment]::SetEnvironmentVariable('JWT_SECRET', 'your-secure-key-min-32-chars', 'Machine')" -ForegroundColor Cyan
} else {
    Write-Host "✓ JWT_SECRET 已设置" -ForegroundColor Green
}

# 创建 Windows Service
Write-Host ""
Write-Host "[6/8] 创建 Windows Service..." -ForegroundColor Yellow

# 停止并删除现有服务
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  停止现有服务..." -ForegroundColor Gray
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# 创建新服务
$exePath = Join-Path $DeployPath "LYBT.WebAPI.exe"
sc.exe create $ServiceName `
    binPath= "$exePath" `
    start= auto `
    displayName= "凌隐宝堂 WebAPI 服务" `
    depend= "MSSQLSERVER" | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "创建服务失败"
    exit 1
}

sc.exe description $ServiceName "凌隐宝堂中医诊所管理系统 WebAPI 服务 - 提供 REST API 接口" | Out-Null
Write-Host "✓ Windows Service 创建成功: $ServiceName" -ForegroundColor Green

# 配置防火墙
Write-Host ""
Write-Host "[7/8] 配置防火墙规则..." -ForegroundColor Yellow
$firewallRuleName = "LYBT-API-$HttpPort"
$existingRule = Get-NetFirewallRule -DisplayName $firewallRuleName -ErrorAction SilentlyContinue
if (-not $existingRule) {
    New-NetFirewallRule `
        -DisplayName $firewallRuleName `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $HttpPort `
        -Action Allow `
        -Profile Domain,Private | Out-Null
    Write-Host "✓ 防火墙规则已创建: 允许端口 $HttpPort" -ForegroundColor Green
} else {
    Write-Host "✓ 防火墙规则已存在" -ForegroundColor Green
}

# IIS 部署（可选）
if ($CreateIIS) {
    Write-Host ""
    Write-Host "[8/8] 配置 IIS..." -ForegroundColor Yellow
    
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    if (-not (Get-Module WebAdministration)) {
        Write-Warning "⚠ IIS 管理模块未安装，跳过 IIS 配置"
    } else {
        # 创建应用程序池
        $appPool = Get-Item "IIS:\AppPools\$IISAppPool" -ErrorAction SilentlyContinue
        if (-not $appPool) {
            New-Item -Path "IIS:\AppPools\$IISAppPool" -ItemType AppPool | Out-Null
            Set-ItemProperty -Path "IIS:\AppPools\$IISAppPool" -Name "managedRuntimeVersion" -Value ""
            Set-ItemProperty -Path "IIS:\AppPools\$IISAppPool" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
            Write-Host "✓ IIS 应用程序池已创建: $IISAppPool" -ForegroundColor Green
        }
        
        # 创建网站
        $site = Get-Item "IIS:\Sites\$IISSiteName" -ErrorAction SilentlyContinue
        if (-not $site) {
            New-Item -Path "IIS:\Sites\$IISSiteName" `
                -Bindings @{protocol="http";bindingInformation="*:$HttpPort:"} `
                -PhysicalPath $DeployPath | Out-Null
            Set-ItemProperty -Path "IIS:\Sites\$IISSiteName" -Name "applicationPool" -Value $IISAppPool
            Write-Host "✓ IIS 网站已创建: $IISSiteName (端口 $HttpPort)" -ForegroundColor Green
        }
        
        Write-Host "  注意：IIS 模式需要安装 ASP.NET Core Hosting Bundle" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "[8/8] 跳过 IIS 配置（使用 Windows Service 模式）" -ForegroundColor Yellow
}

# 创建 web.config（用于 IIS 部署）
$webConfigPath = Join-Path $DeployPath "web.config"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified"/>
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\LYBT.WebAPI.dll" stdoutLogEnabled="false" hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="10485760" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
"@ | Out-File -FilePath $webConfigPath -Encoding UTF8

# 启动服务
Write-Host ""
Write-Host "启动服务..." -ForegroundColor Yellow
try {
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 3
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq "Running") {
        Write-Host "✓ 服务启动成功" -ForegroundColor Green
    } else {
        Write-Warning "⚠ 服务状态: $($service.Status)"
    }
} catch {
    Write-Warning "⚠ 服务启动失败，请检查日志"
}

# 输出部署信息
Write-Host ""
Write-Host "=== 部署完成 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "部署路径: $DeployPath" -ForegroundColor White
Write-Host "服务名称: $ServiceName" -ForegroundColor White
Write-Host "HTTP 端口: $HttpPort" -ForegroundColor White
Write-Host "HTTPS 端口: $HttpsPort" -ForegroundColor White
Write-Host ""
Write-Host "健康检查: http://localhost:$HttpPort/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "管理命令:" -ForegroundColor Yellow
Write-Host "  启动服务: Start-Service -Name '$ServiceName'" -ForegroundColor Gray
Write-Host "  停止服务: Stop-Service -Name '$ServiceName'" -ForegroundColor Gray
Write-Host "  查看日志: Get-Content '$logPath\lybt-web-api-`$(Get-Date -Format 'yyyyMMdd').log' -Tail 50" -ForegroundColor Gray
Write-Host ""
Write-Host "注意事项:" -ForegroundColor Yellow
Write-Host "  1. 请确保已设置 JWT_SECRET 环境变量" -ForegroundColor Gray
Write-Host "  2. 请确保 SQL Server 已运行且数据库已创建" -ForegroundColor Gray
Write-Host "  3. 生产环境建议使用 HTTPS 和 SSL 证书" -ForegroundColor Gray
