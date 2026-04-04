#requires -Version 5.1
<#!
.SYNOPSIS
  配置 IIS + URL Rewrite + ARR，创建 LYBT 反向代理站点。
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SiteName = "LYBT-WebAPI",

    [Parameter()]
    [string]$AppPoolName = "LYBT-WebAPI-Pool",

    [Parameter()]
    [string]$SiteRoot = "C:\inetpub\lybt-webapi-proxy",

    [Parameter()]
    [int]$KestrelHttpPort = 5000,

    [Parameter()]
    [int]$IISHttpPort = 80,

    [Parameter()]
    [int]$IISHttpsPort = 443,

    [Parameter()]
    [string]$HostHeader = "",

    [Parameter()]
    [string]$CertThumbprint = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

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

function Install-MsiPackage {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$Url,
        [Parameter(Mandatory)] [string]$FileName
    )

    $tempDir = Join-Path $env:TEMP "lybt-iis-deps"
    if (-not (Test-Path $tempDir)) {
        New-Item -Path $tempDir -ItemType Directory -Force | Out-Null
    }

    $msiPath = Join-Path $tempDir $FileName
    Write-Log "下载 ${Name}: $Url" "INFO"
    Invoke-WebRequest -Uri $Url -OutFile $msiPath -UseBasicParsing

    Write-Log "安装 $Name" "INFO"
    $p = Start-Process -FilePath "msiexec.exe" -ArgumentList '/i', ('"{0}"' -f $msiPath), '/qn', '/norestart' -PassThru -Wait
    if ($p.ExitCode -ne 0) {
        throw "$Name 安装失败，退出码: $($p.ExitCode)"
    }
}

function Ensure-IISFeatures {
    if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
        Write-Log "检测到 ServerManager，安装 IIS 角色与组件" "INFO"
        Install-WindowsFeature Web-Server, Web-WebServer, Web-Common-Http, Web-Static-Content, Web-Default-Doc, Web-Http-Errors, Web-Http-Logging, Web-Request-Monitor, Web-Filtering, Web-Performance, Web-Stat-Compression, Web-Security, Web-Http-Compression, Web-Mgmt-Tools, Web-Scripting-Tools | Out-Null
    }
    else {
        Write-Log "未检测到 Install-WindowsFeature，尝试启用可选功能（兼容部分系统）" "WARN"
        Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -NoRestart -All | Out-Null
        Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementScriptingTools -NoRestart -All | Out-Null
    }
}

function Ensure-RewriteAndArr {
    Import-Module WebAdministration -ErrorAction Stop

    $rewriteInstalled = Get-WebGlobalModule -Name "RewriteModule" -ErrorAction SilentlyContinue
    if (-not $rewriteInstalled) {
        Install-MsiPackage -Name "IIS URL Rewrite 2.1" -Url "https://download.microsoft.com/download/D/D/9/DD9A82D1-4A1E-41D5-8C3E-2B31B32D3F2E/rewrite_amd64_en-US.msi" -FileName "rewrite_amd64_en-US.msi"
    }
    else {
        Write-Log "URL Rewrite 已安装" "OK"
    }

    # ARR 3.0 官方离线安装包（x64）
    $arrReg = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\IIS Extensions\Application Request Routing" -ErrorAction SilentlyContinue
    if (-not $arrReg) {
        Install-MsiPackage -Name "IIS Application Request Routing 3.0" -Url "https://download.microsoft.com/download/9/0/5/9052A547-7B3D-4F59-B26F-8D22800F894E/requestRouter_amd64.msi" -FileName "requestRouter_amd64.msi"
    }
    else {
        Write-Log "Application Request Routing 已安装" "OK"
    }

    Import-Module WebAdministration -ErrorAction Stop
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/proxy' -Name 'enabled' -Value 'True' | Out-Null
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/proxy' -Name 'preserveHostHeader' -Value 'True' | Out-Null
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/proxy' -Name 'reverseRewriteHostInResponseHeaders' -Value 'False' | Out-Null

    Write-Log "ARR 代理已启用" "OK"
}

