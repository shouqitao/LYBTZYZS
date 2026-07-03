# LYBT WebAPI 一键发布脚本
# 使用方式: .\deploy.ps1 [-Environment Production|Development] [-SkipDb] [-Port 5000]

param(
    [ValidateSet("Production", "Development")]
    [string]$Environment = "Production",
    
    [switch]$SkipDb,
    
    [int]$Port = 5000,
    
    [string]$InstallPath = "C:\Services\LYBT-API"
)

$ErrorActionPreference = "Stop"
$ProjectPath = Join-Path $PSScriptRoot "src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  凌隐宝堂 WebAPI 一键发布工具" -ForegroundColor Cyan
Write-Host "  环境: $Environment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Step 1: Build
Write-Host "`n[1/5] 构建项目..." -ForegroundColor Yellow
dotnet publish $ProjectPath -c Release -o "$env:TEMP\lybt-publish" --self-contained false -v quiet
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
Write-Host "  构建成功" -ForegroundColor Green

# Step 2: Stop existing service
Write-Host "`n[2/5] 停止现有服务..." -ForegroundColor Yellow
$service = Get-Service -Name "LYBT-API" -ErrorAction SilentlyContinue
if ($service) {
    Stop-Service -Name "LYBT-API" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Write-Host "  服务已停止" -ForegroundColor Green
} else {
    Write-Host "  服务不存在，跳过" -ForegroundColor Gray
}

# Step 3: Deploy files
Write-Host "`n[3/5] 部署文件到 $InstallPath..." -ForegroundColor Yellow
if (-not (Test-Path $InstallPath)) {
    New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
}
Copy-Item -Path "$env:TEMP\lybt-publish\*" -Destination $InstallPath -Recurse -Force
Write-Host "  文件复制完成" -ForegroundColor Green

# Step 4: Configure environment
Write-Host "`n[4/5] 配置环境变量..." -ForegroundColor Yellow
if ($Environment -eq "Production") {
    # Check if JWT_SECRET exists
    $jwt = [Environment]::GetEnvironmentVariable("JWT_SECRET", "Machine")
    if (-not $jwt -or $jwt.Length -lt 32) {
        $jwt = [System.Guid]::NewGuid().ToString("N") + [System.Guid]::NewGuid().ToString("N")
        [Environment]::SetEnvironmentVariable("JWT_SECRET", $jwt, "Machine")
        Write-Host "  已生成 JWT_SECRET (32字符)" -ForegroundColor Green
    } else {
        Write-Host "  JWT_SECRET 已存在" -ForegroundColor Gray
    }
    
    # Configure firewall
    $rule = Get-NetFirewallRule -DisplayName "LYBT-API-$Port" -ErrorAction SilentlyContinue
    if (-not $rule) {
        New-NetFirewallRule -DisplayName "LYBT-API-$Port" -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -ErrorAction SilentlyContinue
        Write-Host "  防火墙规则已添加" -ForegroundColor Green
    }
}
Write-Host "  环境配置完成" -ForegroundColor Green

# Step 5: Start service
Write-Host "`n[5/5] 启动服务..." -ForegroundColor Yellow
if ($service) {
    Start-Service -Name "LYBT-API"
    Start-Sleep -Seconds 5
    $status = (Get-Service -Name "LYBT-API").Status
    if ($status -eq "Running") {
        Write-Host "  服务启动成功 (状态: $status)" -ForegroundColor Green
    } else {
        Write-Host "  服务状态异常: $status" -ForegroundColor Red
    }
} else {
    # Create new service
    sc.exe create LYBT-API binPath= "`"$InstallPath\LYBT.WebAPI.exe`"" start= auto displayName= "凌隐宝堂 WebAPI 服务" | Out-Null
    sc.exe description LYBT-API "凌隐宝堂中医诊所管理系统 WebAPI 服务" | Out-Null
    Start-Service -Name "LYBT-API"
    Start-Sleep -Seconds 5
    Write-Host "  服务已创建并启动" -ForegroundColor Green
}

# Verify
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  验证部署..." -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "http://localhost:$Port/health" -TimeoutSec 5
    Write-Host "  健康检查: 通过" -ForegroundColor Green
} catch {
    Write-Host "  健康检查: 失败 ($_)" -ForegroundColor Red
}

Write-Host "`n  部署完成!" -ForegroundColor Green
Write-Host "  服务地址: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "  健康检查: http://localhost:$Port/health" -ForegroundColor Cyan
Write-Host "  Swagger: http://localhost:$Port/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
