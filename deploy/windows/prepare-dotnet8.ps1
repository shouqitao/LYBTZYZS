#Requires -RunAsAdministrator
# =============================================================================
# 凌隐宝堂 - Windows Server 2012 R2 .NET 8 环境准备脚本
# 功能: 安装 .NET 8 ASP.NET Core Hosting Bundle + 配置 TLS 1.2
# 用法: 在 192.168.190.248 上以管理员运行
# =============================================================================

$ErrorActionPreference = "Stop"

Write-Host "=== LYBT .NET 8 环境准备 (Server 2012 R2) ===" -ForegroundColor Cyan
Write-Host ""

# -------------------- 检查操作系统版本 --------------------
Write-Host "[1/5] 检查操作系统..." -ForegroundColor Yellow
$os = Get-CimInstance Win32_OperatingSystem
$osVersion = [System.Environment]::OSVersion.Version
Write-Host "  系统: $($os.Caption)" -ForegroundColor Gray
Write-Host "  版本: $($os.Version)" -ForegroundColor Gray

if ($osVersion.Major -lt 6 -or ($osVersion.Major -eq 6 -and $osVersion.Minor -lt 3)) {
    Write-Error "需要 Windows Server 2012 R2 或更高版本"
    exit 1
}
Write-Host "  ✓ 操作系统兼容" -ForegroundColor Green

# -------------------- 检查/安装 .NET 8 Runtime --------------------
Write-Host ""
Write-Host "[2/5] 检查 .NET 8 运行时..." -ForegroundColor Yellow

$dotnetExists = $false
try {
    $dotnetVer = dotnet --version 2>$null
    if ($dotnetVer -and $dotnetVer.StartsWith("8.")) {
        Write-Host "  ✓ .NET 8 已安装: $dotnetVer" -ForegroundColor Green
        $dotnetExists = $true
    } elseif ($dotnetVer) {
        Write-Host "  ⚠ 已安装 .NET $dotnetVer，需要 8.x" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  .NET 未安装" -ForegroundColor Gray
}

if (-not $dotnetExists) {
    Write-Host "  正在下载 .NET 8 ASP.NET Core Hosting Bundle..." -ForegroundColor Yellow

    # .NET 8 ASP.NET Core Hosting Bundle 下载地址
    $installerUrl = "https://download.visualstudio.microsoft.com/download/pr/9e5a7d9f-5b03-4b16-a332-5b8b8e85c053/91c0b326c2cbfd8aab2f0e0a9a097b94/dotnet-hosting-8.0.16-win.exe"
    $installerPath = "$env:TEMP\dotnet-hosting-8.0-win.exe"

    # 如果本地已有下载则跳过
    if (Test-Path "C:\Install\dotnet-hosting-8.0-win.exe") {
        $installerPath = "C:\Install\dotnet-hosting-8.0-win.exe"
        Write-Host "  使用本地安装包: $installerPath" -ForegroundColor Gray
    } else {
        # 尝试从微软官方下载
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        try {
            Write-Host "  下载中 (约 25MB)..." -ForegroundColor Gray
            Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing
            Write-Host "  ✓ 下载完成" -ForegroundColor Green
        } catch {
            Write-Warning "  自动下载失败，请手动下载:"
            Write-Host "    https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
            Write-Host "    选择 'ASP.NET Core Runtime' → Windows Hosting Bundle" -ForegroundColor Cyan
            Write-Host "    下载后放到 C:\Install\dotnet-hosting-8.0-win.exe" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "  下载完成后重新运行此脚本" -ForegroundColor Yellow
            exit 1
        }
    }

    Write-Host "  安装中..." -ForegroundColor Yellow
    Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru

    # 刷新环境变量
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine")

    # 验证安装
    try {
        $dotnetVer = dotnet --version 2>$null
        Write-Host "  ✓ 安装成功: $dotnetVer" -ForegroundColor Green
    } catch {
        Write-Warning "  安装完成但需要重启系统才能生效"
        Write-Host "  请重启后重新运行此脚本验证" -ForegroundColor Yellow
    }
}

# -------------------- 启用 TLS 1.2 --------------------
Write-Host ""
Write-Host "[3/5] 配置 TLS 1.2..." -ForegroundColor Yellow

# .NET 8 默认使用 TLS 1.2+，但系统层面也要启用
$tlsPath = "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols"
$versions = @(
    @{ Name = "TLS 1.2\Client"; DisabledByDefault = 0; Enabled = 1 },
    @{ Name = "TLS 1.2\Server"; DisabledByDefault = 0; Enabled = 1 }
)

foreach ($ver in $versions) {
    $path = Join-Path $tlsPath $ver.Name
    if (-not (Test-Path $path)) {
        New-Item -Path $path -Force | Out-Null
    }
    Set-ItemProperty -Path $path -Name "DisabledByDefault" -Value $ver.DisabledByDefault -Type DWord
    Set-ItemProperty -Path $path -Name "Enabled" -Value $ver.Enabled -Type DWord
}
Write-Host "  ✓ TLS 1.2 已启用" -ForegroundColor Green

# -------------------- 配置 Windows Update --------------------
Write-Host ""
Write-Host "[4/5] 检查系统更新..." -ForegroundColor Yellow
$hotfix = Get-HotFix -Id "KB2999226" -ErrorAction SilentlyContinue
if (-not $hotfix) {
    Write-Host "  ⚠ 缺少 Universal C Runtime (KB2999226)" -ForegroundColor Yellow
    Write-Host "    建议安装以确保 .NET 8 稳定运行" -ForegroundColor Gray
    Write-Host "    下载: https://www.microsoft.com/download/details.aspx?id=49093" -ForegroundColor Cyan
} else {
    Write-Host "  ✓ Universal C Runtime 已安装" -ForegroundColor Green
}

# -------------------- 配置防火墙 --------------------
Write-Host ""
Write-Host "[5/5] 配置防火墙..." -ForegroundColor Yellow

$ports = @(5000, 5001)
foreach ($port in $ports) {
    $ruleName = "LYBT-API-$port"
    $existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if (-not $existing) {
        New-NetFirewallRule `
            -DisplayName $ruleName `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $port `
            -Action Allow `
            -Profile Domain,Private `
            -Description "凌隐宝堂 WebAPI 端口" | Out-Null
        Write-Host "  ✓ 已开放端口 $port" -ForegroundColor Green
    } else {
        Write-Host "  ✓ 端口 $port 规则已存在" -ForegroundColor Gray
    }
}

# -------------------- 完成 --------------------
Write-Host ""
Write-Host "=== 环境准备完成 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "下一步:" -ForegroundColor Yellow
Write-Host "  1. 从 Ubuntu 执行部署脚本:" -ForegroundColor Gray
Write-Host "     ./scripts/deploy-to-server.sh" -ForegroundColor Cyan
Write-Host "  2. 或手动将发布文件复制到 C:\Services\LYBT-API" -ForegroundColor Gray
Write-Host "  3. 启动服务: Start-Service -Name 'LYBT-API'" -ForegroundColor Gray
Write-Host ""
Write-Host "API 地址: http://192.168.190.248:5000" -ForegroundColor Cyan
Write-Host "Swagger:  http://192.168.190.248:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "⚠ 注意: Server 2012 R2 已结束扩展支持，建议尽快升级到 Server 2019" -ForegroundColor Yellow