function Ensure-SiteRoot {
    if (-not (Test-Path $SiteRoot)) {
        New-Item -Path $SiteRoot -ItemType Directory -Force | Out-Null
        Write-Log "已创建站点目录: $SiteRoot" "OK"
    }

    $templateConfig = Join-Path $PSScriptRoot "web.config"
    $targetConfig = Join-Path $SiteRoot "web.config"
    if (-not (Test-Path $templateConfig)) {
        throw "找不到 web.config 模板: $templateConfig"
    }

    [xml]$webCfg = Get-Content $templateConfig
    $rules = $webCfg.configuration.'system.webServer'.rewrite.rules.rule
    foreach ($rule in $rules) {
        if ($rule.action -and $rule.action.url) {
            $rule.action.url = "http://{0}:{1}/{{R:1}}" -f $env:COMPUTERNAME, $KestrelHttpPort
        }
    }
    $webCfg.Save($targetConfig)
    Write-Log "已生成站点 web.config（目标: $env:COMPUTERNAME`:$KestrelHttpPort）" "OK"
}

function Ensure-IISSite {
    Import-Module WebAdministration -ErrorAction Stop

    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        New-Item "IIS:\AppPools\$AppPoolName" | Out-Null
    }

    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
    Write-Log "应用程序池就绪: $AppPoolName" "OK"

    $existing = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Log "站点已存在，先停止并移除: $SiteName" "WARN"
        Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
        Remove-Website -Name $SiteName
    }

    New-Website -Name $SiteName -PhysicalPath $SiteRoot -ApplicationPool $AppPoolName -Port $IISHttpPort -HostHeader $HostHeader | Out-Null
    Write-Log "已创建站点: $SiteName (HTTP:$IISHttpPort)" "OK"

    if (-not [string]::IsNullOrWhiteSpace($CertThumbprint)) {
        New-WebBinding -Name $SiteName -Protocol https -Port $IISHttpsPort -HostHeader $HostHeader | Out-Null
        Push-Location IIS:\SslBindings
        try {
            $certPath = "0.0.0.0!$IISHttpsPort"
            if (Test-Path $certPath) {
                Remove-Item $certPath -Force
            }
            Get-Item "cert:\LocalMachine\My\$CertThumbprint" | New-Item $certPath | Out-Null
            Write-Log "已绑定 HTTPS 证书: $CertThumbprint" "OK"
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Log "未提供 -CertThumbprint，跳过 HTTPS 绑定" "WARN"
    }

    Start-Website -Name $SiteName
}

function Ensure-FirewallRules {
    $httpRule = "LYBT-IIS-HTTP-$IISHttpPort"
    $httpsRule = "LYBT-IIS-HTTPS-$IISHttpsPort"
    $kestrelHttpRule = "LYBT-Kestrel-HTTP-$KestrelHttpPort"

    if (-not (Get-NetFirewallRule -DisplayName $httpRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $httpRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort $IISHttpPort -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙已放行 HTTP 端口: $IISHttpPort" "OK"
    }

    if (-not (Get-NetFirewallRule -DisplayName $httpsRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $httpsRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort $IISHttpsPort -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙已放行 HTTPS 端口: $IISHttpsPort" "OK"
    }

    if (-not (Get-NetFirewallRule -DisplayName $kestrelHttpRule -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $kestrelHttpRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort $KestrelHttpPort -Profile Domain,Private,Public | Out-Null
        Write-Log "防火墙已放行 Kestrel HTTP 端口: $KestrelHttpPort (允许外部直接访问)" "OK"
    }
}

try {
    if (-not (Test-IsAdministrator)) {
        throw "请使用管理员权限运行 setup-iis.ps1"
    }

    Write-Log "开始配置 IIS 反向代理" "INFO"
    Ensure-IISFeatures
    Ensure-RewriteAndArr
    Ensure-SiteRoot
    Ensure-IISSite
    Ensure-FirewallRules

    Write-Log "IIS 配置完成" "OK"
    Write-Host ""
    Write-Host "IIS 站点: $SiteName" -ForegroundColor Green
    Write-Host "访问地址: http://<server>:$IISHttpPort/" -ForegroundColor Green
}
catch {
    Write-Log "IIS 配置失败: $($_.Exception.Message)" "ERROR"
    exit 1
}
