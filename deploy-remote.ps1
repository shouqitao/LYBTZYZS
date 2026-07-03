# LYBT WebAPI 远程更新脚本
# 在开发机运行，自动发布+传输+重启目标服务器
#
# 使用方式:
#   .\deploy-remote.ps1 -Server "192.168.1.100"
#   .\deploy-remote.ps1 -Server "192.168.1.100" -User "admin" -Pass "xxx"
#
# 前置条件: 目标服务器已安装 .NET 8 Runtime + SQL Server

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [string]$User = "Administrator",
    
    [string]$Password,
    
    [int]$Port = 5000,
    
    [string]$InstallPath = "C:\Services\\LYBT-API"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LYBT WebAPI 远程更新" -ForegroundColor Cyan
Write-Host "  目标: $Server" -ForegroundColor Cyan
# ── Step 1: 本地构建 ──
Write-Host "`n[1/4] 本地构建 Release..." -ForegroundColor Yellow
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -c Release -o "$env:TEMP\lybt-update" --self-contained false -v quiet
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
Write-Host "  构建成功" -ForegroundColor Green

# ── Step 2: 创建临时传输目录 ──
Write-Host "`n[2/4] 准备传输..." -ForegroundColor Yellow
$staging = "$env:TEMP\lybt-staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -Path $staging -ItemType Directory -Force | Out-Null
Copy-Item "$env:TEMP\lybt-update\*" $staging -Recurse -Force
Write-Host "  文件准备完成" -ForegroundColor Green

# ── Step 3: 停止远程服务 + 传输文件 ──
Write-Host "`n[3/4] 更新远程服务器..." -ForegroundColor Yellow

# 创建 PSCredential
if ($Password) {
    $secPassword = ConvertTo-SecureString $Password -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential($User, $secPassword)
} else {
    $cred = Get-Credential -UserName $User -Message "输入 $Server 的登录凭据"
}

# 使用 SMB 文件共享传输 (最可靠)
$uncPath = "\\$Server\C$\Services\\LYBT-API"
try {
    # 先停止远程服务
    Invoke-Command -ComputerName $Server -Credential $cred -ScriptBlock {
        Stop-Service -Name "LYBT-API" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
    }
    Write-Host "  远程服务已停止" -ForegroundColor Green
    
    # 复制文件 (排除日志和配置)
    if (Test-Path $uncPath) {
        Remove-Item "$uncPath\*" -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -Path $uncPath -ItemType Directory -Force | Out-Null
    }
    Copy-Item "$staging\*" $uncPath -Recurse -Force
    Write-Host "  文件传输完成" -ForegroundColor Green
    
    # 重启远程服务
    Invoke-Command -ComputerName $Server -Credential $cred -ScriptBlock {
        Start-Service -Name "LYBT-API"
        Start-Sleep -Seconds 5
    }
    Write-Host "  远程服务已重启" -ForegroundColor Green
    
} catch {
    Write-Host "  远程操作失败: $_" -ForegroundColor Red
    Write-Host "  请检查: 1) 目标服务器已启用 WinRM  2) 防火墙允许 5985/5986  3) 用户凭据正确" -ForegroundColor Yellow
    exit 1
}

# ── Step 4: 验证 ──
Write-Host "`n[4/4] 验证部署..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $health = Invoke-RestMethod -Uri "http://$Server`:$Port/health" -TimeoutSec 10
    Write-Host "  健康检查: 通过" -ForegroundColor Green
} catch {
    Write-Host "  健康检查: 失败 - $_" -ForegroundColor Red
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  更新完成!" -ForegroundColor Green
Write-Host "  服务地址: http://$Server`:$Port" -ForegroundColor Cyan
Write-Host "  Swagger:  http://$Server`:$Port/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# 清理临时文件
Remove-Item $env:TEMP\lybt-update -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
